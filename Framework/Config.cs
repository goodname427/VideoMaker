using System.Collections.Generic;
using System.IO;

namespace Framework
{
    public class Config
    {
        private readonly Dictionary<string, string> _keyValuePairs;

        public Config(string configFilename)
        {
            _keyValuePairs = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(configFilename))
            {
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                var kvp = line.Split('=');
                _keyValuePairs.Add(kvp[0], kvp[1]);
            }
        }

        public string this[string key]
        {
            get
            {
                return _keyValuePairs[key];
            }
        }

        public string GetString(string key)
        {
            return _keyValuePairs[key];
        }

        public int GetInt32(string key)
        {
            return int.Parse(_keyValuePairs[key]);
        }

        public float GetSingle(string key)
        {
            return float.Parse(_keyValuePairs[key]);
        }
    }
}
