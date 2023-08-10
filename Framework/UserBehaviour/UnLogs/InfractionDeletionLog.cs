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
    public class ModeratorDeleteInfractionLogEntry : PardonLog
    {
        [JsonProperty]
        public string Reason = "";

        [JsonProperty]
        public ulong ModeratorId = 0;

        [JsonProperty]
        public ulong InfractionID = 0;

        [JsonProperty]
        public string InfractionFormatted = "";

        public ModeratorDeleteInfractionLogEntry(ulong id = 0, ulong timestamp = 0, string reason = "", ulong infractionid = 0, ulong moderatorid = 0, string infractionformatted = "") : base(id, timestamp)
        {
            Reason = reason;
            InfractionID = infractionid;
            ModeratorId = moderatorid;
            InfractionFormatted = infractionformatted;
        }

        [JsonConstructor]
        public ModeratorDeleteInfractionLogEntry() : base()
        {
        }

        [JsonProperty]
        public override string Name { get; protected set; } = "moddeleteinfraction";

        public override UserBehaviourLogEntry Instantiate()
        {
            var tmp = new ModeratorDeleteInfractionLogEntry(0, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Reason, InfractionID, ModeratorId, InfractionFormatted);
            tmp._template = false;
            return tmp;
        }

        public override UserBehaviourLogEntry Load(string jsonstring)
        {
            var loaded = JsonConvert.DeserializeObject<ModeratorDeleteInfractionLogEntry>(jsonstring);
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
            return $"- {ID}: <@{ModeratorId}> deleted an infraction for this user at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}>.";
        }

        public override EmbedBuilder FormatDetailed()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle($"<@{ModeratorId}> deleted an infraction for this user at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}>.")
                .WithDescription($"<@{ModeratorId}> deleted an infraction for this user.")
                .AddField("Reason", Reason)
                .AddField("Event ID", ID)
                .AddField("Infraction ID", InfractionID)
                .AddField("Infraction", InfractionFormatted)
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