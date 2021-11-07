using Dawn;
using Microsoft.Extensions.Logging;
using Orchestnation.Common.Exceptions;
using Orchestnation.Common.Models;
using Orchestnation.Core.Configuration;
using Orchestnation.Core.Contexts;
using Orchestnation.Core.Jobsters;
using Orchestnation.Core.Models;
using Orchestnation.Core.Notifiers;
using Orchestnation.Core.StateHandlers;
using Orchestnation.Core.Validators;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestnation.Core.Engines
{
    public class BasicEngine<T> : IOrchestnationEngine<T> where T : IJobsterContext
    {
        private readonly IConfiguration<T> _configuration;
        private readonly JobsterFailureModel _jobsterFailureModel = new();
        private readonly JobsterManager<T> _jobsterManager;
        private readonly IJobsterStateHandler<T> _jobsterStateHandler;
        private readonly IList<Task> _jobsterTasks;
        private readonly ILogger _logger;
        private JobsterProgressModel _jobsterProgressModel;
        private Jobsters<T> _jobsters;

        public BasicEngine(
            ILogger logger,
            JobsterManager<T> jobsterManager,
            IConfiguration<T> configuration,
            IJobsterStateHandler<T> jobsterStateHandler = null)
        {
            Guard.Argument(jobsterManager)
                .NotNull();
            BlockingCollection<IJobsterAsync<T>> jobsterAsync = jobsterManager.GetJobsterAsync();
            Guard.Argument(jobsterAsync)
                .NotNull();
            Guard.Argument(configuration)
                .NotNull()
                .Member(
                    p => p.JobsterExecutor,
                    u => u.NotNull());

            _logger = logger;
            _configuration = configuration;
            _jobsters = new Jobsters<T>(jobsterAsync);
            _jobsterManager = jobsterManager;
            _jobsterTasks = new List<Task>(jobsterAsync.Count);
            _jobsterProgressModel = new JobsterProgressModel(jobsterAsync.Count);
            _jobsterStateHandler = jobsterStateHandler;

            configuration.JobsterExecutor.JobsterFinishedEvent += OnJobsterFinished;
        }

        public async Task<IList<IJobsterAsync<T>>> AddJobsters(
            CancellationToken cancellationToken,
            string groupId,
            params IJobsterAsync<T>[] jobsterAsync)
        {
            _jobsterManager.AddJobsters(OrchestnationStatus.Engine, groupId, jobsterAsync);

            if (!_jobsters.AreAllFinished())
            {
                return _jobsterManager.GetAllJobsterAsync();
            }

            return await ScheduleJobstersInternalAsync(cancellationToken);
        }

        public async Task<IList<IJobsterAsync<T>>> ScheduleJobstersAsync(
            CancellationToken cancellationToken)
        {
            await RestoreState();
            Validate();

            return await ScheduleJobstersInternalAsync(cancellationToken);
        }

        private OperationContext<T> GetContextByJobster(
            CancellationToken cancellationToken,
            IJobsterAsync<T> jobsterToSchedule)
        {
            return new OperationContext<T>(
                this,
                cancellationToken,
                _jobsters.JobstersAsync
                    .Where(
                        p => jobsterToSchedule.RequiredJobIds != null
                             && jobsterToSchedule.RequiredJobIds.Contains(p.JobId))
                    .ToArray());
        }

        private void NotifyErrors(
            Exception ex,
            IJobsterAsync<T> jobsterAsync,
            IJobsterAsync<T>[] groupJobsters)
        {
            foreach (IProgressNotifier<T> progressNotifier in _configuration.ProgressNotifiers)
            {
                progressNotifier.OnJobsterError(
                    ex,
                    jobsterAsync,
                    _jobsterProgressModel);
                progressNotifier.OnJobsterGroupError(
                    ex,
                    jobsterAsync.GroupId,
                    groupJobsters,
                    _jobsterProgressModel);
            }
        }

        private void NotifyGroupFinished(
            IJobsterAsync<T> jobsterAsync,
            IJobsterAsync<T>[] groupJobsters)
        {
            foreach (IProgressNotifier<T> progressNotifier in _configuration.ProgressNotifiers)
            {
                progressNotifier.OnJobsterGroupFinished(
                    jobsterAsync.GroupId,
                    groupJobsters,
                    _jobsterProgressModel);
            }
        }

        private async Task OnJobsterFinished(
            IJobsterAsync<T> jobsterAsync,
            JobsterStatusEnum status,
            Exception ex = null)
        {
            jobsterAsync.Status = status;
            _jobsterProgressModel.ReportJobsterFinished(status);
            IJobsterAsync<T>[] groupJobsters = _jobsters.JobstersAsync
                .Where(p => p.GroupId == jobsterAsync.GroupId)
                .ToArray();
            if (_jobsters.IsGroupFinished(jobsterAsync.GroupId))
            {
                NotifyGroupFinished(jobsterAsync, groupJobsters);
            }

            if (_jobsterStateHandler != null)
            {
                await _jobsterStateHandler.PersistState(_jobsters.JobstersAsync);
            }

            if (status != JobsterStatusEnum.Failed)
            {
                return;
            }

            _jobsterFailureModel.SetIsError(jobsterAsync.JobId, ex);
            NotifyErrors(ex, jobsterAsync, groupJobsters);
        }

        private async Task RestoreState()
        {
            if (_jobsterStateHandler == null)
            {
                return;
            }

            IEnumerable<IJobsterAsync<T>> jobsters = await _jobsterStateHandler.RestoreState();
            if (!jobsters.Any(
                p => p.Status is JobsterStatusEnum.Executing or JobsterStatusEnum.NotStarted))
            {
                return;
            }

            BlockingCollection<IJobsterAsync<T>> blockingJobsters = new(jobsters.Count());
            foreach (IJobsterAsync<T> jobster in jobsters)
            {
                jobster.Logger = _logger;
                blockingJobsters.Add(jobster);
            }

            _jobsterManager.RestoreJobsters(blockingJobsters);
            _jobsters = new Jobsters<T>(blockingJobsters, true);
            _jobsterProgressModel = new JobsterProgressModel(
                _jobsters.JobstersAsync
                    .Count(
                        p => p.Status is JobsterStatusEnum.NotStarted or JobsterStatusEnum.Executing));
            _logger.LogInformation("Previous state has been restored, resuming jobsters...");
        }

        private async Task<IList<IJobsterAsync<T>>> ScheduleJobstersInternalAsync(
            CancellationToken cancellationToken)
        {
            _jobsterManager.ApplyAdHocJobsters();
            IEnumerable<IJobsterAsync<T>> initialJobsters = _jobsters.GetNoDependencyJobsters();
            foreach (IJobsterAsync<T> jobsterMetadata in initialJobsters)
            {
                await ThrottleJobsters();

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                jobsterMetadata.Status = JobsterStatusEnum.Executing;
                _jobsterTasks.Add(
                    _configuration.JobsterExecutor.ExecuteAsync(
                        new OperationContext<T>(
                            this,
                            cancellationToken,
                            null),
                        _configuration.ProgressNotifiers,
                        _jobsterProgressModel,
                        jobsterMetadata));
            }

            do
            {
                if (_jobsterFailureModel.IsError)
                {
                    _logger.LogError($"Error has been thrown. Jobster errors:\n{_jobsterFailureModel.GetErrors()}");

                    if (_configuration.ExceptionPolicy == ExceptionPolicy.ThrowImmediately)
                    {
                        throw new JobsterException(_jobsterFailureModel);
                    }

                    break;
                }

                await ThrottleJobsters();

                if (cancellationToken.IsCancellationRequested)
                {
                    await Task.WhenAll(_jobsterTasks);
                    _logger.LogInformation("Jobsters cancelled by the user.");
                    break;
                }

                _jobsterManager.ApplyAdHocJobsters();

                IJobsterAsync<T> jobsterToSchedule =
                    _jobsters.JobstersAsync
                        .FirstOrDefault(
                            p => p.Status == JobsterStatusEnum.NotStarted
                                 && !_jobsters.JobstersAsync.Any(
                                     q => p.RequiredJobIds != null && p.RequiredJobIds.Contains(q.JobId)
                                                                   && q.Status != JobsterStatusEnum.Completed));
                if (jobsterToSchedule == null)
                {
                    await Task.WhenAll(_jobsterTasks);
                    break;
                }

                jobsterToSchedule.Status = JobsterStatusEnum.Executing;
                _jobsterTasks.Add(
                    _configuration.JobsterExecutor.ExecuteAsync(
                        GetContextByJobster(cancellationToken, jobsterToSchedule),
                        _configuration.ProgressNotifiers,
                        _jobsterProgressModel,
                        jobsterToSchedule));

                await Task.WhenAny(_jobsterTasks);
            } while (_jobsters.JobstersAsync.Any(
                p => p.Status is JobsterStatusEnum.NotStarted or JobsterStatusEnum.Executing));

            await Task.WhenAll(_jobsterTasks);
            if (_jobsterManager.IsAnyAdHocJobsterPending()
                && !cancellationToken.IsCancellationRequested)
            {
                return await ScheduleJobstersInternalAsync(cancellationToken);
            }

            _logger.LogInformation("All jobsters completed. Job is done.");

            if (_jobsterFailureModel.IsError
                && _configuration.ExceptionPolicy == ExceptionPolicy.ThrowAtTheEnd)
            {
                throw new JobsterException(_jobsterFailureModel);
            }

            return _jobsters.JobstersAsync
                .ToList();
        }

        private async Task ThrottleJobsters()
        {
            if (_configuration.BatchSize == 0)
            {
                return;
            }

            while (_configuration.BatchSize <= _jobsterTasks.Count(p => !p.IsCompleted))
            {
                await Task.WhenAny(_jobsterTasks);
            }
        }

        private void Validate()
        {
            foreach (IJobsterValidator<T> validator in _configuration.JobsterValidators)
            {
                validator.Validate(_jobsters.JobstersAsync);
            }
        }
    }
}