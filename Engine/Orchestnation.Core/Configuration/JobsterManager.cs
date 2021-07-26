using Dawn;
using Orchestnation.Core.Contexts;
using Orchestnation.Core.Jobsters;
using Orchestnation.Core.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Orchestnation.Core.Configuration
{
    public class JobsterManager<T> where T : IJobsterContext
    {
        public const string JobstersDefaultGroup = "jobsters-default-group";
        private BlockingCollection<IJobsterAsync<T>> _adHocJobsterData = new BlockingCollection<IJobsterAsync<T>>();
        private BlockingCollection<IJobsterAsync<T>> _jobsterData = new BlockingCollection<IJobsterAsync<T>>();

        public void AddJobsters(
            OrchestnationStatus orchestnationStatus,
            string groupId,
            params IJobsterAsync<T>[] jobsterAsync)
        {
            Guard.Argument(jobsterAsync, nameof(jobsterAsync)).NotNull();
            groupId ??= JobstersDefaultGroup;

            foreach (IJobsterAsync<T> jobster in jobsterAsync)
            {
                Guard.Argument(jobster, nameof(jobster)).NotNull();
                if (IsAdded(groupId, jobster))
                    continue;

                jobster.GroupId = groupId;
                if (orchestnationStatus == OrchestnationStatus.Builder)
                    _jobsterData.Add(jobster);
                else
                    _adHocJobsterData.Add(jobster);
            }
        }

        public void ApplyAdHocJobsters()
        {
            if (!_adHocJobsterData.Any())
                return;

            foreach (IJobsterAsync<T> jobster in _adHocJobsterData)
            {
                _jobsterData.Add(jobster);
            }
            _adHocJobsterData = new BlockingCollection<IJobsterAsync<T>>();
        }

        public IList<IJobsterAsync<T>> GetAllJobsterAsync() => _jobsterData
            .Concat(_adHocJobsterData)
            .ToList();

        public BlockingCollection<IJobsterAsync<T>> GetJobsterAsync() => _jobsterData;

        public int GetJobstersNumber()
        {
            return _jobsterData.Count;
        }

        public bool IsAnyAdHocJobsterPending()
        {
            return _adHocJobsterData.Any()
                   || _jobsterData.Any(p => p.Status == JobsterStatusEnum.NotStarted);
        }

        public void RestoreJobsters(BlockingCollection<IJobsterAsync<T>> jobsters)
        {
            _jobsterData.Dispose();
            _jobsterData = jobsters;
        }

        private static bool IsAddedToList(
            IEnumerable<IJobsterAsync<T>> jobsterList,
            string groupId,
            IJobsterAsync<T> jobsterAsync)
        {
            return jobsterList
                .Any(p => p.JobId == jobsterAsync.JobId
                          && p.GroupId == groupId);
        }

        private bool IsAdded(
            string groupId,
            IJobsterAsync<T> jobsterAsync)
        {
            return IsAddedToList(_jobsterData, groupId, jobsterAsync)
                   || IsAddedToList(_adHocJobsterData, groupId, jobsterAsync);
        }
    }
}