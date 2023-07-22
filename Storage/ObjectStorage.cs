using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using OriBot.Framework;

namespace OriBot.Storage2
{
    public class JObject
    {
        [JsonProperty]
        private Dictionary<dynamic, dynamic> _storage;

        [JsonProperty]
        private bool strict = false;

        public JObject(bool strict = false)
        {
            this.strict = strict;
        }

        [JsonConstructor]
        public JObject(Dictionary<dynamic, dynamic> starterobject, bool strict = false)
        {
            this.strict = strict;
            this._storage = starterobject;
        }

        public dynamic this[dynamic key]
        {
            get
            {
                if (_storage is null)
                {
                    _storage = new();
                }
                if (!_storage.TryGetValue(key,out dynamic _))
                {
                    if (strict)
                    {
                        Logging.Warn("Null returned when trying to get key " + key + ", a crash may be happening. Disable Strict mode to prevent this from happening", Origin.SERVER);
                        return null;
                    }
                    _storage[key] = new JObject(false);
                }
                return _storage[key];
            }
            set
            {
                if(_storage is null)
                {
                    _storage = new();
                }
                _storage[key] = value;
            }
        }

        public string Save()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }

        public static JObject Load(string obj)
        {
            return JsonConvert.DeserializeObject<JObject>(obj);
        }

        public override string ToString()
        {
            return Save();
        }
    }
}
