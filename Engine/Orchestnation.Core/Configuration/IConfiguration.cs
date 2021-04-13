using Orchestnation.Core.Contexts;
using Orchestnation.Core.Executors;
using Orchestnation.Core.Notifiers;
using Orchestnation.Core.Validators;
using System.Collections.Generic;

namespace Orchestnation.Core.Configuration
{
    public interface IConfiguration<T> where T : IJobsterContext
    {
        int BatchSize { get; }
        IJobsterExecutor<T> JobsterExecutor { get; set; }
        IJobsterValidator<T>[] JobsterValidators { get; set; }
        IList<IProgressNotifier<T>> ProgressNotifiers { get; set; }

        void SetBatchSize(int batchSize);
    }
}