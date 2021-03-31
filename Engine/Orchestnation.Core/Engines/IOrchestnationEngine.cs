using Orchestnation.Core.Contexts;
using Orchestnation.Core.Jobsters;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestnation.Core.Engines
{
    public interface IOrchestnationEngine<T> where T : IJobsterContext
    {
        Task<IList<IJobsterAsync<T>>> ScheduleJobstersAsync(CancellationToken cancellationToken);
    }
}