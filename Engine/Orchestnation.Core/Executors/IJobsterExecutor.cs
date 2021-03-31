using Orchestnation.Core.Contexts;
using Orchestnation.Core.Jobsters;
using Orchestnation.Core.Notifiers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestnation.Core.Executors
{
    public delegate Task JobsterFinished<T>(
        IJobsterAsync<T> jobsterAsync,
        JobsterStatusEnum status,
        Exception ex = null) where T : IJobsterContext;

    public interface IJobsterExecutor<T> where T : IJobsterContext
    {
        event JobsterFinished<T> JobsterFinishedEvent;

        Task ExecuteAsync(
            IJobsterAsync<T> jobsterAsync,
            IJobsterAsync<T>[] requiredJobsterAsync,
            IList<IProgressNotifier<T>> progressNotifiers,
            JobsterProgressModel progressModel);
    }
}