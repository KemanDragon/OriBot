using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Discord;
using Discord.WebSocket;

using Newtonsoft.Json;

using OldOriBot.Data.MemberInformation;

using OriBot.Framework.UserProfiles;

namespace OriBot.Framework.UserBehaviour
{
    public class UserBehaviourLogRegistry
    {
        private static List<UserBehaviourLogEntry> _logEntries;

        public static IReadOnlyList<UserBehaviourLogEntry> LogEntries
        {
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
                new ModeratorWarnLogEntry(),
                new ModeratorNoteLogEntry(),
                new ModeratorMuteLogEntry(),
                new ModeratorBanLogEntry(),
                new ModeratorUnmuteLogEntry(),
                new ModeratorDeleteInfractionLogEntry(),
                new ModeratorPurgeInfractionLogEntry(),
                new ModeratorDeleteNoteLogEntry(),
                new ModeratorPurgeNoteLogEntry(),
                new ModeratorUnbanLogEntry(),
                new UserVerifiedLogEntry(),
                new UserPromotedLogEntry(),
            };
        }

        public static T CreateLogEntry<T>() where T : UserBehaviourLogEntry
        {
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

    public class UserBehaviourLogEntry
    {
        [JsonProperty]
        public ulong ID { get; set; }

        [JsonProperty]
        public ulong TimestampUTC { get; private set; }

        [JsonProperty] public virtual string Name { get; protected set; } = "default";

        public virtual UserBehaviourLogEntry Instantiate()
        {
            throw new NotImplementedException();
        }

        public virtual UserBehaviourLogEntry Load(string jsonstring)
        {
            throw new NotImplementedException();
        }

        public virtual string Save()
        {
            throw new NotImplementedException();
        }

        public virtual string FormatSimple()
        {
            throw new NotImplementedException();
        }

        public virtual EmbedBuilder FormatDetailed()
        {
            throw new NotImplementedException();
        
        }

        public bool IsTemplate
        {
            get { return _template; }
        }

        [JsonIgnore]
        protected bool _template = true;

        protected UserBehaviourLogEntry(ulong id = 0, ulong timestamp = 0)
        {
            ID = id;
            TimestampUTC = timestamp;
        }

        [JsonConstructor]
        protected UserBehaviourLogEntry()
        {
            ID = 0;
            TimestampUTC = 0;
        }
    }

    public abstract class MinorLog : UserBehaviourLogEntry
    {
        protected MinorLog(ulong id, ulong timestamp) : base(id, timestamp)
        {
        }

        protected MinorLog() : base()
        {
        }
    }

    public abstract class MajorLog : UserBehaviourLogEntry
    {
        protected MajorLog(ulong id, ulong timestamp) : base(id, timestamp)
        {
        }

        protected MajorLog() : base()
        {
        }
    }

    public abstract class PardonLog : UserBehaviourLogEntry
    {
        protected PardonLog(ulong id, ulong timestamp) : base(id, timestamp)
        {
        }

        protected PardonLog() : base()
        {
        }
    }

    public abstract class PermissionChangeLog : UserBehaviourLogEntry
    {
        protected PermissionChangeLog(ulong id, ulong timestamp) : base(id, timestamp)
        {
        }

        protected PermissionChangeLog() : base()
        {
        }
    }
}