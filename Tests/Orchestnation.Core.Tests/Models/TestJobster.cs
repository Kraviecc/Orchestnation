using Microsoft.Extensions.Logging;
using Orchestnation.Core.Jobsters;
using System;
using System.Threading.Tasks;

namespace Orchestnation.Core.Tests.Models
{
    public class TestJobster : IJobsterAsync<CoreTestContext>
    {
        private const int Timeout = 500;
        private readonly bool _longRunning;
        private readonly bool _throwException;

        public TestJobster(
            CoreTestContext context,
            bool throwException = false,
            string[] requiredJobIds = null,
            bool longRunning = false)
        {
            _longRunning = longRunning;
            _throwException = throwException;
            Context = context;
            RequiredJobIds = requiredJobIds ?? new string[0];
        }

        public CoreTestContext Context { get; set; }
        public string GroupId { get; set; }
        public string JobId { get; set; } = Guid.NewGuid().ToString();
        public ILogger Logger { get; set; }
        public string[] RequiredJobIds { get; set; }
        public JobsterStatusEnum Status { get; set; }

        public async Task<CoreTestContext> ExecuteAsync(
            IJobsterAsync<CoreTestContext>[] requiredJobsters)
        {
            if (_throwException)
                throw new Exception("Exception from jobster");

            if (_longRunning)
                await Task.Delay(Timeout);

            Context.Increment();

            return Context;
        }
    }
}