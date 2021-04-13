using NUnit.Framework;
using Orchestnation.Core.Configuration;
using Orchestnation.Core.Executors;
using Orchestnation.Core.Tests.Models;
using Orchestnation.Core.Validators;
using System;

namespace Orchestnation.Core.Tests.Configuration
{
    public class BasicConfigurationTests
    {
        private BasicConfiguration<CoreTestContext> _basicConfiguration;

        [Test]
        public void Configuration_BasicConfiguration_ShouldLetPositiveBatchSize()
        {
            Assert.DoesNotThrow(() => _basicConfiguration.SetBatchSize(10));
        }

        [Test]
        public void Configuration_BasicConfiguration_ShouldNotLet0BatchSize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _basicConfiguration.SetBatchSize(0));
        }

        [Test]
        public void Configuration_BasicConfiguration_ShouldNotLetNegativeBatchSize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _basicConfiguration.SetBatchSize(-1));
        }

        [SetUp]
        public void Setup()
        {
            _basicConfiguration = new BasicConfiguration<CoreTestContext>(
                new LocalExecutor<CoreTestContext>(),
                new IJobsterValidator<CoreTestContext>[0]);
        }
    }
}