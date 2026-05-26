using System;
using System.IO;
using Newtonsoft.Json;

namespace CyberHookAP
{
    internal sealed class JsonFileStore<T> where T : new()
    {
        private readonly string _path;

        public JsonFileStore(string path)
        {
            _path = path;
        }

        public T LoadOrCreate()
        {
            try
            {
                if (!File.Exists(_path))
                {
                    T created = new T();
                    Save(created);
                    return created;
                }

                string json = File.ReadAllText(_path);
                T result = JsonConvert.DeserializeObject<T>(json);
                if (result == null)
                {
                    result = new T();
                    Save(result);
                }

                return result;
            }
            catch (Exception ex)
            {
                CyberHookApMod.SafeLog("Failed to load " + Path.GetFileName(_path) + ": " + ex);
                T fallback = new T();
                Save(fallback);
                return fallback;
            }
        }

        public void Save(T value)
        {
            string directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonConvert.SerializeObject(value, Formatting.Indented);
            File.WriteAllText(_path, json);
        }
    }
}
