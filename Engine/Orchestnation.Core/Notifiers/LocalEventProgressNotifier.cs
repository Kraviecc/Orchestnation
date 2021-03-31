using Orchestnation.Core.Contexts;
using Orchestnation.Core.Jobsters;

namespace Orchestnation.Core.Notifiers
{
    public delegate void Notify<T>(IJobsterAsync<T> jobster, JobsterProgressModel progressModel) where T : IJobsterContext;

    public class LocalEventProgressNotifier<T> : IProgressNotifier<T> where T : IJobsterContext
    {
        public event Notify<T> NotifyEvent;

        public void Notify(IJobsterAsync<T> jobsterAsync, JobsterProgressModel jobsterProgressModel)
        {
            NotifyEvent?.Invoke(jobsterAsync, jobsterProgressModel);
        }
    }
}