using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Orchestnation.Core.Configuration;
using Orchestnation.Core.Engines;
using Orchestnation.Core.Executors;
using Orchestnation.Core.Jobsters;
using Orchestnation.Core.Tests.Models;
using Orchestnation.Core.Validators;
using System;
using System.Collections.Generic;

namespace Orchestnation.Core.Tests.Engines
{
    public class BasicEngineTests
    {
        private ILogger _mockLogger;

        [Test]
        public void Engines_BasicEngine_ShouldThrowExceptionWhenNullConfiguration()
        {
            Assert.Throws<ArgumentNullException>(() => new BasicEngine<CoreTestContext>(
                _mockLogger,
                new List<IJobsterAsync<CoreTestContext>>(0),
                null));
        }

        [Test]
        public void Engines_BasicEngine_ShouldThrowExceptionWhenNullJobsterExecutor()
        {
            Assert.Throws<ArgumentNullException>(() => new BasicEngine<CoreTestContext>(
                _mockLogger,
                new List<IJobsterAsync<CoreTestContext>>(0),
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

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger>().Object;
        }
    }
}