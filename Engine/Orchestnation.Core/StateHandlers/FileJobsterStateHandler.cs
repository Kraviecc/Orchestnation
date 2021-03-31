using Newtonsoft.Json;
using Orchestnation.Core.Contexts;
using Orchestnation.Core.Jobsters;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Orchestnation.Core.StateHandlers
{
    public class FileJobsterStateHandler<T> : IJobsterStateHandler<T> where T : IJobsterContext
    {
        private readonly object _lock = new object();
        private readonly string _path;

        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            TypeNameHandling = TypeNameHandling.Auto
        };

        public FileJobsterStateHandler(string path)
        {
            _path = path;
        }

        public Task PersistState(IEnumerable<IJobsterAsync<T>> jobsters)
        {
            lock (_lock)
            {
                File.WriteAllText(
                    _path,
                    JsonConvert.SerializeObject(
                        jobsters,
                        Formatting.Indented,
                        _serializerSettings));
            }

            return Task.CompletedTask;
        }

        public Task<IJobsterAsync<T>[]> RestoreState()
        {
            if (!File.Exists(_path))
                return Task.FromResult(new IJobsterAsync<T>[0]);

            string savedState = File.ReadAllText(_path);
            return Task.FromResult(JsonConvert.DeserializeObject<IJobsterAsync<T>[]>(
                savedState,
                // ReSharper disable once InconsistentlySynchronizedField
                _serializerSettings));
        }
    }
}