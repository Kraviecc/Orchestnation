using Orchestnation.Core.Contexts;
using Orchestnation.Core.Jobsters;
using Orchestnation.Core.Notifiers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestnation.Core.Executors
{
    public class LocalExecutor<T> : IJobsterExecutor<T> where T : IJobsterContext
    {
        public event JobsterFinished<T> JobsterFinishedEvent;

        public async Task ExecuteAsync(
            OperationContext<T> operationOperationContext,
            IEnumerable<IProgressNotifier<T>> progressNotifiers,
            JobsterProgressModel progressModel,
            IJobsterAsync<T> jobsterAsync)
        {
            try
            {
                await jobsterAsync
                    .ExecuteAsync(operationOperationContext)
                    .ConfigureAwait(false);
                if (JobsterFinishedEvent != null)
                {
                    await JobsterFinishedEvent
                        .Invoke(jobsterAsync, JobsterStatusEnum.Completed)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (JobsterFinishedEvent != null)
                {
                    await JobsterFinishedEvent
                        .Invoke(jobsterAsync, JobsterStatusEnum.Failed, ex)
                        .ConfigureAwait(false);
                }
            }

            foreach (IProgressNotifier<T> progressNotifier in progressNotifiers)
            {
                progressNotifier.OnJobsterFinished(jobsterAsync, progressModel);
            }
        }
    }
}