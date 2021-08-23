using Microsoft.Extensions.Logging;
using Orchestnation.Core.Jobsters;
using System;
using System.Threading.Tasks;

namespace Orchestnation.Common.Tests.Models
{
    public class TestJobster : IJobsterAsync<TestJobsterContext>
    {
        public TestJobster(TestJobsterContext context, string[] requiredJobIds = null)
        {
            Context = context;
            RequiredJobIds = requiredJobIds ?? Array.Empty<string>();
        }

        public TestJobsterContext Context { get; set; }
        public string GroupId { get; set; }

        public string JobId { get; set; } = Guid.NewGuid()
            .ToString();

        public ILogger Logger { get; set; }
        public string[] RequiredJobIds { get; set; }
        public JobsterStatusEnum Status { get; set; }

        public Task<TestJobsterContext> ExecuteAsync(IJobsterAsync<TestJobsterContext>[] requiredJobsters)
        {
            return Task.FromResult(Context);
        }
    }
}