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
    public class UserPromotedLogEntry : PermissionChangeLog
    {
        [JsonProperty]
        public ulong ModeratorId = 0;

        [JsonProperty]
        public PermissionLevel PreviousLevel = PermissionLevel.NewUser;

        [JsonProperty]
        public PermissionLevel AfterLevel = PermissionLevel.NewUser;

        public UserPromotedLogEntry(ulong id = 0, ulong timestamp = 0, ulong moderatorid = 0, PermissionLevel previouslevel = PermissionLevel.NewUser,  PermissionLevel afterlevel = PermissionLevel.NewUser) : base(id, timestamp)
        {
            ModeratorId = moderatorid;
            PreviousLevel = previouslevel;
            AfterLevel = afterlevel;
        }

        [JsonConstructor]
        public UserPromotedLogEntry() : base()
        {
        }

        [JsonProperty]
        public override string Name { get; protected set; } = "userpromoted";

        public override UserBehaviourLogEntry Instantiate()
        {
            var tmp = new UserPromotedLogEntry(0, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), ModeratorId, PreviousLevel, AfterLevel);
            tmp._template = false;
            return tmp;
        }

        public override UserBehaviourLogEntry Load(string jsonstring)
        {
            var loaded = JsonConvert.DeserializeObject<UserPromotedLogEntry>(jsonstring);
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
            return $"- {ID}: <@{ModeratorId}> changed this user permission level at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}>";
        }

        public override EmbedBuilder FormatDetailed()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle($"<@{ModeratorId}> changed this user permission level at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}>")
                .WithDescription($"<@{ModeratorId}> changed this user permission level.")
                .AddField("Entry ID", ID)
                .AddField("Previous permission level", PreviousLevel)
                .AddField("New permission level", AfterLevel)
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