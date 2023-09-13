using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using OriBot.Framework.UserProfiles;
using OriBot.GuildData;
using OriBot.Utilities;

namespace OriBot.Commands {

    public abstract class CommandLogEntry {
        public virtual string ToLogString() {
            throw new NotImplementedException();
        }

        public virtual Embed ToEmbed() {
            throw new NotImplementedException();
        }
    }

    public class CommandSuccessLogEntry : CommandLogEntry
    {
        private UserProfile user;

        private string commandName;

        private DateTime time;

        private SocketGuild guild;

        private Dictionary<string, string> additionalFields = new Dictionary<string, string>();

        public CommandSuccessLogEntry(ulong userid, string commandName, DateTime time, SocketGuild guild) {
            this.user = ProfileManager.GetUserProfile(userid);
            this.commandName = commandName;
            this.time = time;
            this.guild = guild;
        }

        public override string ToLogString() {
            return $"<@{user.UserID}> executed {commandName} with no error, on {Math.Floor(time.Subtract(DateTime.UnixEpoch).TotalSeconds)}";
        }

        public override Embed ToEmbed() {
            var result = new EmbedBuilder()
                .WithTitle($"User <@{user.UserID}> executed command {commandName} without error")
                .WithDescription($"<@{user.UserID}> executed {commandName} with no error, on <t:{Math.Floor(time.Subtract(DateTime.UnixEpoch).TotalSeconds)}>");
            if (guild is not null) {
                result = result.WithAuthor(guild.GetUser(user.UserID));
            }
            result.Color = Discord.Color.Green;
            foreach (var item in additionalFields)
            {
                result = result.AddField(item.Key, item.Value);
            }
            return result.Build();
        }

        public CommandSuccessLogEntry WithAdditonalField(string key, string value)
        {
            additionalFields[key] = value;
            return this;
        }
    }

    public class CommandWarningLogEntry : CommandLogEntry
    {
        private UserProfile user;

        private string commandName;

        private DateTime time;

        private SocketGuild guild;

        private string errorName;

        private Dictionary<string, string> additionalFields = new Dictionary<string, string>();

        public CommandWarningLogEntry(ulong userid, string commandName, DateTime time, SocketGuild guild, string errorname)
        {
            this.user = ProfileManager.GetUserProfile(userid);
            this.errorName = errorname;
            this.commandName = commandName;
            this.time = time;
            this.guild = guild;
        }

        public override string ToLogString()
        {
            return $"<@{user.UserID}> executed {commandName} with a handled error, on {Math.Floor(time.Subtract(DateTime.UnixEpoch).TotalSeconds)}";
        }

        public override Embed ToEmbed()
        {
            var result = new EmbedBuilder()
                .WithTitle($"User <@{user.UserID}> executed command {commandName} with a handled error")
                .WithDescription($"<@{user.UserID}> executed {commandName} with a handled error, on <t:{Math.Floor(time.Subtract(DateTime.UnixEpoch).TotalSeconds)}>");
            if (guild is not null)
            {
                result = result.WithAuthor(guild.GetUser(user.UserID));
            }
            result.Color = Discord.Color.Orange; 
            result = result.AddField("Error:", errorName);
            foreach (var item in additionalFields)
            {
                result = result.AddField(item.Key, item.Value);
            }
            return result.Build();
        }

        public CommandWarningLogEntry WithAdditonalField(string key, string value)
        {
            additionalFields[key] = value;
            return this;
        }
    }

    public class CommandUnhandledExceptionLogEntry : CommandLogEntry
    {
        private UserProfile user;

        private string commandName;

        private DateTime time;

        private SocketGuild guild;

        private Exception error;

        private Dictionary<string, string> additionalFields = new Dictionary<string, string>();

        private string correlationID => $"unhandledexception/{commandName}/{Math.Floor(time.Subtract(DateTime.UnixEpoch).TotalSeconds)}/{error.GetType().Name}";

        public CommandUnhandledExceptionLogEntry(ulong userid, string commandName, DateTime time, SocketGuild guild, Exception error)
        {
            this.user = ProfileManager.GetUserProfile(userid);
            this.commandName = commandName;
            this.time = time;
            this.guild = guild;
            this.error = error;
        }

        public override string ToLogString()
        {
            return $"<@{user.UserID}> executed {commandName} with an unhandled exception, on {Math.Floor(time.Subtract(DateTime.UnixEpoch).TotalSeconds)}. Correlation ID: {correlationID}";
        }

        public override Embed ToEmbed()
        {
            var result = new EmbedBuilder()
                .WithTitle($"User <@{user.UserID}> executed command {commandName} with an unhandled exception")
                .WithDescription($"<@{user.UserID}> executed {commandName} with an unhandled exception, on <t:{Math.Floor(time.Subtract(DateTime.UnixEpoch).TotalSeconds)}>");
            if (guild is not null)
            {
                result = result.WithAuthor(guild.GetUser(user.UserID));
            }
            result = result.AddField("Exception:", error.ToString());
            result = result.AddField("Correlation ID:", correlationID);
            result.Color = Discord.Color.Red;
            foreach (var item in additionalFields)
            {
                result = result.AddField(item.Key, item.Value);
            }
            return result.Build();
        }

        public CommandUnhandledExceptionLogEntry WithAdditonalField(string key, string value) {
            if (value.Trim() == "") {
                value = "No value";
            }
            additionalFields[key] = value;
            return this;
        }
    }

    public static class CommandLogger {

        public static SocketGuildChannel GetLoggingChannel(SocketGuild guild) {
            if (!GlobalGuildData.GetPerGuildData(guild.Id).ContainsKey("boteventlogs")) {
                return guild.Channels.Where(x => x.Name == Config.properties["auditing"]["boteventlogs"].ToObject<string>()).FirstOrDefault() as SocketGuildChannel;
            }
            return guild.Channels.FirstOrDefault(x => x.Id == GlobalGuildData.GetValueFromData<ulong>(guild.Id, "boteventlogs"));
        }

        public async static Task LogCommandAsync(ulong userid, SocketGuild guild, CommandLogEntry entry) {
            var user = ProfileManager.GetUserProfile(userid);
            //Logger.Debug(entry.ToLogString());
            user.DiagnosticLogs.Add(entry.ToLogString());
            if (guild is null) return;
            var channel = GetLoggingChannel(guild) as SocketTextChannel;
            if (channel == null) return;
            await channel.SendMessageAsync(embed: entry.ToEmbed());
        }
    }
}