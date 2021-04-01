using Dawn;
using Microsoft.Extensions.Logging;
using Orchestnation.Core.Configuration;
using Orchestnation.Core.Contexts;
using Orchestnation.Core.Jobsters;
using Orchestnation.Core.Models;
using Orchestnation.Core.Notifiers;
using Orchestnation.Core.StateHandlers;
using Orchestnation.Core.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestnation.Core.Engines
{
    public class BasicEngine<T> : IOrchestnationEngine<T> where T : IJobsterContext
    {
        private readonly IConfiguration<T> _configuration;
        private readonly JobsterFailureModel _jobsterFailureModel = new JobsterFailureModel();
        private readonly IJobsterStateHandler<T> _jobsterStateHandler;
        private readonly IList<Task> _jobsterTasks;
        private readonly ILogger _logger;
        private JobsterProgressModel _jobsterProgressModel;
        private Jobsters<T> _jobsters;

        public BasicEngine(
            ILogger logger,
            IList<IJobsterAsync<T>> jobstersAsync,
            IConfiguration<T> configuration,
            IJobsterStateHandler<T> jobsterStateHandler = null)
        {
            Guard.Argument(configuration)
                .NotNull()
                .Member(p => p.JobsterExecutor,
                    u => u.NotNull());

            _logger = logger;
            _configuration = configuration;
            _jobsters = new Jobsters<T>(jobstersAsync);
            _jobsterTasks = new List<Task>(jobstersAsync.Count);
            _jobsterProgressModel = new JobsterProgressModel(jobstersAsync.Count);
            _jobsterStateHandler = jobsterStateHandler;

            configuration.JobsterExecutor.JobsterFinishedEvent += OnJobsterFinished;
        }

        public async Task<IList<IJobsterAsync<T>>> ScheduleJobstersAsync(CancellationToken cancellationToken)
        {
            if (_jobsterStateHandler != null)
            {
                IEnumerable<IJobsterAsync<T>> jobsters = await _jobsterStateHandler.RestoreState();
                if (jobsters.Any(
                    p => p.Status == JobsterStatusEnum.Executing || p.Status == JobsterStatusEnum.NotStarted))
                {
                    _jobsters = new Jobsters<T>(
                        jobsters
                            .Select(
                                p =>
                                {
                                    p.Logger = _logger;
                                    return p;
                                })
                            .ToList(),
                        true);
                    _jobsterProgressModel = new JobsterProgressModel(_jobsters.JobstersAsync
                        .Count(p => p.Status == JobsterStatusEnum.NotStarted
                                    || p.Status == JobsterStatusEnum.Executing));
                    _logger.LogInformation("Previous state has been restored, resuming jobsters...");
                }
            }

            Validate();

            IEnumerable<IJobsterAsync<T>> initialJobsters = _jobsters.GetNoDependencyJobsters();
            foreach (IJobsterAsync<T> jobsterMetadata in initialJobsters)
            {
                await ThrottleJobsters();

                if (cancellationToken.IsCancellationRequested)
                    break;

                jobsterMetadata.Status = JobsterStatusEnum.Executing;
                _jobsterTasks.Add(_configuration.JobsterExecutor.ExecuteAsync(
                    jobsterMetadata,
                    null,
                    _configuration.ProgressNotifiers,
                    _jobsterProgressModel));
            }

            do
            {
                if (_jobsterFailureModel.IsError)
                {
                    _logger.LogError($"Error has been thrown. Jobster errors:\n{_jobsterFailureModel.GetErrors()}");
                    break;
                }

                await ThrottleJobsters();

                if (cancellationToken.IsCancellationRequested)
                {
                    await Task.WhenAll(_jobsterTasks);
                    _logger.LogInformation("Jobsters cancelled by the user.");
                    break;
                }

                IJobsterAsync<T> jobsterToSchedule =
                    _jobsters.JobstersAsync
                        .FirstOrDefault(p => p.Status == JobsterStatusEnum.NotStarted
                                    && !_jobsters.JobstersAsync.Any(
                                        q => p.RequiredJobIds.Contains(q.JobId)
                                             && q.Status != JobsterStatusEnum.Completed));
                if (jobsterToSchedule == null)
                {
                    await Task.WhenAll(_jobsterTasks);
                    break;
                }

                jobsterToSchedule.Status = JobsterStatusEnum.Executing;
                _jobsterTasks.Add(_configuration.JobsterExecutor.ExecuteAsync(
                    jobsterToSchedule,
                    _jobsters.JobstersAsync
                        .Where(p => jobsterToSchedule.RequiredJobIds.Contains(p.JobId))
                        .ToArray(),
                    _configuration.ProgressNotifiers,
                    _jobsterProgressModel));

                await Task.WhenAny(_jobsterTasks);
            } while (_jobsters.JobstersAsync.Any(p => p.Status == JobsterStatusEnum.NotStarted
                                                      || p.Status == JobsterStatusEnum.Executing));

            _logger.LogInformation("All jobsters completed. Job is done.");
            return _jobsters.JobstersAsync;
        }

        private async Task OnJobsterFinished(
            IJobsterAsync<T> jobsterAsync,
            JobsterStatusEnum status,
            Exception ex = null)
        {
            jobsterAsync.Status = status;
            _jobsterProgressModel.ReportJobsterFinished(status);
            await _jobsterStateHandler.PersistState(_jobsters.JobstersAsync);

            if (status != JobsterStatusEnum.Failed)
                return;

            _jobsterFailureModel.SetIsError(jobsterAsync.JobId, ex);
        }

        private async Task ThrottleJobsters()
        {
            if (_configuration.BatchSize == 0)
                return;

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