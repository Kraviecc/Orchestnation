using Microsoft.Extensions.Logging;
using Orchestnation.Core.Jobsters;
using System;
using System.Threading.Tasks;

namespace Orchestnation.Core.Tests.Models
{
    public class TestJobster : IJobsterAsync<CoreTestContext>
    {
        public TestJobster(CoreTestContext context, string[] requiredJobIds = null)
        {
            Context = context;
            RequiredJobIds = requiredJobIds ?? new string[0];
        }

        public CoreTestContext Context { get; set; }
        public string JobId { get; set; } = Guid.NewGuid().ToString();
        public ILogger Logger { get; set; }
        public string[] RequiredJobIds { get; set; }
        public JobsterStatusEnum Status { get; set; }

        public Task<CoreTestContext> ExecuteAsync(IJobsterAsync<CoreTestContext>[] requiredJobsters)
        {
            return Task.FromResult(Context);
        }
    }
}