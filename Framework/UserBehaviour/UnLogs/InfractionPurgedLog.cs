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
    public class ModeratorPurgeInfractionLogEntry : PardonLog
    {
        [JsonProperty]
        public string Reason = "";

        [JsonProperty]
        public ulong ModeratorId = 0;

        [JsonProperty]
        public ulong AmountDeleted = 0;

        [JsonProperty]
        public string TransactionID = "";

        public ModeratorPurgeInfractionLogEntry(ulong id = 0, ulong timestamp = 0, ulong moderatorid = 0, ulong amountdeleted = 0, string reason = "", string transactionid = "") : base(id, timestamp)
        {
            Reason = reason;
            ModeratorId = moderatorid;
            AmountDeleted = amountdeleted;
            TransactionID = transactionid;
        }

        [JsonConstructor]
        public ModeratorPurgeInfractionLogEntry() : base()
        {
        }

        [JsonProperty]
        public override string Name { get; protected set; } = "modpurgeinfraction";

        public override UserBehaviourLogEntry Instantiate()
        {
            var tmp = new ModeratorPurgeInfractionLogEntry(0, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),ModeratorId, AmountDeleted, Reason, TransactionID);
            tmp._template = false;
            return tmp;
        }

        public override UserBehaviourLogEntry Load(string jsonstring)
        {
            var loaded = JsonConvert.DeserializeObject<ModeratorPurgeInfractionLogEntry>(jsonstring);
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
            return $"- {ID}: <@{ModeratorId}> purged all infractions of this user at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}>.";
        }

        public override EmbedBuilder FormatDetailed()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle($"<@{ModeratorId}> purged all infractions of this user at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}>")
                .WithDescription($"<@{ModeratorId}> purged all infractions of this user.")
                .AddField("Reason", Reason)
                .AddField("Event ID", ID)
                .AddField("Amount deleted", AmountDeleted)
                .AddField("Transaction ID", TransactionID)
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