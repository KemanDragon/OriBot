using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using OriBot.Framework;

namespace OriBot.Storage
{
    public class JObject
    {
        
        private Dictionary<dynamic, dynamic> _internaldict = new();

        public JObject(string obj) {
            _internaldict = JsonConvert.DeserializeObject<Dictionary<dynamic, dynamic>>(obj);
        }

        
        public JObject(Dictionary<dynamic, dynamic> starterobject)
        {
            this._internaldict = starterobject;
        }

        public JObject() {}



        public dynamic this[dynamic key]
        {
            get
            {

                return _internaldict[key];
            }
            set
            {
                _internaldict[key] = value;
            }
        }

        public string Save()
        {
            return JsonConvert.SerializeObject(_internaldict, Formatting.None);
        }

        public static JObject Load(string obj)
        {
            var tmp = JsonConvert.DeserializeObject<Dictionary<dynamic, dynamic>>(obj);
            return new JObject(tmp);
        }

        public static JObject Blank() {
            return new JObject();
        }

        public override string ToString()
        {
            return Save();
        }
    }
}
