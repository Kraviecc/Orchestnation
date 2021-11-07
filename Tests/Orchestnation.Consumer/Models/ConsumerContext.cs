using Orchestnation.Core.Contexts;

namespace Orchestnation.Consumer.Models
{
    public class ConsumerContext : IJobsterContext
    {
        private readonly object _lock = new();
        public int Counter { get; set; }

        public void Increment()
        {
            lock (_lock)
            {
                Counter++;
            }
        }
    }
}