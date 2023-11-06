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

    public class ModeratorBanLogEntry : MajorLog
    {
        [JsonProperty]
        public string Reason = "";

        [JsonProperty]
        public ulong ModeratorId = 0;

        [JsonProperty]
        public ulong MessagePruneDays = 0;

        public ModeratorBanLogEntry(ulong id = 0, ulong timestamp = 0, string reason = "", ulong moderatorid = 0, ulong MessagePruneDays2 = 0) : base(id, timestamp)
        {
            Reason = reason;
            ModeratorId = moderatorid;
            MessagePruneDays = MessagePruneDays2;
        }

        [JsonConstructor]
        public ModeratorBanLogEntry() : base()
        {
        }

        [JsonProperty]
        public override string Name { get; protected set; } = "modban";

        public override UserBehaviourLogEntry Instantiate()
        {
            var tmp = new ModeratorBanLogEntry(0,(ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Reason);
            tmp._template = false;
            return tmp;
        }

        public override UserBehaviourLogEntry Load(string jsonstring)
        {
            var loaded = JsonConvert.DeserializeObject<ModeratorBanLogEntry>(jsonstring);
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
            return $"- {ID}: <@{ModeratorId}> issued a Ban at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}> for this user.";
        }


        public override EmbedBuilder FormatDetailed()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle($"Ban issued at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}> for this user")
                .WithDescription($"<@{ModeratorId}> issued Warning for this user.")
                .AddField("Reason", Reason)
                .AddField("Case ID", ID)
                .AddField("Message Prune Days", MessagePruneDays)
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