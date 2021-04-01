using System;
using System.Collections.Generic;
using System.Linq;

namespace Orchestnation.Core.Models
{
    public class JobsterFailureModel
    {
        private readonly IList<KeyValuePair<string, Exception>> _exceptions = new List<KeyValuePair<string, Exception>>();
        public bool IsError { get; internal set; }

        public string GetErrors()
        {
            return string.Join(
                "\n",
                _exceptions
                    .Select(p => $"{p.Key} | {p.Value}"));
        }

        public void SetIsError(
            string jobsterId,
            Exception ex)
        {
            IsError = true;
            if (ex == null)
                return;

            _exceptions.Add(new KeyValuePair<string, Exception>(jobsterId, ex));
        }
    }
}