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
        private TestJobster _b;
        private TestJobster _c;
        private TestJobster _d;
        private List<IJobsterAsync<TestJobsterContext>> _jobsters;
        private TestJobster _rootJobster;

        [SetUp]
        public void Setup()
        {
            _rootJobster = new TestJobster(new TestJobsterContext());

            _d = new TestJobster(new TestJobsterContext(), new string[0]);
            _b = new TestJobster(new TestJobsterContext(), new[] { _rootJobster.JobId, _d.JobId });
            _c = new TestJobster(new TestJobsterContext(), new[] { _b.JobId });

            _jobsters = new List<IJobsterAsync<TestJobsterContext>>
            {
                _rootJobster, _d, _b, _c
            };
        }

        [Test]
        public void Validation_CircularDependency_ShouldDetectCircularDependency()
        {
            _d.RequiredJobIds = new[] { _rootJobster.JobId, _c.JobId };

            Assert.Throws<CircularDependencyException>(() => _jobsters.TopologicalSort(p => _jobsters
                .Where(q => p.RequiredJobIds.Contains(q.JobId)))
                .ToArray());
        }

        [Test]
        public void Validation_CircularDependency_ShouldFindNoIssues()
        {
            Assert.DoesNotThrow(() => _jobsters.TopologicalSort(p => _jobsters
                .Where(q => p.RequiredJobIds.Contains(q.JobId)))
                .ToArray());
        }
    }
}