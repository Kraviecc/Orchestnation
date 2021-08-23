using Microsoft.Extensions.Logging;
using Orchestnation.Core.Jobsters;
using System;
using System.Threading.Tasks;

namespace Orchestnation.Core.Tests.Models
{
    public class TestJobster : IJobsterAsync<CoreTestContext>
    {
        private readonly int? _longRunningTimeout;
        private readonly bool _throwException;

        public TestJobster(
            CoreTestContext context,
            bool throwException = false,
            string[] requiredJobIds = null,
            int? longRunningTimeoutTimeout = null)
        {
            _longRunningTimeout = longRunningTimeoutTimeout;
            _throwException = throwException;
            Context = context;
            RequiredJobIds = requiredJobIds ?? Array.Empty<string>();
        }

        public CoreTestContext Context { get; set; }
        public string GroupId { get; set; }

        public string JobId { get; set; } = Guid.NewGuid()
            .ToString();

        public ILogger Logger { get; set; }
        public string[] RequiredJobIds { get; set; }
        public JobsterStatusEnum Status { get; set; }

        public async Task<CoreTestContext> ExecuteAsync(
            IJobsterAsync<CoreTestContext>[] requiredJobsters)
        {
            if (_throwException)
            {
                throw new Exception("Exception from jobster");
            }

            if (_longRunningTimeout.HasValue)
            {
                await Task.Delay(_longRunningTimeout.Value);
            }

            Context.Increment();

            return Context;
        }
    }
}