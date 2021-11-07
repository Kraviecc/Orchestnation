using Microsoft.Extensions.Logging;
using Orchestnation.Core.Configuration;
using Orchestnation.Core.Jobsters;
using Orchestnation.Core.Models;
using Orchestnation.Core.StateHandlers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestnation.Core.Tests.Models
{
    public class TestSubOrchestratorJobster : IJobsterAsync<CoreTestContext>
    {
        public TestSubOrchestratorJobster(
            CoreTestContext context,
            CancellationToken cancellationToken,
            IEnumerable<IJobsterAsync<CoreTestContext>> state,
            ExceptionPolicy exceptionPolicy,
            int batchSize,
            IEnumerable<IJobsterAsync<CoreTestContext>> jobsters, ILogger mockLogger)
        {
            Context = context;
            CancellationToken = cancellationToken;
            State = state;
            ExceptionPolicy = exceptionPolicy;
            BatchSize = batchSize;
            Jobsters = jobsters;
            MockLogger = mockLogger;
        }

        public CoreTestContext Context { get; }

        public string JobId { get; } = Guid.NewGuid()
            .ToString();

        public string GroupId { get; set; }

        public ILogger Logger { get; set; }
        public string[] RequiredJobIds { get; set; }
        public JobsterStatusEnum Status { get; set; }

        private int BatchSize { get; }

        private CancellationToken CancellationToken { get; }

        private ExceptionPolicy ExceptionPolicy { get; }

        private IEnumerable<IJobsterAsync<CoreTestContext>> Jobsters { get; }

        private ILogger MockLogger { get; }

        private IEnumerable<IJobsterAsync<CoreTestContext>> State { get; }

        public async Task<CoreTestContext> ExecuteAsync(OperationContext<CoreTestContext> operationOperationContext)
        {
            Context.Increment();

            JobsterBuilder<CoreTestContext> builder = new JobsterBuilder<CoreTestContext>(MockLogger)
                .AddBatchSize(BatchSize)
                .AddExceptionPolicy(ExceptionPolicy)
                .AddStateHandler(new MemoryJobsterStateHandler<CoreTestContext>(State));

            _ = await builder
                .BuildEngine()
                .ScheduleJobstersAsync(CancellationToken);

            return Context;
        }
    }
}