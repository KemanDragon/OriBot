using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using main;
using OldOriBot.Data.Commands.ArgData;

using OriBot.Commands.RequirementEngine;
using OriBot.Framework;
using OriBot.Framework.UserBehaviour;
using OriBot.Framework.UserProfiles;
using OriBot.Framework.UserProfiles.SaveableTimer;
using OriBot.Transactions;
using OriBot.Utilities;

namespace OriBot.Commands
{
    public enum LogType
    {
        Major,
        Minor
    }

    public class PurgeInfarctionTransaction : TransactionData {
        public UserProfile user;
    }

    public class PurgeNoteTransaction : TransactionData
    {
        public UserProfile user;
    }

    public static class ModerationFunctions {
        public static bool IsNotePresent(UserProfile user, ulong entryid) {
            var searched = user.BehaviourLogs.Logs.FirstOrDefault(x => x.ID == entryid && x is ModeratorNoteLogEntry);
            return searched != null;
        }

        public static bool IsInfarctionPresent(UserProfile user, ulong entryid)
        {
            var searched = user.BehaviourLogs.Logs.FirstOrDefault(x => x.ID == entryid && x is not ModeratorNoteLogEntry);
            return searched != null;
        }

        public static bool RemoveNote(UserProfile user, ulong entryid) {
            if (!IsNotePresent(user, entryid) || !(user.BehaviourLogs.GetByID(entryid) is ModeratorNoteLogEntry)) {
                return false;
            }
            var removed = user.BehaviourLogs.RemoveByID(entryid);
            return removed > 0;
        }

        public static bool RemoveInfarction(UserProfile user, ulong entryid)
        {
            if (!IsInfarctionPresent(user, entryid) || !(user.BehaviourLogs.GetByID(entryid) is not ModeratorNoteLogEntry))
            {
                return false;
            }
            var removed = user.BehaviourLogs.RemoveByID(entryid);
            return removed > 0;
        }

        public static bool DeleteAllNotes(UserProfile user)
        {
            var prevlength = user.BehaviourLogs.Logs.Count;
            var removed = user.BehaviourLogs.Logs.Where(x => x is not ModeratorNoteLogEntry);
            user.BehaviourLogs.Logs = removed.ToList();
            return removed.Count() < prevlength;
        }

        public static bool DeleteAllInfarctions(UserProfile user)
        {
            var prevlength = user.BehaviourLogs.Logs.Count;
            var removed = user.BehaviourLogs.Logs.Where(x => x is ModeratorNoteLogEntry);
            user.BehaviourLogs.Logs = removed.ToList();
            return removed.Count() < prevlength;
        }
    }

    public static class ModerationConstants {
        public static Requirements ModeratorRequirements => new Requirements(
            (context,_,_) => {
            if (context.Interaction.IsDMInteraction) {
                context.Interaction.RespondAsync("This command is not available in DMs.", ephemeral: true);
                return false;
            }
            return true;
        },
        (context, commandinfo, services) =>
            {
                var res = ((List<long>)Config.properties["oricordServers"].ToObject<List<long>>()).Contains((long)context.Guild.Id);
                return res;
            });
    }

    [Requirements(typeof(NoteModule))]
    [Group("note", "Note commands")]
    public class NoteModule : OricordCommand {
        private static TransactionContainer transactions = new();

        [SlashCommand("create", "Creates moderator only private note for this user.")]
        public async Task SetNote(SocketGuildUser user, string note) {
            var userprofile = ProfileManager.GetUserProfile(user);
            var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorNoteLogEntry>();
            logentry.ID = userprofile.BehaviourLogs.Logs.Select(x => x.ID).Max() + 1;
            logentry.Note = note;
            logentry.ModeratorId = Context.User.Id;
            userprofile.BehaviourLogs.AddLogEntry(logentry);
            {
                var embed = new EmbedBuilder().WithAuthor(user);
                embed.Title = $"Moderator {Context.User.Mention} ({Context.User.GlobalName}) created a note for this user";
                embed.AddField("Note", note);
                embed.AddField("Note ID", logentry.ID);
                embed.AddField("Event Time: ", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>");
                embed.WithFooter($"Moderator ID: {Context.User.Id} | Person ID: {user.Id} | Unix Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                await user.Guild.SystemChannel.SendMessageAsync("", embed: embed.Build());
            }
            
            await RespondAsync($"Made a private note for {user.Mention}, contents: {note}. Entry ID: {logentry.ID}", ephemeral: true);
        }

        [SlashCommand("get", "Notes for this user page by page.")]
        public async Task GetNote(SocketGuildUser user, int page)
        {
            var userprofile = ProfileManager.GetUserProfile(user);
            var embed = new EmbedBuilder().WithAuthor(user);
            var pagestart = ModerationModule.ItemsPerPage * (page - 1);
            var pageend = ModerationModule.ItemsPerPage * page;
            await DeferAsync();
            {
                // Notes
                var builtstring = "";
                var listof = from x in userprofile.BehaviourLogs.Logs where x is ModeratorNoteLogEntry select x;
                var count = listof.Count();
                var paginated = ModerationModule.PaginateArray(listof.ToArray(), ModerationModule.ItemsPerPage, page);
                var truncatedduetolength = false;
                foreach (var item in paginated)
                {
                    var added = builtstring + item.Format() + "\n";
                    if (added.Length > 1024)
                    {
                        truncatedduetolength = true;
                        break;
                    }
                    builtstring = added;
                }
                if (builtstring.Length < 1)
                {
                    builtstring += "This user has no Notes.";
                }
                embed.AddField($"Notes (showing from {Math.Max(Math.Min(pageend, count) - ModerationModule.ItemsPerPage, 0)}-{Math.Min(pageend, count)}) {(truncatedduetolength ? "(Truncated due to 1024 char limit)" : "")}", builtstring);
            }
            await FollowupAsync(embed: embed.Build(), ephemeral: true);
        }

        [SlashCommand("delete", "Deletes a note.")]
        public async Task DeleteNote(SocketGuildUser user, ulong entryid)
        {
            var userprofile = ProfileManager.GetUserProfile(user);
            var checknote = ModerationFunctions.IsNotePresent(userprofile, entryid);
            if (!checknote)
            {
                await RespondAsync($"No note found with ID {entryid}.", ephemeral: true);
                return;
            }
            {
                var embed = new EmbedBuilder().WithAuthor(user);
                embed.Title = $"Moderator {Context.User.Mention} ({Context.User.GlobalName}) deleted a note for this user";
                embed.AddField("Note ID", entryid);
                embed.AddField("Note contents", userprofile.BehaviourLogs.GetByID(entryid).Format());
                embed.AddField("Event Time: ", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>");
                embed.WithFooter($"Moderator ID: {Context.User.Id} | Person ID: {user.Id} | Unix Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                await user.Guild.SystemChannel.SendMessageAsync("", embed: embed.Build());
            }
            ModerationFunctions.RemoveNote(userprofile, entryid);
            await RespondAsync($"Deleted notes ");

        }

        [SlashCommand("purge", "Deletes all notes for this user.")]
        public async Task PurgeNotes(SocketGuildUser user)
        {
            
            var userprofile = ProfileManager.GetUserProfile(user);
            var maxconfirm = DateTime.UtcNow.AddMinutes(1);
            var transactiondata = new PurgeNoteTransaction();
            transactiondata.user = userprofile;
            var transaction = transactions.StartTransaction(maxconfirm, transactiondata);
            await RespondAsync($"Please confirm deletion of all notes for this user. (<t:{Math.Floor(maxconfirm.Subtract(DateTime.UnixEpoch).TotalSeconds)}:R>) ||(Transaction ID: {transaction})||", components: 
            new ComponentBuilder().WithButton("Confirm", $"confirmpurgenote_{transaction}")
            .WithButton("Cancel", $"cancelpurgenote_{transaction}")
            .Build()
            , ephemeral: true);

        }

        [ComponentInteraction("confirmpurgenote_*", true)]
        public async Task PurgeButton(string transactionid)
        {
            if (!transactions.CheckTransaction(transactionid, false)) {
                await RespondAsync("Transaction expired.", ephemeral: true);
                return;
            }
            var transdata = transactions.GetTransactionById(transactionid, true).TransactionData;
            var userprofile = ((PurgeNoteTransaction)transdata).user;
            var checknote = ModerationFunctions.DeleteAllNotes(userprofile);
            if (!checknote)
            {
                await RespondAsync($"No notes found for this user. ||(Transaction ID: {transactionid})||", ephemeral: true);
                return;
            }
            {
                var embed = new EmbedBuilder();
                var user = await Context.Guild.GetUserAsync(userprofile.Member.Id);
                if (user != null)
                {
                    embed.WithAuthor(user);
                }
                embed.Title = $"Moderator {Context.User.Mention} ({Context.User.GlobalName}) deleted all notes for this user";
                embed.AddField("Transaction ID", transactionid);
                embed.AddField("Event Time: ", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>");
                embed.WithFooter($"Moderator ID: {Context.User.Id} | Person ID: {userprofile.Member.Id} | Unix Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                await (await this.Context.Guild.GetSystemChannelAsync()).SendMessageAsync("", embed: embed.Build());
            }
            await RespondAsync($"Deleted all notes for <@{userprofile.Member.Id}>. ||(Transaction ID: {transactionid})||");
        }

        [ComponentInteraction("cancelpurgenote_*", true)]
        public async Task CancelButton(string transactionid)
        {
            if (!transactions.CheckTransaction(transactionid, true))
            {
                await RespondAsync("Transaction expired.", ephemeral: true);
                return;
            }
            await RespondAsync($"Transaction cancelled. ||(Transaction ID: {transactionid})||", ephemeral: true);
        }

        public override Requirements GetRequirements()
        {
            
            return ModerationConstants.ModeratorRequirements;
        }
    }

    [Requirements(typeof(InfarctionModule))]
    [Group("infarction", "Infarction commands")]
    public class InfarctionModule : OricordCommand {

        private static TransactionContainer transactions = new();

        [SlashCommand("get", "Infarctions for this user page by page.")]
        public async Task GetInfarctions(SocketGuildUser user, int page)
        {
            var userprofile = ProfileManager.GetUserProfile(user);
            var embed = new EmbedBuilder().WithAuthor(user);
            var pagestart = ModerationModule.ItemsPerPage * (page - 1);
            var pageend = ModerationModule.ItemsPerPage * page;
            await DeferAsync();
            {
                // Warns
                var builtstring = "";
                var listof = from x in userprofile.BehaviourLogs.Logs where x is ModeratorWarnLogEntry select x;
                var count = listof.Count();
                var paginated = ModerationModule.PaginateArray(listof.ToArray(), ModerationModule.ItemsPerPage, page);
                var truncatedduetolength = false;
                foreach (var item in paginated)
                {
                    var added = builtstring + item.Format() + "\n";
                    if (added.Length > 1024)
                    {
                        truncatedduetolength = true;
                        break;
                    }
                    builtstring = added;
                }
                if (builtstring.Length < 1)
                {
                    builtstring += "This user has no Warnings.";
                }
                embed.AddField($"Warnings (showing from {Math.Max(Math.Min(pageend, count) - ModerationModule.ItemsPerPage, 0)}-{Math.Min(pageend, count)}) {(truncatedduetolength ? "(Truncated due to 1024 char limit)" : "")}", builtstring);
            }
            {
                // Mutes
                var builtstring = "";
                var listof = from x in userprofile.BehaviourLogs.Logs where x is ModeratorMuteLogEntry select x;
                var count = listof.Count();
                var paginated = ModerationModule.PaginateArray(listof.ToArray(), ModerationModule.ItemsPerPage, page);
                var truncatedduetolength = false;
                foreach (var item in paginated)
                {
                    var added = builtstring + item.Format() + "\n";
                    if (added.Length > 1024)
                    {
                        truncatedduetolength = true;
                        break;
                    }
                    builtstring = added;
                }
                if (builtstring.Length < 1)
                {
                    builtstring += "This user has no Warnings.";
                }
                embed.AddField($"Mutes (showing from {Math.Max(Math.Min(pageend, count) - ModerationModule.ItemsPerPage, 0)}-{Math.Min(pageend, count)}) {(truncatedduetolength ? "(Truncated due to 1024 char limit)" : "")}", builtstring);
            }
            await FollowupAsync(embed: embed.Build(), ephemeral: true);
        }

        [SlashCommand("delete", "Deletes an infarction")]
        public async Task DeleteInfarction(SocketGuildUser user, ulong entryid)
        {
            var userprofile = ProfileManager.GetUserProfile(user);
            var checknote = ModerationFunctions.IsInfarctionPresent(userprofile, entryid);
            if (!checknote)
            {
                await RespondAsync($"No infarction found with ID {entryid}.", ephemeral: true);
                return;
            }
            {
                var embed = new EmbedBuilder().WithAuthor(user);
                embed.Title = $"Moderator {Context.User.Mention} ({Context.User.GlobalName}) deleted an infarction for this user";
                embed.AddField("Infarction ID", entryid);
                embed.AddField("Infarction contents", userprofile.BehaviourLogs.GetByID(entryid).Format());
                embed.AddField("Event Time: ", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>");
                embed.WithFooter($"Moderator ID: {Context.User.Id} | Person ID: {user.Id} | Unix Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                await user.Guild.SystemChannel.SendMessageAsync("", embed: embed.Build());
            }
            ModerationFunctions.RemoveInfarction(userprofile, entryid);
            await RespondAsync($"Deleted infarction.");

        }

        [SlashCommand("purge", "Deletes all infarctions for this user.")]
        public async Task PurgeInfarctions(SocketGuildUser user)
        {
            var userprofile = ProfileManager.GetUserProfile(user);
            var maxconfirm = DateTime.UtcNow.AddMinutes(1);
            var transactiondata = new PurgeInfarctionTransaction();
            transactiondata.user = userprofile;
            var transaction = transactions.StartTransaction(maxconfirm, transactiondata);
            await RespondAsync($"Please confirm deletion of all infarctions for this user. (<t:{Math.Floor(maxconfirm.Subtract(DateTime.UnixEpoch).TotalSeconds)}:R>) ||(Transaction ID: {transaction})||", components:
            new ComponentBuilder().WithButton("Confirm", $"confirmpurgeinfarctions_{transaction}")
            .WithButton("Cancel", $"cancelpurgeinfarctions_{transaction}")
            .Build()
            , ephemeral: true);

        }

        [ComponentInteraction("confirmpurgeinfarctions_*", true)]
        public async Task PurgeButton(string transactionid)
        {
            if (!transactions.CheckTransaction(transactionid, false))
            {
                await RespondAsync("Transaction expired.", ephemeral: true);
                return;
            }
            var transdata = transactions.GetTransactionById(transactionid, true).TransactionData;
            var userprofile = ((PurgeInfarctionTransaction)transdata).user;
            var checknote = ModerationFunctions.DeleteAllInfarctions(userprofile);
            if (!checknote)
            {
                await RespondAsync($"No infarctions found for this user. ||(Transaction ID: {transactionid})||", ephemeral: true);
                return;
            }
{
                var embed = new EmbedBuilder();
                var user = await Context.Guild.GetUserAsync(userprofile.Member.Id);
                if (user != null)
                {
                    embed.WithAuthor(user);
                }
                embed.Title = $"Moderator {Context.User.Mention} ({Context.User.GlobalName}) deleted all infarctions for this user";
                embed.AddField("Transaction ID", transactionid);
                embed.AddField("Event Time: ", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>");
                embed.WithFooter($"Moderator ID: {Context.User.Id} | Person ID: {userprofile.Member.Id} | Unix Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                await (await this.Context.Guild.GetSystemChannelAsync()).SendMessageAsync("", embed: embed.Build());
            }
            await RespondAsync($"Deleted all infarctions for <@{userprofile.Member.Id}> ||(Transaction ID: {transactionid})||");
        }

        [ComponentInteraction("cancelpurgeinfarctions_*", true)]
        public async Task CancelButton(string transactionid)
        {
            if (!transactions.CheckTransaction(transactionid, true))
            {
                await RespondAsync("Transaction expired.", ephemeral: true);
                return;
            }
            await RespondAsync($"Transaction cancelled. ||(Transaction ID: {transactionid})||", ephemeral: true);
        }

        public override Requirements GetRequirements()
        {

            return ModerationConstants.ModeratorRequirements;
        }
    }

    [Requirements(typeof(ModerationModule))]
    public class ModerationModule : OricordCommand
    {
        public static string NormalRoleName => Config.properties["rolenames"]["normal"];

        public static string MutedRoleName => Config.properties["rolenames"]["muted"];

        public static int ItemsPerPage => Config.properties["moderation"]["itemsPerPage"];

        public static bool EnableConfirmations => Config.properties["moderation"]["enableConfirmations"];

        [SlashCommand("warn", "Warns a user")]
        public async Task Warn(WarnType warntype, SocketGuildUser user, string reason)
        {
            var userprofile = ProfileManager.GetUserProfile(user);
            var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorWarnLogEntry>();
            logentry.ID = userprofile.BehaviourLogs.Logs.Select(x => x.ID).Max() + 1;
            logentry.WarningType = warntype;
            logentry.Reason = reason;
            logentry.ModeratorId = Context.User.Id;
            userprofile.BehaviourLogs.AddLogEntry(logentry);
            try
            {
                _ = user.SendMessageAsync($"You have been warned by {Context.User.Mention} for {reason}.");
                {
                    var embed = new EmbedBuilder().WithAuthor(user);
                    embed.Title = $"Moderator {Context.User.Mention} ({Context.User.GlobalName}) issued a {warntype} for this user";
                    embed.AddField("Warning ID", logentry.ID);
                    embed.AddField("Formatted warning: ", logentry.Format());
                    embed.AddField("Reason: ", reason);
                    embed.AddField("Event Time: ", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>");
                    embed.WithFooter($"Moderator ID: {Context.User.Id} | Person ID: {user.Id} | Unix Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                    await user.Guild.SystemChannel.SendMessageAsync("", embed: embed.Build());
                }
                await RespondAsync($"Warned {user.Mention} for {reason}.", ephemeral: true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await RespondAsync("Could not send message to user.", ephemeral: true);
            }
        }

        [SlashCommand("mute", "Mutes a user for a certain time with a reason.")]
        public async Task Mute(SocketGuildUser user, string reason, TimeSpan duration)
        {
            
            var userprofile = ProfileManager.GetUserProfile(user);
            if (userprofile.IsMuted)
            {
                await RespondAsync($"{user.Mention} is already muted.", ephemeral: true);
                return;
            }

            var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorMuteLogEntry>();
            logentry.ID = userprofile.BehaviourLogs.Logs.Select(x => x.ID).Max() + 1;
            logentry.Reason = reason;
            logentry.ModeratorId = Context.User.Id;
            logentry.MuteEndUTC = DateTime.UtcNow.Add(duration);
            userprofile.BehaviourLogs.AddLogEntry(logentry);
            try
            {
                _ = user.SendMessageAsync($"You have been muted by {Context.User.Mention} for {reason}, and you will be unmuted <t:{Math.Floor(DateTime.UtcNow.Add(duration).ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds)}:R>");

                await RespondAsync($"Muted {user.Mention} for {reason}.", ephemeral: true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                
                await RespondAsync("Could not send message to user.", ephemeral: true);
            }
            var mutedrole = Context.Guild.Roles.Where(x => x.Name == MutedRoleName).FirstOrDefault();
            var normalrole = Context.Guild.Roles.Where(x => x.Name == NormalRoleName).FirstOrDefault();
            await user.RemoveRoleAsync(normalrole);
            await user.AddRoleAsync(mutedrole);
            var timer = SaveableTimerRegistry.CreateTimer<MuteTimer>(DateTime.UtcNow.Add(duration));
            timer.SetData(Context.Guild.Id, user.Id);
            userprofile.MutedTimerID = timer.InstanceUID;
            GlobalTimerStorage.AddTimer(timer);
            {
                var embed = new EmbedBuilder().WithAuthor(user);
                embed.Title = $"Moderator {Context.User.Mention} ({Context.User.GlobalName}) muted for this user";
                embed.AddField("Mute ID:", logentry.ID);
                embed.AddField("Formatted mute: ", logentry.Format());
                embed.AddField("Reason: ", reason);
                embed.AddField("Mute expiry: ", $"<t:{Math.Floor(logentry.MuteEndUTC.Subtract(DateTime.UnixEpoch).TotalSeconds)}:R> (<t:{Math.Floor(logentry.MuteEndUTC.Subtract(DateTime.UnixEpoch).TotalSeconds)}>)");
                embed.AddField("Mute timer ID: ", $"{timer.InstanceUID}");
                embed.AddField("Event Time: ", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>");
                embed.WithFooter($"Moderator ID: {Context.User.Id} | Person ID: {user.Id} | Unix Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                await user.Guild.SystemChannel.SendMessageAsync("", embed: embed.Build());
            }
        }

        [SlashCommand("unmute", "Unmutes a user.")]
        public async Task Unmute(SocketGuildUser user, bool removefromrecord = false)
        {
            var userprofile = ProfileManager.GetUserProfile(user);
            if (!userprofile.IsMuted)
            {
                await RespondAsync($"{user.Mention} is already unmuted.", ephemeral: true);
                return;
            }
            GlobalTimerStorage.GetTimerByID(userprofile.MutedTimerID).OnTarget();
            if (removefromrecord) {
                userprofile.BehaviourLogs.RemoveByID(userprofile.BehaviourLogs.Logs.Where(x => x is ModeratorMuteLogEntry).Select(x => x.ID).Last());
            }
            await RespondAsync($"Unmuted {user.Mention}.", ephemeral: true);
            {
                var embed = new EmbedBuilder().WithAuthor(user);
                embed.Title = $"Moderator {Context.User.Mention} ({Context.User.GlobalName}) unmuted for this user";
                embed.AddField("Mute timer ID: ", $"{userprofile.MutedTimerID}");
                embed.AddField("Event Time: ", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>");
                embed.WithFooter($"Moderator ID: {Context.User.Id} | Person ID: {user.Id} | Unix Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                await user.Guild.SystemChannel.SendMessageAsync("", embed: embed.Build());
            }
        }

        public override Requirements GetRequirements()
        {
            return ModerationConstants.ModeratorRequirements;
        }

        public static List<T> PaginateArray<T>(T[] array, int pageSize, int page)
        {
            var result = new List<List<T>>();
            var index = 0;
            foreach (var item in array)
            {
                if (index % pageSize == 0)
                {
                    result.Add(new List<T>());
                }
                result.Last().Add(item);
                index++;
            }
            if (result.Count == 0)
            {
                return new();
            }
            if (page - 1 > result.Count - 1)
            {
                page = result.Count - 1;
                return result[page];
            }
            else
            {
                return result[page-1];
            }
        }
    }
}