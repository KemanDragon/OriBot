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
    public class ModeratorDeleteNoteLogEntry : PardonLog
    {
        [JsonProperty]
        public string Reason = "";

        [JsonProperty]
        public string NoteContent = "";

        [JsonProperty]
        public ulong ModeratorId = 0;

        [JsonProperty]
        public ulong NoteID = 0;

        public ModeratorDeleteNoteLogEntry(ulong id = 0, ulong timestamp = 0, string reason = "", ulong NoteID2 = 0, ulong moderatorid = 0, string notecontent = "") : base(id, timestamp)
        {
            Reason = reason;
            NoteID = NoteID2;
            ModeratorId = moderatorid;
            NoteContent = notecontent;
        }

        [JsonConstructor]
        public ModeratorDeleteNoteLogEntry() : base()
        {
        }

        [JsonProperty]
        public override string Name { get; protected set; } = "moddeletenote";

        public override UserBehaviourLogEntry Instantiate()
        {
            var tmp = new ModeratorDeleteNoteLogEntry(0, (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Reason, NoteID, ModeratorId, NoteContent);
            tmp._template = false;
            return tmp;
        }

        public override UserBehaviourLogEntry Load(string jsonstring)
        {
            var loaded = JsonConvert.DeserializeObject<ModeratorDeleteNoteLogEntry>(jsonstring);
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
            return $"- {ID}: <@{ModeratorId}> deleted a note for this user at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}>.";
        }

        public override EmbedBuilder FormatDetailed()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle($"<@{ModeratorId}> deleted a note for this user at <t:{Math.Floor(UnixTimeStampToDateTime(TimestampUTC).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}>.")
                .WithDescription($"<@{ModeratorId}> deleted a note for this user.")
                .AddField("Reason", Reason)
                .AddField("Event ID", ID)
                .AddField("Note ID", NoteID)
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