using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using OriBot.Utilities;

namespace OriBot.Framework.UserProfiles {
    public class DiagnosticLogContainer {

        public static int MaxEntriesPerUser => Config.properties["userprofile"]["maxdiagnosticlogentries"].ToObject<int>();

        private List<string> _log = new List<string>();

        public IReadOnlyList<string> Log {
            get => _log;
            set
            {
                _log = value.ToList();
                while (_log.Count > MaxEntriesPerUser)
                {
                    _log.RemoveAt(0);
                }
                saveAction?.Invoke();
            }
        }

        private DiagnosticLogContainer() {}

        private Action saveAction { get; set; }
        
        public void Add(string message)
        {
            _log.Add(message);
            while (_log.Count > MaxEntriesPerUser) {
                _log.RemoveAt(0);
            }
            saveAction?.Invoke();
        }

        public static DiagnosticLogContainer Load(string encodedjson, Action savefunction)
        {
            if (encodedjson == null)
            {
                return new DiagnosticLogContainer() { saveAction = savefunction };
            }
            var tmp = new DiagnosticLogContainer();
            tmp.saveAction = () => { };
            var tmp2 = JsonConvert.DeserializeObject<List<string>>(encodedjson);
            foreach (var item in tmp2)
            {
                tmp.Add(item);
            }
            tmp.saveAction = savefunction;
            return tmp;
        }

        public string Serialized
        {
            get
            {
                return JsonConvert.SerializeObject(_log, Formatting.None);
            }
        }
    }
}