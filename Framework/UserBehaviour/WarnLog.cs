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
    public enum WarnType
    {
        Harsh,
        Minor,
        Normal,
    }

    public class ModeratorWarnLogEntry : MajorLog
    {
        [JsonProperty]
        public string Reason = "";

        [JsonProperty]
        public WarnType WarningType = WarnType.Minor;

        [JsonProperty]
        public ulong ModeratorId = 0;

        public ModeratorWarnLogEntry(ulong id = 0, ulong timestamp = 0, string reason = "", WarnType type = WarnType.Minor, ulong moderatorid = 0) : base(id, timestamp)
        {
            Reason = reason;
            WarningType = type;
            ModeratorId = moderatorid;
        }

        [JsonConstructor]
        public ModeratorWarnLogEntry() : base()
        {
        }

        [JsonProperty]
        public override string Name { get; protected set; } = "modwarn";

        public override UserBehaviourLogEntry Instantiate()
        {
            var tmp = new ModeratorWarnLogEntry(0,(ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Reason, WarningType,ModeratorId);
            tmp._template = false;
            return tmp;
        }

        public override UserBehaviourLogEntry Load(string jsonstring)
        {
            var loaded = JsonConvert.DeserializeObject<ModeratorWarnLogEntry>(jsonstring);
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
            return $"- {ID}: <@{ModeratorId}> issued a Warning at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}> for this user.";
        }

        public override EmbedBuilder FormatDetailed()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle($"Warning issued at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}> for this user")
                .WithDescription($"<@{ModeratorId}> issued a Warning for this user.")
                .AddField("Reason", Reason)
                .AddField("Type", WarningType.ToString())
                .AddField("Case ID", ID)
                .WithColor(Color.Orange)
                .WithFooter($"Entry ID: {ID} | Moderator ID: {ModeratorId} | Event timestamp: {Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}");
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