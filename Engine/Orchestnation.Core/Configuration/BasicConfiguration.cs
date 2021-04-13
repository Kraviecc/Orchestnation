using Dawn;
using Orchestnation.Core.Contexts;
using Orchestnation.Core.Executors;
using Orchestnation.Core.Notifiers;
using Orchestnation.Core.Validators;
using System.Collections.Generic;

namespace Orchestnation.Core.Configuration
{
    public class BasicConfiguration<T> : IConfiguration<T> where T : IJobsterContext
    {
        public BasicConfiguration(
            IJobsterExecutor<T> jobsterExecutor,
            IJobsterValidator<T>[] jobsterValidators)
        {
            Guard.Argument(jobsterExecutor)
                .NotNull();
            Guard.Argument(jobsterValidators)
                .NotNull();

            JobsterExecutor = jobsterExecutor;
            JobsterValidators = jobsterValidators;
            ProgressNotifiers = new List<IProgressNotifier<T>>();
        }

        public int BatchSize { get; internal set; }

        public IJobsterExecutor<T> JobsterExecutor { get; set; }

        public IJobsterValidator<T>[] JobsterValidators { get; set; }

        public IList<IProgressNotifier<T>> ProgressNotifiers { get; set; }

        public void SetBatchSize(int batchSize)
        {
            Guard.Argument(batchSize)
                .NotZero("Batch size cannot be 0")
                .NotNegative(_ => "Batch size cannot be negative");

            BatchSize = batchSize;
        }
    }
}