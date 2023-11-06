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
    public class UserVerifiedLogEntry : PermissionChangeLog
    {
        [JsonProperty]
        public ulong ModeratorId = 0;

        public UserVerifiedLogEntry (ulong id = 0, ulong timestamp = 0, ulong moderatorid = 0) : base(id, timestamp)
        {
            ModeratorId = moderatorid;
        }

        [JsonConstructor]
        public UserVerifiedLogEntry () : base()
        {
        }

        [JsonProperty]
        public override string Name { get; protected set; } = "userverified";

        public override UserBehaviourLogEntry Instantiate()
        {
            var tmp = new UserVerifiedLogEntry (0, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ModeratorId);
            tmp._template = false;
            return tmp;
        }

        public override UserBehaviourLogEntry Load(string jsonstring)
        {
            var loaded = JsonConvert.DeserializeObject<UserVerifiedLogEntry >(jsonstring);
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
            return $"- {ID}: <@{ModeratorId}> verified this user at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}>";
        }

        public override EmbedBuilder FormatDetailed()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle($"<@{ModeratorId}> verified this user at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}>")
                .WithDescription($"<@{ModeratorId}> verified this user")
                .AddField("Entry ID", ID)
                .WithColor(Color.Green)
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