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
    public class ModeratorMuteLogEntry : MajorLog
    {
        [JsonProperty]
        public string Reason = "";

        [JsonProperty]
        public DateTime MuteEndUTC = DateTime.MinValue;

        [JsonProperty]
        public ulong ModeratorId = 0;

        public ModeratorMuteLogEntry(ulong id = 0, ulong timestamp = 0, string reason = "", ulong moderatorid = 0, DateTime enddate = default) : base(id, timestamp)
        {
            Reason = reason;
            ModeratorId = moderatorid;
            MuteEndUTC = enddate;
        }

        [JsonConstructor]
        public ModeratorMuteLogEntry() : base()
        {
        }

        [JsonProperty]
        public override string Name { get; protected set; } = "modmute";

        public override UserBehaviourLogEntry Instantiate()
        {
            var tmp = new ModeratorMuteLogEntry(0,(ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Reason, ModeratorId, MuteEndUTC);
            tmp._template = false;
            return tmp;
        }

        public override UserBehaviourLogEntry Load(string jsonstring)
        {
            var loaded = JsonConvert.DeserializeObject<ModeratorMuteLogEntry>(jsonstring);
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
            return $"Entry #{ID}: <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}>: Moderator <@{ModeratorId}> muted this user until <t:{Math.Floor(MuteEndUTC.Subtract(DateTime.UnixEpoch).TotalSeconds)}> for reason: \"{Reason}\"";
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