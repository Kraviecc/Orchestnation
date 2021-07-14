using Orchestnation.Core.Contexts;
using Orchestnation.Core.Jobsters;
using System;
using System.Collections.Generic;

namespace Orchestnation.Core.Notifiers
{
    public interface IProgressNotifier<T> where T : IJobsterContext
    {
        void OnJobsterError(
            Exception exception,
            IJobsterAsync<T> jobsterAsync,
            JobsterProgressModel jobsterProgressModel);

        void OnJobsterFinished(
            IJobsterAsync<T> jobsterAsync,
            JobsterProgressModel jobsterProgressModel);

        void OnJobsterGroupError(
            Exception exception,
            string groupId,
            IEnumerable<IJobsterAsync<T>> jobsterAsync,
            JobsterProgressModel jobsterProgressModel);

        void OnJobsterGroupFinished(
            string groupId,
            IEnumerable<IJobsterAsync<T>> jobsterAsync,
            JobsterProgressModel jobsterProgressModel);
    }
}