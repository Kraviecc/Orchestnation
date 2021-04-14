using Orchestnation.Common.Models;
using System;

namespace Orchestnation.Common.Exceptions
{
    public class JobsterException : Exception
    {
        public JobsterException(JobsterFailureModel failureModel)
        {
            FailureModel = failureModel;
        }

        public JobsterFailureModel FailureModel { get; }
    }
}