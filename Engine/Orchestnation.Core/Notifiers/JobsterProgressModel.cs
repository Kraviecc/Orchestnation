using Orchestnation.Core.Jobsters;

namespace Orchestnation.Core.Notifiers
{
    public class JobsterProgressModel
    {
        private readonly object _lock = new object();

        public JobsterProgressModel(int jobstersAsyncCount)
        {
            All = jobstersAsyncCount;
        }

        public int All { get; }
        public int Completed { get; private set; }
        public int Failed { get; private set; }
        public int Finished { get; private set; }

        public void ReportJobsterFinished(JobsterStatusEnum status)
        {
            switch (status)
            {
                case JobsterStatusEnum.Completed:
                    lock (_lock)
                    {
                        Completed++;
                        Finished++;
                    }
                    break;

                case JobsterStatusEnum.Failed:
                    lock (_lock)
                    {
                        Failed++;
                        Finished++;
                    }
                    break;
            }
        }
    }
}