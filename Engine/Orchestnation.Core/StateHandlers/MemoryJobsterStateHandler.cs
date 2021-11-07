using Orchestnation.Core.Contexts;
using Orchestnation.Core.Jobsters;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orchestnation.Core.StateHandlers
{
    public class MemoryJobsterStateHandler<T> : IJobsterStateHandler<T> where T : IJobsterContext
    {
        private readonly object _lock = new();

        private IEnumerable<IJobsterAsync<T>> _stateReference;

        public MemoryJobsterStateHandler(IEnumerable<IJobsterAsync<T>> stateReference)
        {
            _stateReference = stateReference;
        }

        public IEnumerable<IJobsterAsync<T>> GetState()
        {
            return _stateReference;
        }

        public Task PersistState(IEnumerable<IJobsterAsync<T>> jobsters)
        {
            lock (_lock)
            {
                _stateReference = jobsters;
            }

            return Task.CompletedTask;
        }

        public Task<IJobsterAsync<T>[]> RestoreState()
        {
            return Task.FromResult(_stateReference?.ToArray());
        }
    }
}