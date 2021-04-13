using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Orchestnation.Core.Configuration;
using Orchestnation.Core.Jobsters;
using Orchestnation.Core.Notifiers;
using Orchestnation.Core.StateHandlers;
using Orchestnation.Core.Tests.Models;
using System;

namespace Orchestnation.Core.Tests.Configuration
{
    public class JobsterBuilderTests
    {
        private JobsterBuilder<CoreTestContext> _jobsterBuilder;

        [Test]
        public void Configuration_BasicConfiguration_ShouldBuildEngine()
        {
            Assert.NotNull(_jobsterBuilder.BuildEngine());
        }

        [Test]
        public void Configuration_BasicConfiguration_ShouldLetAddJobsters()
        {
            Assert.DoesNotThrow(() => _jobsterBuilder.AddJobsters(new IJobsterAsync<CoreTestContext>[1]));
        }

        [Test]
        public void Configuration_BasicConfiguration_ShouldLetAddProgressNotifier()
        {
            Assert.DoesNotThrow(() => _jobsterBuilder.AddProgressNotifier(new LocalEventProgressNotifier<CoreTestContext>()));
        }

        [Test]
        public void Configuration_BasicConfiguration_ShouldLetAddStateHandler()
        {
            Assert.DoesNotThrow(() => _jobsterBuilder.AddStateHandler(new FileJobsterStateHandler<CoreTestContext>(string.Empty)));
        }

        [Test]
        public void Configuration_BasicConfiguration_ShouldLetPositiveBatchSize()
        {
            Assert.DoesNotThrow(() => _jobsterBuilder.AddBatchSize(10));
        }

        [Test]
        public void Configuration_BasicConfiguration_ShouldNotAddDuplicatedJobsters()
        {
            TestJobster jobster = new TestJobster(new CoreTestContext());
            TestJobster jobster2 = new TestJobster(new CoreTestContext());
            _jobsterBuilder.AddJobsters(jobster, jobster2, jobster);

            Assert.AreEqual(2, _jobsterBuilder.GetJobstersNumber());
        }

        [Test]
        public void Configuration_BasicConfiguration_ShouldThrowExceptionWhenAddingNullJobsters()
        {
            Assert.Throws<ArgumentNullException>(() => _jobsterBuilder.AddJobsters(null));
        }

        [Test]
        public void Configuration_BasicConfiguration_ShouldThrowExceptionWhenAddingNullProgressNotifier()
        {
            Assert.Throws<ArgumentNullException>(() => _jobsterBuilder.AddProgressNotifier(null));
        }

        [Test]
        public void Configuration_BasicConfiguration_ShouldThrowExceptionWhenAddingNullStateHandler()
        {
            Assert.Throws<ArgumentNullException>(() => _jobsterBuilder.AddStateHandler(null));
        }

        [Test]
        public void Configuration_BasicConfiguration_ShouldThrowExceptionWhenNegativeBatchSize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _jobsterBuilder.AddBatchSize(-10));
        }

        [SetUp]
        public void Setup()
        {
            _jobsterBuilder = new JobsterBuilder<CoreTestContext>(new Mock<ILogger>().Object);
        }
    }
}