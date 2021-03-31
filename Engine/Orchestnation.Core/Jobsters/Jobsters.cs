﻿using Orchestnation.Core.Contexts;
using System.Collections.Generic;
using System.Linq;

namespace Orchestnation.Core.Jobsters
{
    public class Jobsters<T> where T : IJobsterContext
    {
        public Jobsters(IList<IJobsterAsync<T>> jobstersAsync, bool restoredState = false)
        {
            JobstersAsync = jobstersAsync;

            if (restoredState)
                return;

            foreach (IJobsterAsync<T> jobsterAsync in jobstersAsync)
            {
                jobsterAsync.Status = JobsterStatusEnum.NotStarted;
            }
        }

        public IList<IJobsterAsync<T>> JobstersAsync { get; set; }

        public IEnumerable<IJobsterAsync<T>> GetNoDependencyJobsters()
        {
            return JobstersAsync.Where(p => p.Status == JobsterStatusEnum.NotStarted
                                            && (p.RequiredJobIds == null || p.RequiredJobIds.Length == 0));
        }
    }
}