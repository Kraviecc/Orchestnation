using Microsoft.Extensions.Logging;
using Orchestnation.Core.Contexts;
using Orchestnation.Core.Jobsters;
using System.Collections.Generic;

namespace Orchestnation.Core.Validators
{
    public interface IJobsterValidator<T> where T : IJobsterContext
    {
        ILogger Logger { get; set; }

        void Validate(IList<IJobsterAsync<T>> jobsterMetadata);
    }
}