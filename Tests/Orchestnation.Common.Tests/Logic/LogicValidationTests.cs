using NUnit.Framework;
using Orchestnation.Common.Exceptions;
using Orchestnation.Common.Logic;
using Orchestnation.Common.Tests.Models;
using Orchestnation.Core.Jobsters;
using System.Collections.Generic;
using System.Linq;

namespace Orchestnation.Common.Tests.Logic
{
    public class LogicValidationTests
    {
        private TestJobster _rootJobster;

        [SetUp]
        public void Setup()
        {
            _rootJobster = new TestJobster(new TestJobsterContext());
        }

        [Test]
        public void Validation_CircularDependency_ShouldDetectCircularDependency()
        {
            var d = new TestJobster(new TestJobsterContext(), new string[0]);
            var b = new TestJobster(new TestJobsterContext(), new[] { _rootJobster.JobId, d.JobId });
            var c = new TestJobster(new TestJobsterContext(), new[] { b.JobId });

            d.RequiredJobIds = new[] { _rootJobster.JobId, c.JobId };

            List<IJobsterAsync<TestJobsterContext>> jobsters = new List<IJobsterAsync<TestJobsterContext>>
            {
                _rootJobster, d, b, c
            };

            Assert.Throws<CircularDependencyException>(() => jobsters.TopologicalSort(p => jobsters
                .Where(q => p.RequiredJobIds.Contains(q.JobId)))
                .ToArray());
        }

        [Test]
        public void Validation_CircularDependency_ShouldFindNoIssues()
        {
            var d = new TestJobster(new TestJobsterContext(), new string[0]);
            var b = new TestJobster(new TestJobsterContext(), new[] { _rootJobster.JobId, d.JobId });
            var c = new TestJobster(new TestJobsterContext(), new[] { b.JobId });

            List<IJobsterAsync<TestJobsterContext>> jobsters = new List<IJobsterAsync<TestJobsterContext>>
            {
                _rootJobster, d, b, c
            };

            Assert.DoesNotThrow(() => jobsters.TopologicalSort(p => jobsters
                .Where(q => p.RequiredJobIds.Contains(q.JobId)))
                .ToArray());
        }
    }
}