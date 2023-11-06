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
    public class ModeratorUnbanLogEntry : PardonLog
    {
        [JsonProperty]
        public string Reason = "";

        [JsonProperty]
        public ulong ModeratorId = 0;

        public ModeratorUnbanLogEntry(ulong id = 0, ulong timestamp = 0, string reason = "", ulong moderatorid = 0) : base(id, timestamp)
        {
            Reason = reason;
            ModeratorId = moderatorid;
        }

        [JsonConstructor]
        public ModeratorUnbanLogEntry() : base()
        {
        }

        [JsonProperty]
        public override string Name { get; protected set; } = "modunban";

        public override UserBehaviourLogEntry Instantiate()
        {
            var tmp = new ModeratorUnbanLogEntry(0, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Reason, ModeratorId);
            tmp._template = false;
            return tmp;
        }

        public override UserBehaviourLogEntry Load(string jsonstring)
        {
            var loaded = JsonConvert.DeserializeObject<ModeratorUnbanLogEntry>(jsonstring);
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
            return $"- {ID}: <@{ModeratorId}> unbanned this user at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}>.";
        }

        public override EmbedBuilder FormatDetailed()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle($"Unban issued at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).Subtract(DateTime.UnixEpoch).TotalSeconds)}> for this user")
                .WithDescription($"<@{ModeratorId}> unbanned this user.")
                .AddField("Reason", Reason)
                .AddField("Event ID", ID)
                .WithColor(Color.Orange)
                .WithFooter($"Event ID: {ID} | Moderator ID: {ModeratorId} | Event timestamp: {Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}");
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