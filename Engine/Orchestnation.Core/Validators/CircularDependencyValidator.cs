using Microsoft.Extensions.Logging;
using Orchestnation.Common.Logic;
using Orchestnation.Core.Contexts;
using Orchestnation.Core.Jobsters;
using System.Collections.Generic;
using System.Linq;

namespace Orchestnation.Core.Validators
{
    public class CircularDependencyValidator<T> : IJobsterValidator<T> where T : IJobsterContext
    {
        public CircularDependencyValidator(ILogger logger)
        {
            Logger = logger;
        }

        public ILogger Logger { get; set; }

        public void Validate(IList<IJobsterAsync<T>> jobsterMetadata)
        {
            Logger.LogInformation("Validating dependencies");
            _ = jobsterMetadata.TopologicalSort(
                    p => jobsterMetadata
                        .Where(q => p.RequiredJobIds.Contains(q.JobId)))
                .ToArray();
            Logger.LogInformation("Validation successful");
        }
    }
}