using Orchestnation.Core.Contexts;
using Orchestnation.Core.Engines;
using System.Collections.Generic;
using System.Threading;

namespace Orchestnation.Core.Jobsters
{
    public class OperationContext<T> where T : IJobsterContext
    {
        public OperationContext(
            IOrchestnationEngine<T> engine,
            CancellationToken cancellationToken,
            IEnumerable<IJobsterAsync<T>> requiredJobsters)
        {
            RequiredJobsters = requiredJobsters;
            Engine = engine;
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }
        public IOrchestnationEngine<T> Engine { get; }

        public IEnumerable<IJobsterAsync<T>> RequiredJobsters { get; }
    }
}