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
            JobsterExecutor = jobsterExecutor;
            JobsterValidators = jobsterValidators;
            ProgressNotifiers = new List<IProgressNotifier<T>>();
        }

        public int BatchSize { get; set; }
        public IJobsterExecutor<T> JobsterExecutor { get; set; }
        public IJobsterValidator<T>[] JobsterValidators { get; set; }
        public IList<IProgressNotifier<T>> ProgressNotifiers { get; set; }
    }
}