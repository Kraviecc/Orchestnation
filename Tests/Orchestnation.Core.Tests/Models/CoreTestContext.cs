using Orchestnation.Core.Contexts;

namespace Orchestnation.Core.Tests.Models
{
    public class CoreTestContext : IJobsterContext
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