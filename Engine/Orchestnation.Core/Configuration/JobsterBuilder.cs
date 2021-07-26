using Dawn;
using Microsoft.Extensions.Logging;
using Orchestnation.Core.Contexts;
using Orchestnation.Core.Engines;
using Orchestnation.Core.Executors;
using Orchestnation.Core.Jobsters;
using Orchestnation.Core.Models;
using Orchestnation.Core.Notifiers;
using Orchestnation.Core.StateHandlers;
using Orchestnation.Core.Validators;

namespace Orchestnation.Core.Configuration
{
    public class JobsterBuilder<T> where T : IJobsterContext
    {
        private readonly IConfiguration<T> _configuration;
        private readonly JobsterManager<T> _jobsterManager = new JobsterManager<T>();
        private readonly ILogger _logger;
        private IJobsterStateHandler<T> _jobsterStateHandler;

        public JobsterBuilder(ILogger logger)
        {
            _logger = logger;
            _configuration = new BasicConfiguration<T>(
                new LocalExecutor<T>(),
                new IJobsterValidator<T>[]
                {
                    new CircularDependencyValidator<T>(_logger)
                });
        }

        public JobsterBuilder<T> AddBatchSize(int batchSize)
        {
            _configuration.SetBatchSize(batchSize);

            return this;
        }

        public JobsterBuilder<T> AddExceptionPolicy(ExceptionPolicy exceptionPolicy)
        {
            _configuration.ExceptionPolicy = exceptionPolicy;

            return this;
        }

        public JobsterBuilder<T> AddJobsters(
            string groupId,
            params IJobsterAsync<T>[] jobsterAsync)
        {
            _jobsterManager.AddJobsters(OrchestnationStatus.Builder, groupId, jobsterAsync);

            return this;
        }

        public JobsterBuilder<T> AddProgressNotifier(IProgressNotifier<T> progressNotifier)
        {
            Guard.Argument(progressNotifier, nameof(progressNotifier)).NotNull();

            _configuration.ProgressNotifiers.Add(progressNotifier);

            return this;
        }

        public JobsterBuilder<T> AddStateHandler(IJobsterStateHandler<T> jobsterStateHandler)
        {
            Guard.Argument(jobsterStateHandler, nameof(jobsterStateHandler)).NotNull();

            _jobsterStateHandler = jobsterStateHandler;

            return this;
        }

        public IOrchestnationEngine<T> BuildEngine()
        {
            _logger.LogInformation("Building basic engine...");
            return new BasicEngine<T>(
                _logger,
                _jobsterManager,
                _configuration,
                _jobsterStateHandler);
        }

        public int GetJobstersNumber()
        {
            return _jobsterManager.GetJobstersNumber();
        }
    }
}