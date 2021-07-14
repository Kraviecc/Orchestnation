using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orchestnation.Core.Contexts;
using System.Threading.Tasks;

namespace Orchestnation.Core.Jobsters
{
    public interface IJobsterAsync<T> where T : IJobsterContext
    {
        T Context { get; set; }
        string GroupId { get; set; }
        string JobId { get; set; }

        [JsonIgnore]
        ILogger Logger { get; set; }

        string[] RequiredJobIds { get; set; }
        JobsterStatusEnum Status { get; set; }

        Task<T> ExecuteAsync(IJobsterAsync<T>[] requiredJobsters);
    }
}