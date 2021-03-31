using Orchestnation.Core.Contexts;
using Orchestnation.Core.Jobsters;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orchestnation.Core.StateHandlers
{
    public interface IJobsterStateHandler<T> where T : IJobsterContext
    {
        Task PersistState(IEnumerable<IJobsterAsync<T>> jobsters);

        Task<IJobsterAsync<T>[]> RestoreState();
    }
}