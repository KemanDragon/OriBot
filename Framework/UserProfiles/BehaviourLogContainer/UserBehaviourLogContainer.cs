using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using OriBot.Framework.UserBehaviour;
using OriBot.Framework.UserProfiles.ProfileConfig;

namespace OriBot.Framework.UserProfiles.BehaviourLogContainer
{
    public class UserBehaviourLogContainer
    {
        private List<UserBehaviourLogEntry> _logs = new List<UserBehaviourLogEntry>();

        public IReadOnlyList<UserBehaviourLogEntry> Logs => _logs;

        private Action saveAction { get; set; }

        private UserBehaviourLogContainer()
        {
        }

        public void AddLogEntry(UserBehaviourLogEntry entry)
        {
            _logs.Add(entry);
            saveAction();
        }

        public void RemoveByID(ulong eventid)
        {
            _logs.RemoveAll(x => x.ID == eventid);
            saveAction();
        }

        public UserBehaviourLogEntry GetByID(ulong eventid)
        {
            return _logs.Find(x => x.ID == eventid);
        }

        public static UserBehaviourLogContainer Load(string jsonstring, Action saveAction)
        {
            if (jsonstring == null) {
                var tmp =  new UserBehaviourLogContainer();
                tmp.saveAction = saveAction;
                return tmp;
            }
            var collection = JsonConvert.DeserializeObject<List<string>>(jsonstring);
            var container = new UserBehaviourLogContainer();
            container._logs.AddRange(collection.Select(x => UserBehaviourLogRegistry.LoadLogEntryFromString(x)));
            container.saveAction = saveAction;
            return container;
        }

        public string Serialized {
            get {
                var tmp = new List<string>();
                foreach (var entry in Logs)
                {
                    tmp.Add(entry.Save());
                }
                return JsonConvert.SerializeObject(tmp, Formatting.None);
            }
        }
    }

}
