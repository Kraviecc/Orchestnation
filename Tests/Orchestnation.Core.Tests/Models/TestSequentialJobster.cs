using Microsoft.Extensions.Logging;
using Orchestnation.Core.Jobsters;
using System;
using System.Threading.Tasks;

namespace Orchestnation.Core.Tests.Models
{
    public class TestSequentialJobster : IJobsterAsync<CoreTestContext>
    {
        public const int MaxPageNumber = 9;

        public TestSequentialJobster(
            CoreTestContext context,
            int pageNumber,
            string pagingCookie = null)
        {
            Context = context;
            PageNumber = pageNumber;
            PagingCookie = pagingCookie;
        }

        public int PageNumber { get; }
        public string PagingCookie { get; }

        public CoreTestContext Context { get; set; }
        public string GroupId { get; set; }

        public string JobId { get; set; } = Guid.NewGuid()
            .ToString();

        public ILogger Logger { get; set; }
        public string[] RequiredJobIds { get; set; }
        public JobsterStatusEnum Status { get; set; }

        public async Task<CoreTestContext> ExecuteAsync(OperationContext<CoreTestContext> operationOperationContext)
        {
            Console.WriteLine($"Processing page number {PageNumber}.");
            Context.Increment();

            if (PageNumber < MaxPageNumber)
            {
                await operationOperationContext.Engine.AddJobsters(
                    operationOperationContext.CancellationToken,
                    GroupId,
                    new TestSequentialJobster(Context, PageNumber + 1, "random paging cookie"));
            }

            return Context;
        }
    }
}