using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Orchestnation.Common.Exceptions;
using Orchestnation.Core.Configuration;
using Orchestnation.Core.Engines;
using Orchestnation.Core.Executors;
using Orchestnation.Core.Jobsters;
using Orchestnation.Core.Models;
using Orchestnation.Core.Notifiers;
using Orchestnation.Core.StateHandlers;
using Orchestnation.Core.Tests.Models;
using Orchestnation.Core.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestnation.Core.Tests.Engines
{
    public class BasicEngineTests
    {
        private CancellationTokenSource _cancellationTokenSource;
        private ILogger _mockLogger;

        [Test]
        public async Task Engines_BasicEngine_ShouldCancelExecution()
        {
            const int jobsterCount = 100;
            _cancellationTokenSource.Cancel();

            CoreTestContext context = await ExecuteOrchestrator(jobsterCount, 1);

            Assert.AreEqual(0, context.Counter);
        }

        [Test]
        public async Task Engines_BasicEngine_ShouldExecuteAdHocJobsters()
        {
            const int jobsterCount = 100;
            const int adHocJobsterCount = 50;
            CoreTestContext context = new CoreTestContext();
            IOrchestnationEngine<CoreTestContext> engine = new JobsterBuilder<CoreTestContext>(_mockLogger)
                .AddBatchSize(25)
                .AddExceptionPolicy(ExceptionPolicy.ThrowImmediately)
                .AddStateHandler(new MemoryJobsterStateHandler<CoreTestContext>(
                    new IJobsterAsync<CoreTestContext>[0]))
                .AddJobsters(null, CreateLongRunningJobsters(jobsterCount, context).ToArray())
                .BuildEngine();

            Task<IList<IJobsterAsync<CoreTestContext>>> tasks = engine
                .ScheduleJobstersAsync(_cancellationTokenSource.Token);

            Task<IList<IJobsterAsync<CoreTestContext>>> allTasks = engine
                .AddJobsters(
                    _cancellationTokenSource.Token,
                    null,
                    CreateLongRunningJobsters(adHocJobsterCount, context).ToArray());
            await Task.WhenAll(tasks, allTasks);

            const int allJobsters = jobsterCount + adHocJobsterCount;
            Assert.AreEqual(allJobsters, allTasks.Result.Count);
            Assert.IsFalse(allTasks.Result
                .Any(p => p.Status == JobsterStatusEnum.Executing));
            Assert.IsFalse(allTasks.Result
                .Any(p => p.Status == JobsterStatusEnum.NotStarted));
            Assert.AreEqual(allJobsters, context.Counter);
        }

        [Test]
        [TestCase(true, TestName = "More jobsters than batch size")]
        [TestCase(false, TestName = "Less jobsters than batch size")]
        public async Task Engines_BasicEngine_ShouldExecuteAllJobsters(
            bool moreThanBatchSize)
        {
            const int batchSize = 3;
            int jobsterCount = moreThanBatchSize ? 5 : 2;

            CoreTestContext context = await ExecuteOrchestrator(jobsterCount, batchSize);

            Assert.AreEqual(jobsterCount, context.Counter);
        }

        [Test]
        public void Engines_BasicEngine_ShouldImmediatelyThrowException()
        {
            const int jobsterCount = 100;

            Assert.ThrowsAsync<JobsterException>(async () => await ExecuteOrchestrator(
                jobsterCount,
                1,
                true,
                ExceptionPolicy.ThrowImmediately));
        }

        [Test]
        public async Task Engines_BasicEngine_ShouldNotRestoreCompletedState()
        {
            const int jobsterCount = 100;
            CoreTestContext context = new CoreTestContext();

            IJobsterAsync<CoreTestContext>[] completedState =
            {
                new TestJobster(context)
                {
                    Status = JobsterStatusEnum.Completed
                },
                new TestJobster(context)
                {
                    Status = JobsterStatusEnum.Completed
                }
            };

            CoreTestContext newContext = await ExecuteOrchestrator(
                jobsterCount,
                jobsterCount,
                false,
                ExceptionPolicy.NoThrow,
                null,
                completedState);

            Assert.AreEqual(jobsterCount, newContext.Counter);
        }

        [Test]
        public async Task Engines_BasicEngine_ShouldNotThrowException()
        {
            const int jobsterCount = 100;

            CoreTestContext context = await ExecuteOrchestrator(
                jobsterCount,
                1,
                true,
                ExceptionPolicy.NoThrow);

            Assert.AreEqual(jobsterCount - 1, context.Counter);
        }

        [Test]
        public async Task Engines_BasicEngine_ShouldReportErrorProgress()
        {
            bool wasNotified = false;
            bool wasGroupNotified = false;
            const int jobsterCount = 100;
            LocalEventProgressNotifier<CoreTestContext> progressNotifier = new LocalEventProgressNotifier<CoreTestContext>();
            progressNotifier.OnJobsterErrorNotifyEvent += (ex, jobster, progress) => wasNotified = true;
            progressNotifier.OnJobsterGroupErrorNotifyEvent += (ex, groupId, jobsters, progress) => wasGroupNotified = true;

            _ = await ExecuteOrchestrator(
                jobsterCount,
                jobsterCount,
                true,
                ExceptionPolicy.NoThrow,
                null,
                null,
                null,
                progressNotifier);

            Assert.IsTrue(wasNotified);
            Assert.IsTrue(wasGroupNotified);
        }

        [Test]
        public async Task Engines_BasicEngine_ShouldReportProgress()
        {
            bool wasNotified = false;
            bool wasGroupNotified = false;
            const int jobsterCount = 100;
            LocalEventProgressNotifier<CoreTestContext> progressNotifier = new LocalEventProgressNotifier<CoreTestContext>();
            progressNotifier.OnJobsterFinishedNotifyEvent += (jobster, progress) => wasNotified = true;
            progressNotifier.OnJobsterGroupFinishedNotifyEvent += (groupId, jobsters, progress) => wasGroupNotified = true;

            _ = await ExecuteOrchestrator(
                jobsterCount,
                jobsterCount,
                false,
                ExceptionPolicy.NoThrow,
                null,
                null,
                null,
                progressNotifier);

            Assert.IsTrue(wasNotified);
            Assert.IsTrue(wasGroupNotified);
        }

        [Test]
        public async Task Engines_BasicEngine_ShouldRestorePartiallyCompletedState()
        {
            const int jobsterCount = 100;
            CoreTestContext context = new CoreTestContext();
            TestJobster initialJobster = new TestJobster(context)
            {
                Status = JobsterStatusEnum.Completed
            };
            IJobsterAsync<CoreTestContext>[] completedState =
            {
                initialJobster,
                new TestJobster(
                    context,
                    false,
                    new []{ initialJobster.JobId })
                {
                    Status = JobsterStatusEnum.NotStarted
                }
            };

            _ = await ExecuteOrchestrator(
                jobsterCount,
                jobsterCount,
                false,
                ExceptionPolicy.ThrowImmediately,
                null,
                completedState);

            Assert.AreEqual(1, context.Counter);
        }

        [Test]
        public async Task Engines_BasicEngine_ShouldSaveState()
        {
            const int jobsterCount = 100;
            MemoryJobsterStateHandler<CoreTestContext> stateHandler = new MemoryJobsterStateHandler<CoreTestContext>(
                new IJobsterAsync<CoreTestContext>[0]);

            _ = await ExecuteOrchestrator(
                jobsterCount,
                jobsterCount,
                false,
                ExceptionPolicy.NoThrow,
                null,
                null,
                stateHandler);

            IJobsterAsync<CoreTestContext>[] stateArray = stateHandler.GetState()?.ToArray();
            Assert.IsNotNull(stateArray);
            Assert.AreEqual(jobsterCount, stateArray.Length);
            Assert.IsEmpty(stateArray
                .Where(p => p.Status != JobsterStatusEnum.Completed));
        }

        [Test]
        public void Engines_BasicEngine_ShouldThrowExceptionAtTheEnd()
        {
            const int jobsterCount = 100;
            CoreTestContext context = new CoreTestContext();

            Assert.ThrowsAsync<JobsterException>(async () => await ExecuteOrchestrator(
                jobsterCount,
                1,
                true,
                ExceptionPolicy.ThrowAtTheEnd,
                context));
            Assert.AreEqual(jobsterCount - 1, context.Counter);
        }

        [Test]
        public void Engines_BasicEngine_ShouldThrowExceptionWhenNullConfiguration()
        {
            Assert.Throws<ArgumentNullException>(() => new BasicEngine<CoreTestContext>(
                _mockLogger,
                new JobsterManager<CoreTestContext>(),
                null));
        }

        [Test]
        public void Engines_BasicEngine_ShouldThrowExceptionWhenNullJobsterExecutor()
        {
            Assert.Throws<ArgumentNullException>(() => new BasicEngine<CoreTestContext>(
                _mockLogger,
                new JobsterManager<CoreTestContext>(),
                new BasicConfiguration<CoreTestContext>(
                    null,
                    new IJobsterValidator<CoreTestContext>[0])));
        }

        [Test]
        public void Engines_BasicEngine_ShouldThrowExceptionWhenNullJobsters()
        {
            Assert.Throws<ArgumentNullException>(() => new BasicEngine<CoreTestContext>(
                _mockLogger,
                null,
                new BasicConfiguration<CoreTestContext>(
                    new LocalExecutor<CoreTestContext>(),
                    new IJobsterValidator<CoreTestContext>[0])));
        }

        [Test]
        public void Engines_BasicEngine_ShouldValidateCyclicDependencies()
        {
            var rootJobster = new TestJobster(new CoreTestContext());
            var d = new TestJobster(new CoreTestContext(), false, new string[0]);
            var b = new TestJobster(new CoreTestContext(), false, new[] { rootJobster.JobId, d.JobId });
            var c = new TestJobster(new CoreTestContext(), false, new[] { b.JobId });
            d.RequiredJobIds = new[] { rootJobster.JobId, c.JobId };

            List<IJobsterAsync<CoreTestContext>> jobsters = new List<IJobsterAsync<CoreTestContext>>
            {
                rootJobster, d, b, c
            };

            Assert.ThrowsAsync<CircularDependencyException>(async () => await new JobsterBuilder<CoreTestContext>(_mockLogger)
                .AddJobsters(null, jobsters.ToArray())
                .BuildEngine()
                .ScheduleJobstersAsync(_cancellationTokenSource.Token));
        }

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger>().Object;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        private static IEnumerable<IJobsterAsync<CoreTestContext>> CreateLongRunningJobsters(
            int jobsterCount,
            CoreTestContext context)
        {
            IList<IJobsterAsync<CoreTestContext>> jobsters = new List<IJobsterAsync<CoreTestContext>>(jobsterCount);
            for (int i = 0; i < jobsterCount; i++)
            {
                jobsters.Add(new TestJobster(context, false, null, true));
            }

            return jobsters;
        }

        private async Task<CoreTestContext> ExecuteOrchestrator(
            int jobsterCount,
            int batchSize,
            bool jobsterThrowsExceptionInTheMiddle = false,
            ExceptionPolicy exceptionPolicy = ExceptionPolicy.NoThrow,
            CoreTestContext context = null,
            IEnumerable<IJobsterAsync<CoreTestContext>> state = null,
            MemoryJobsterStateHandler<CoreTestContext> stateHandler = null,
            IProgressNotifier<CoreTestContext> progressNotifier = null)
        {
            context ??= new CoreTestContext();
            int middleJobster = jobsterCount / 2;

            IList<IJobsterAsync<CoreTestContext>> jobsters = new List<IJobsterAsync<CoreTestContext>>(jobsterCount);
            for (int i = 0; i < jobsterCount; i++)
            {
                jobsters.Add(new TestJobster(
                    context,
                    jobsterThrowsExceptionInTheMiddle && i == middleJobster));
            }

            JobsterBuilder<CoreTestContext> builder = new JobsterBuilder<CoreTestContext>(_mockLogger)
               .AddBatchSize(batchSize)
               .AddExceptionPolicy(exceptionPolicy)
               .AddJobsters(null, jobsters.ToArray())
               .AddStateHandler(stateHandler ?? new MemoryJobsterStateHandler<CoreTestContext>(
                   state ?? new IJobsterAsync<CoreTestContext>[0]));
            if (progressNotifier != null)
                builder.AddProgressNotifier(progressNotifier);

            _ = await builder
                .BuildEngine()
                .ScheduleJobstersAsync(_cancellationTokenSource.Token);

            return context;
        }
    }
}