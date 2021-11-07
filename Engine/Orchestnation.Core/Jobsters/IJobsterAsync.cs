using Microsoft.Extensions.Logging;
using Orchestnation.Core.Contexts;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Orchestnation.Core.Jobsters
{
    public interface IJobsterAsync<T> where T : IJobsterContext
    {
        T Context { get; }
        string JobId { get; }

        string[] RequiredJobIds { get; }
        string GroupId { get; set; }

        [JsonIgnore] ILogger Logger { get; set; }
        JobsterStatusEnum Status { get; set; }

        Task<T> ExecuteAsync(OperationContext<T> operationOperationContext);
    }
}