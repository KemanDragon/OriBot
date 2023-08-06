using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Discord.WebSocket;

using Newtonsoft.Json;

using OldOriBot.Data.MemberInformation;

using OriBot.Framework.UserProfiles;

namespace OriBot.Framework.UserBehaviour
{
    public class ModeratorNoteLogEntry : MajorLog
    {
        [JsonProperty]
        public string Note = "";

        [JsonProperty]
        public ulong ModeratorId = 0;

        public ModeratorNoteLogEntry(ulong id = 0, ulong timestamp = 0, string note = "", ulong moderatorid = 0) : base(id, timestamp)
        {
            Note = note;
            ModeratorId = moderatorid;
        }

        [JsonConstructor]
        public ModeratorNoteLogEntry() : base()
        {
        }

        [JsonProperty]
        public override string Name { get; protected set; } = "modnote";

        public override UserBehaviourLogEntry Instantiate()
        {
            var tmp = new ModeratorNoteLogEntry(0,(ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Note,ModeratorId);
            tmp._template = false;
            return tmp;
        }

        public override UserBehaviourLogEntry Load(string jsonstring)
        {
            var loaded = JsonConvert.DeserializeObject<ModeratorNoteLogEntry>(jsonstring);
            if (loaded != null)
            {
                loaded._template = false;
                return loaded;
            }
            return null;
        }

        public override string Save()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }

        public override string Format()
        {
            return $"Entry #{ID}: <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}>: Moderator <@{ModeratorId}> made a private note: \"{Note}\" for this user.";
        }

        public static DateTime UnixTimeStampToDateTime(ulong unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
    }
}