using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Discord.WebSocket;

using Newtonsoft.Json;

using OldOriBot.Data.MemberInformation;
using OriBot.Framework.UserProfiles;

namespace OriBot.Framework.UserBehaviour {

    public class UserBehaviourLogRegistry {
        private static List<UserBehaviourLogEntry> _logEntries;

        public static IReadOnlyList<UserBehaviourLogEntry> LogEntries {
            get
            {
                if (_logEntries == null)
                {
                    InitializeBehaviourRegistry();
                }
                return _logEntries.ToList();
            }
        }

        public static void InitializeBehaviourRegistry()
        {
            _logEntries = new List<UserBehaviourLogEntry>() {
                new ModeratorWarnLogEntry(0),
                new ModeratorNoteLogEntry(0)
            };
        }

        public static T CreateLogEntry<T>() where T : UserBehaviourLogEntry {
            var tmp = Activator.CreateInstance<T>();
            foreach (UserBehaviourLogEntry log in LogEntries)
            {
                if (log.Name.ToLower() == tmp.Name.ToLower())
                {
                    return (T)log.Instantiate();
                }
            }
            return null;
        }

        public static UserBehaviourLogEntry LoadLogEntryFromString(string data)
        {
            var tmp = JsonConvert.DeserializeObject<UserBehaviourLogEntry>(data);
            foreach (UserBehaviourLogEntry log in LogEntries)
            {
                if (log.Name.ToLower() == tmp.Name.ToLower())
                {
                    return log.Load(data);
                }
            }
            return null;
        }
    }

    public class UserBehaviourLogEntry {

        [JsonProperty]
        public ulong ID { get; private set; }
        [JsonProperty] public virtual string Name { get; protected set; } = "default";

        public virtual UserBehaviourLogEntry Instantiate() { 
            throw new NotImplementedException();
        }

        public virtual UserBehaviourLogEntry Load(string jsonstring) {
            throw new NotImplementedException();
        }

        public virtual string Save() {
            throw new NotImplementedException();
        }

        public virtual string Format() {
            throw new NotImplementedException();
        }

        public bool IsTemplate {
            get { return _template; }
        }

        [JsonIgnore]
        protected bool _template = true;

        protected UserBehaviourLogEntry(ulong id = 0) { 
            ID = id;
        }

        [JsonConstructor]
        protected UserBehaviourLogEntry()
        {
            ID = 0;
        }
    }

    public abstract class MinorLog : UserBehaviourLogEntry {
        protected MinorLog(ulong id) : base(id)
        {

        }

        protected MinorLog() : base()
        {
        }
    }

    public abstract class MajorLog : UserBehaviourLogEntry
    {
        protected MajorLog(ulong id) : base(id)
        {

        }

        protected MajorLog() : base()
        {
        }
    }
}