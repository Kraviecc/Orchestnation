using Orchestnation.Core.Contexts;
using Orchestnation.Core.Jobsters;
using System;
using System.Collections.Generic;

namespace Orchestnation.Core.Notifiers
{
    public delegate void Notify<T>(IJobsterAsync<T> jobster, JobsterProgressModel progressModel)
        where T : IJobsterContext;

    public delegate void NotifyError<T>(Exception ex, IJobsterAsync<T> jobster, JobsterProgressModel progressModel)
        where T : IJobsterContext;

    public delegate void NotifyGroup<T>(
        string groupId, IEnumerable<IJobsterAsync<T>> jobster, JobsterProgressModel progressModel)
        where T : IJobsterContext;

    public delegate void NotifyGroupError<T>(
        Exception ex, string groupId, IEnumerable<IJobsterAsync<T>> jobster, JobsterProgressModel progressModel)
        where T : IJobsterContext;

    public class LocalEventProgressNotifier<T> : IProgressNotifier<T> where T : IJobsterContext
    {
        public void OnJobsterError(
            Exception exception, IJobsterAsync<T> jobsterAsync, JobsterProgressModel jobsterProgressModel)
        {
            OnJobsterErrorNotifyEvent?.Invoke(
                exception,
                jobsterAsync,
                jobsterProgressModel);
        }

        public void OnJobsterFinished(IJobsterAsync<T> jobsterAsync, JobsterProgressModel jobsterProgressModel)
        {
            OnJobsterFinishedNotifyEvent?.Invoke(jobsterAsync, jobsterProgressModel);
        }

        public void OnJobsterGroupError(
            Exception exception, string groupId, IEnumerable<IJobsterAsync<T>> jobsterAsync,
            JobsterProgressModel jobsterProgressModel)
        {
            OnJobsterGroupErrorNotifyEvent?.Invoke(
                exception,
                groupId,
                jobsterAsync,
                jobsterProgressModel);
        }

        public void OnJobsterGroupFinished(
            string groupId, IEnumerable<IJobsterAsync<T>> jobsterAsync, JobsterProgressModel jobsterProgressModel)
        {
            OnJobsterGroupFinishedNotifyEvent?.Invoke(
                groupId,
                jobsterAsync,
                jobsterProgressModel);
        }

        public event NotifyError<T> OnJobsterErrorNotifyEvent;

        public event Notify<T> OnJobsterFinishedNotifyEvent;

        public event NotifyGroupError<T> OnJobsterGroupErrorNotifyEvent;

        public event NotifyGroup<T> OnJobsterGroupFinishedNotifyEvent;
    }
}