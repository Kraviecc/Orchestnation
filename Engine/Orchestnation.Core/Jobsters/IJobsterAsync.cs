using Microsoft.Extensions.Logging;
using Orchestnation.Core.Contexts;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Orchestnation.Core.Jobsters
{
    public interface IJobsterAsync<T> where T : IJobsterContext
    {
        T Context { get; }
        string GroupId { get; set; }
        string JobId { get; }

        [JsonIgnore] ILogger Logger { get; set; }

        string[] RequiredJobIds { get; }
        JobsterStatusEnum Status { get; set; }

        Task<T> ExecuteAsync(IJobsterAsync<T>[] requiredJobsters);
    }
}