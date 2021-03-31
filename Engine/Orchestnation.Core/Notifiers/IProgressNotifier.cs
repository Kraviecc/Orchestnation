using Orchestnation.Core.Contexts;
using Orchestnation.Core.Jobsters;

namespace Orchestnation.Core.Notifiers
{
    public interface IProgressNotifier<T> where T : IJobsterContext
    {
        void Notify(IJobsterAsync<T> jobsterAsync, JobsterProgressModel jobsterProgressModel);
    }
}