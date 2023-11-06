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
    public class ModeratorMuteLogEntry : MajorLog
    {
        [JsonProperty]
        public string Reason = "";

        [JsonProperty]
        public DateTime MuteEndUTC = DateTime.MinValue;

        [JsonProperty]
        public ulong ModeratorId = 0;

        [JsonProperty]
        public string MuteTimerID = "";

        public ModeratorMuteLogEntry(ulong id = 0, ulong timestamp = 0, string reason = "", ulong moderatorid = 0, string mutetimerid = "", DateTime enddate = default) : base(id, timestamp)
        {
            Reason = reason;
            ModeratorId = moderatorid;
            MuteEndUTC = enddate;
            MuteTimerID = mutetimerid;
        }

        [JsonConstructor]
        public ModeratorMuteLogEntry() : base()
        {
        }

        [JsonProperty]
        public override string Name { get; protected set; } = "modmute";

        public override UserBehaviourLogEntry Instantiate()
        {
            var tmp = new ModeratorMuteLogEntry(0,(ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Reason, ModeratorId, MuteTimerID, MuteEndUTC);
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

        public override string FormatSimple()
        {
            return $"- {ID}: <@{ModeratorId}> issued a Mute at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}> for this user.";
        }

        public override EmbedBuilder FormatDetailed()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle($"Mute issued at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).Subtract(DateTime.UnixEpoch).TotalSeconds)}> for this user")
                .WithDescription($"<@{ModeratorId}> issued a Mute for this user.")
                .AddField("Reason", Reason)
                .AddField("Case ID", ID)
                .AddField("Mute end date", $"<t:{Math.Floor(MuteEndUTC.Subtract(DateTime.UnixEpoch).TotalSeconds)}> / <t:{Math.Floor(MuteEndUTC.Subtract(DateTime.UnixEpoch).TotalSeconds)}:R>")
                .AddField("Mute timer ID", MuteTimerID)
                .WithColor(Color.Orange)
                .WithFooter($"Case ID: {ID} | Moderator ID: {ModeratorId} | Event timestamp: {Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}");
            return embed;
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