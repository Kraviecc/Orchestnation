using Orchestnation.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Orchestnation.Common.Logic
{
    public static class Validation
    {
        public static IEnumerable<T> TopologicalSort<T>(
            this IEnumerable<T> nodes,
            Func<T, IEnumerable<T>> connected)
        {
            Dictionary<T, HashSet<T>> elements = nodes.ToDictionary(
                node => node,
                node => new HashSet<T>(connected(node)));

            while (elements.Count > 0)
            {
                KeyValuePair<T, HashSet<T>> elem = elements
                    .FirstOrDefault(x => x.Value.Count == 0);
                if (elem.Key == null)
                {
                    throw new CircularDependencyException();
                }

                elements.Remove(elem.Key);
                foreach (KeyValuePair<T, HashSet<T>> element in elements)
                {
                    element.Value.Remove(elem.Key);
                }

                yield return elem.Key;
            }
        }
    }
}