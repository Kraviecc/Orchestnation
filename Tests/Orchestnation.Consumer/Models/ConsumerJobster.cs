using Microsoft.Extensions.Logging;
using Orchestnation.Core.Jobsters;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Orchestnation.Consumer.Models
{
    public class ConsumerJobster : IJobsterAsync<ConsumerContext>
    {
        public ConsumerJobster()
        {
        }

        public ConsumerJobster(
            ILogger<ConsumerJobster> logger,
            ConsumerContext context,
            string[] requiredJobIds = null)
        {
            Logger = logger;
            Context = context;
            RequiredJobIds = requiredJobIds ?? new string[0];
        }

        public ConsumerContext Context { get; set; }
        public string JobId { get; set; } = Guid.NewGuid().ToString();
        public ILogger Logger { get; set; }
        public string[] RequiredJobIds { get; set; }
        public JobsterStatusEnum Status { get; set; }

        public async Task<ConsumerContext> ExecuteAsync(IJobsterAsync<ConsumerContext>[] requiredJobsters)
        {
            await Task.Delay(500);
            Logger.LogDebug($"Executing Jobster with ID={JobId} in progress... Required jobsters: {string.Join(' ', RequiredJobIds)}");
            Logger.LogDebug($"First required jobster status: {requiredJobsters?.FirstOrDefault()?.Status}");
            Context.Increment();

            return Context;
        }
    }
}