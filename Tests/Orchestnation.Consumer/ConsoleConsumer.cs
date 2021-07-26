using Microsoft.Extensions.Logging;
using Orchestnation.Consumer.Models;
using Orchestnation.Core.Configuration;
using Orchestnation.Core.Engines;
using Orchestnation.Core.Jobsters;
using Orchestnation.Core.Notifiers;
using Orchestnation.Core.StateHandlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestnation.Consumer
{
    internal class ConsoleConsumer
    {
        private static async Task Main(string[] args)
        {
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Debug)
                    .AddConsole();
            });
            ILogger logger = loggerFactory.CreateLogger<ConsoleConsumer>();
            ILogger<ConsumerJobster> jobsterLogger = loggerFactory.CreateLogger<ConsumerJobster>();

            LocalEventProgressNotifier<ConsumerContext> progressNotifier = new LocalEventProgressNotifier<ConsumerContext>();
            progressNotifier.OnJobsterFinishedNotifyEvent += (jobster, progress) =>
                logger.LogInformation($"Jobster with ID={jobster.JobId} has finished. Current progress: {progress.Completed}/{progress.All}");

            ConsumerContext consumerContext = new ConsumerContext();
            IList<IJobsterAsync<ConsumerContext>> jobsters = new List<IJobsterAsync<ConsumerContext>>(100);
            for (int i = 0; i < 100; i++)
            {
                jobsters.Add(new ConsumerJobster(
                    jobsterLogger,
                    consumerContext));
            }

            IOrchestnationEngine<ConsumerContext> jobsterEngine = new JobsterBuilder<ConsumerContext>(logger)
                .AddBatchSize(10)
                .AddJobsters(null, jobsters.ToArray())
                .AddProgressNotifier(progressNotifier)
                .AddStateHandler(new FileJobsterStateHandler<ConsumerContext>(@"saved_state.json"))
                .BuildEngine();

            CancellationTokenSource cancellationToken = new CancellationTokenSource();
            IList<IJobsterAsync<ConsumerContext>> resultJobsters = await jobsterEngine.ScheduleJobstersAsync(cancellationToken.Token);

            logger.LogInformation($"Finished, result: {resultJobsters.First().Context.Counter}");
            Console.ReadKey();
        }
    }
}