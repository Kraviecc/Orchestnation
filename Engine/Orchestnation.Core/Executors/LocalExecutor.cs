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
            IJobsterAsync<T> jobsterAsync,
            IJobsterAsync<T>[] requiredJobsterAsync,
            IList<IProgressNotifier<T>> progressNotifiers,
            JobsterProgressModel progressModel)
        {
            try
            {
                await jobsterAsync.ExecuteAsync(requiredJobsterAsync);
                if (JobsterFinishedEvent != null)
                    await JobsterFinishedEvent?.Invoke(jobsterAsync, JobsterStatusEnum.Completed);
            }
            catch (Exception ex)
            {
                if (JobsterFinishedEvent != null)
                    await JobsterFinishedEvent?.Invoke(jobsterAsync, JobsterStatusEnum.Failed, ex);
            }

            foreach (IProgressNotifier<T> progressNotifier in progressNotifiers)
            {
                progressNotifier.Notify(jobsterAsync, progressModel);
            }
        }
    }
}