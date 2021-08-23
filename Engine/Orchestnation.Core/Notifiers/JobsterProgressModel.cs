using Orchestnation.Core.Jobsters;
using System;
using System.Threading;

namespace Orchestnation.Core.Notifiers
{
    public class JobsterProgressModel
    {
        private int _completed;
        private int _failed;
        private int _finished;

        public JobsterProgressModel(int jobstersAsyncCount)
        {
            All = jobstersAsyncCount;
        }

        public int All { get; }
        public int Completed => _completed;
        public int Failed => _failed;
        public int Finished => _finished;

        public void ReportJobsterFinished(JobsterStatusEnum status)
        {
            switch (status)
            {
                case JobsterStatusEnum.Completed:
                    Interlocked.Increment(ref _completed);
                    Interlocked.Increment(ref _finished);
                    break;

                case JobsterStatusEnum.Failed:
                    Interlocked.Increment(ref _failed);
                    Interlocked.Increment(ref _finished);
                    break;

                case JobsterStatusEnum.NotStarted:
                    break;

                case JobsterStatusEnum.Executing:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }
    }
}