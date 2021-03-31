using Microsoft.Extensions.Logging;
using Orchestnation.Core.Contexts;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Orchestnation.Core.Jobsters
{
    public interface IJobsterAsync<T> where T : IJobsterContext
    {
        T Context { get; set; }
        string JobId { get; set; }
        [JsonIgnore]
        ILogger Logger { get; set; }
        string[] RequiredJobIds { get; set; }
        JobsterStatusEnum Status { get; set; }

        Task<T> ExecuteAsync(IJobsterAsync<T>[] requiredJobsters);
    }
}