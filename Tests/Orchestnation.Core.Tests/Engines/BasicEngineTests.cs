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
        [TestCase(true, TestName = "AdHoc run immediately")]
        [TestCase(false, TestName = "AdHoc run after initial are finished")]
        public async Task Engines_BasicEngine_ShouldExecuteAdHocJobsters(
            bool runImmediately)
        {
            const int jobsterCount = 100;
            const int adHocJobsterCount = 50;
            const int jobsterTimeout = 500;
            CoreTestContext context = new();
            IOrchestnationEngine<CoreTestContext> engine = new JobsterBuilder<CoreTestContext>(_mockLogger)
                .AddBatchSize(25)
                .AddExceptionPolicy(ExceptionPolicy.ThrowImmediately)
                .AddStateHandler(
                    new MemoryJobsterStateHandler<CoreTestContext>(
                        Array.Empty<IJobsterAsync<CoreTestContext>>()))
                .AddJobsters(
                    null, CreateLongRunningJobsters(jobsterCount, context, jobsterTimeout)
                        .ToArray())
                .BuildEngine();

            Task<IList<IJobsterAsync<CoreTestContext>>> tasks = engine
                .ScheduleJobstersAsync(_cancellationTokenSource.Token);

            if (!runImmediately)
            {
                await Task.WhenAll(tasks);
            }

            Task<IList<IJobsterAsync<CoreTestContext>>> allTasks = engine
                .AddJobsters(
                    _cancellationTokenSource.Token,
                    null,
                    CreateLongRunningJobsters(adHocJobsterCount, context, jobsterTimeout)
                        .ToArray());
            await Task.WhenAll(tasks, allTasks);

            const int allJobsters = jobsterCount + adHocJobsterCount;
            Assert.AreEqual(allJobsters, allTasks.Result.Count);
            Assert.IsFalse(
                allTasks.Result
                    .Any(p => p.Status == JobsterStatusEnum.Executing));
            Assert.IsFalse(
                allTasks.Result
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

            Assert.ThrowsAsync<JobsterException>(
                async () => await ExecuteOrchestrator(
                    jobsterCount,
                    1,
                    true,
                    ExceptionPolicy.ThrowImmediately));
        }

        [Test]
        public async Task Engines_BasicEngine_ShouldNotRestoreCompletedState()
        {
            const int jobsterCount = 100;
            CoreTestContext context = new();

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
                true);

            Assert.AreEqual(jobsterCount - 1, context.Counter);
        }

        [Test]
        public async Task Engines_BasicEngine_ShouldProcessSequentialJobsters()
        {
            CoreTestContext result = await ExecuteSequentialOrchestrator(
                1,
                new CoreTestContext());

            Assert.AreEqual(TestSequentialJobster.MaxPageNumber + 1, result.Counter);
        }

        [Test]
        public async Task Engines_BasicEngine_ShouldReportErrorProgress()
        {
            bool wasNotified = false;
            bool wasGroupNotified = false;
            const int jobsterCount = 100;
            LocalEventProgressNotifier<CoreTestContext> progressNotifier =
                new();
            progressNotifier.OnJobsterErrorNotifyEvent += (ex, jobster, progress) => wasNotified = true;
            progressNotifier.OnJobsterGroupErrorNotifyEvent +=
                (ex, groupId, jobsters, progress) => wasGroupNotified = true;

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
            LocalEventProgressNotifier<CoreTestContext> progressNotifier =
                new();
            progressNotifier.OnJobsterFinishedNotifyEvent += (jobster, progress) => wasNotified = true;
            progressNotifier.OnJobsterGroupFinishedNotifyEvent +=
                (groupId, jobsters, progress) => wasGroupNotified = true;

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
            CoreTestContext context = new();
            TestJobster initialJobster = new(context)
            {
                Status = JobsterStatusEnum.Completed
            };
            IJobsterAsync<CoreTestContext>[] completedState =
            {
                initialJobster,
                new TestJobster(
                    context,
                    false,
                    new[] { initialJobster.JobId })
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
            MemoryJobsterStateHandler<CoreTestContext> stateHandler = new(
                Array.Empty<IJobsterAsync<CoreTestContext>>());

            _ = await ExecuteOrchestrator(
                jobsterCount,
                jobsterCount,
                false,
                ExceptionPolicy.NoThrow,
                null,
                null,
                stateHandler);

            IJobsterAsync<CoreTestContext>[] stateArray = stateHandler.GetState()
                ?.ToArray();
            Assert.IsNotNull(stateArray);
            Assert.AreEqual(jobsterCount, stateArray.Length);
            Assert.IsEmpty(
                stateArray
                    .Where(p => p.Status != JobsterStatusEnum.Completed));
        }

        [Test]
        public void Engines_BasicEngine_ShouldThrowExceptionAtTheEnd()
        {
            const int jobsterCount = 100;
            CoreTestContext context = new();

            Assert.ThrowsAsync<JobsterException>(
                async () => await ExecuteOrchestrator(
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
            Assert.Throws<ArgumentNullException>(
                () => new BasicEngine<CoreTestContext>(
                    _mockLogger,
                    new JobsterManager<CoreTestContext>(),
                    null));
        }

        [Test]
        public void Engines_BasicEngine_ShouldThrowExceptionWhenNullJobsterExecutor()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BasicEngine<CoreTestContext>(
                    _mockLogger,
                    new JobsterManager<CoreTestContext>(),
                    new BasicConfiguration<CoreTestContext>(
                        null,
                        Array.Empty<IJobsterValidator<CoreTestContext>>())));
        }

        [Test]
        public void Engines_BasicEngine_ShouldThrowExceptionWhenNullJobsters()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BasicEngine<CoreTestContext>(
                    _mockLogger,
                    null,
                    new BasicConfiguration<CoreTestContext>(
                        new LocalExecutor<CoreTestContext>(),
                        Array.Empty<IJobsterValidator<CoreTestContext>>())));
        }

        [Test]
        public void Engines_BasicEngine_ShouldValidateCyclicDependencies()
        {
            TestJobster rootJobster = new(new CoreTestContext());
            TestJobster d = new(new CoreTestContext(), false, Array.Empty<string>());
            TestJobster b = new(new CoreTestContext(), false, new[] { rootJobster.JobId, d.JobId });
            TestJobster c = new(new CoreTestContext(), false, new[] { b.JobId });
            d.RequiredJobIds = new[] { rootJobster.JobId, c.JobId };

            List<IJobsterAsync<CoreTestContext>> jobsters = new()
            {
                rootJobster, d, b, c
            };

            Assert.ThrowsAsync<CircularDependencyException>(
                async () => await new JobsterBuilder<CoreTestContext>(_mockLogger)
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
            CoreTestContext context,
            int timeout)
        {
            IList<IJobsterAsync<CoreTestContext>> jobsters = new List<IJobsterAsync<CoreTestContext>>(jobsterCount);
            for (int i = 0; i < jobsterCount; i++)
            {
                jobsters.Add(new TestJobster(context, false, null, timeout));
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
                jobsters.Add(
                    new TestJobster(
                        context,
                        jobsterThrowsExceptionInTheMiddle && i == middleJobster));
            }

            JobsterBuilder<CoreTestContext> builder = new JobsterBuilder<CoreTestContext>(_mockLogger)
                .AddBatchSize(batchSize)
                .AddExceptionPolicy(exceptionPolicy)
                .AddJobsters(null, jobsters.ToArray())
                .AddStateHandler(
                    stateHandler ?? new MemoryJobsterStateHandler<CoreTestContext>(
                        state ?? Array.Empty<IJobsterAsync<CoreTestContext>>()));
            if (progressNotifier != null)
            {
                builder.AddProgressNotifier(progressNotifier);
            }

            _ = await builder
                .BuildEngine()
                .ScheduleJobstersAsync(_cancellationTokenSource.Token);

            return context;
        }

        private async Task<CoreTestContext> ExecuteSequentialOrchestrator(
            int batchSize,
            CoreTestContext context)
        {
            JobsterBuilder<CoreTestContext> builder = new JobsterBuilder<CoreTestContext>(_mockLogger)
                .AddBatchSize(batchSize)
                .AddJobsters(null, new TestSequentialJobster(context, 0));

            _ = await builder
                .BuildEngine()
                .ScheduleJobstersAsync(_cancellationTokenSource.Token);

            return context;
        }
    }
}