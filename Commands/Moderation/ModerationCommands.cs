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

    public class PurgeInfractionTransaction : TransactionData
    {
        public UserProfile user;
    }

    public class PurgeNoteTransaction : TransactionData
    {
        public UserProfile user;
    }

    public class UnbanTransaction : TransactionData
    {
        public UserProfile user;
    }

    public static class Channels
    {
        public static SocketGuildChannel GetModerationChannel(SocketGuild guild)
        {
            return guild.Channels.Where(x => x.Name == Config.properties["auditing"]["moderationlogs"].ToObject<string>()).FirstOrDefault() as SocketGuildChannel;
        }
    }

    public static class ModerationFunctions
    {
        public static bool IsNotePresent(UserProfile user, ulong entryid)
        {
            var searched = user.BehaviourLogs.Logs.FirstOrDefault(x => x.ID == entryid && x is ModeratorNoteLogEntry);
            return searched != null;
        }

        public static bool IsInfractionPresent(UserProfile user, ulong entryid)
        {
            var searched = user.BehaviourLogs.Logs.FirstOrDefault(x => x.ID == entryid && x is not ModeratorNoteLogEntry);
            return searched != null;
        }

        public static bool RemoveNote(UserProfile user, ulong entryid)
        {
            if (!IsNotePresent(user, entryid) || !(user.BehaviourLogs.GetByID(entryid) is ModeratorNoteLogEntry))
            {
                return false;
            }
            var removed = user.BehaviourLogs.RemoveByID(entryid);
            return removed > 0;
        }

        public static bool RemoveInfraction(UserProfile user, ulong entryid)
        {
            if (!IsInfractionPresent(user, entryid) || !(user.BehaviourLogs.GetByID(entryid) is not ModeratorNoteLogEntry))
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

        public static bool DeleteAllInfractions(UserProfile user)
        {
            var prevlength = user.BehaviourLogs.Logs.Count;
            var removed = user.BehaviourLogs.Logs.Where(x => x is ModeratorNoteLogEntry);
            user.BehaviourLogs.Logs = removed.ToList();
            return removed.Count() < prevlength;
        }
    }

    public static class ModerationConstants
    {
        public static Requirements ModeratorRequirements => new Requirements(
            (context, _, _) =>
            {
                if (context.Interaction.IsDMInteraction)
                {
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
    public class NoteModule : OricordCommand
    {
        private static TransactionContainer transactions = new();

        [SlashCommand("create", "Creates moderator only private note for this user.")]
        public async Task SetNote(SocketGuildUser user, string note)
        {
            try
            {

                var userprofile = ProfileManager.GetUserProfile(user.Id);
                var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorNoteLogEntry>();
                if (userprofile.BehaviourLogs.Logs.Count == 0)
                {
                    logentry.ID = 1;
                }
                else
                {
                    logentry.ID = userprofile.BehaviourLogs.Logs.Select(x => x.ID).Max() + 1;
                }
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
                    await (Channels.GetModerationChannel(user.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                }

                await RespondAsync($"Made a private note for {user.Mention}, contents: {note}. Entry ID: {logentry.ID}", ephemeral: true);
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "note create", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "note create", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Parameter 'user'", $"{user.Mention}")
                    .WithAdditonalField("Parameter 'note'", $"{note}")
                );
            }
        }

        [SlashCommand("get", "Notes for this user page by page.")]
        public async Task GetNote(SocketGuildUser user, int page)
        {
            try
            {

                var userprofile = ProfileManager.GetUserProfile(user.Id);
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
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "note get", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "note get", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Parameter 'user'", $"{user.Mention}")
                    .WithAdditonalField("Parameter 'page'", $"{page}")
                );
            }
        }

        [SlashCommand("delete", "Deletes a note.")]
        public async Task DeleteNote(SocketGuildUser user, ulong entryid)
        {

            try
            {
                var userprofile = ProfileManager.GetUserProfile(user.Id);
                var checknote = ModerationFunctions.IsNotePresent(userprofile, entryid);
                if (!checknote)
                {

                    await RespondAsync($"No note found with ID {entryid}.", ephemeral: true);
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandSuccessLogEntry(Context.User.Id, "note delete", DateTime.UtcNow, Context.Guild as SocketGuild)
                        .WithAdditonalField("Additional remarks: ", $"No note found with ID {entryid}.")
                    );
                    return;
                }
                {
                    var embed = new EmbedBuilder().WithAuthor(user);
                    embed.Title = $"Moderator {Context.User.Mention} ({Context.User.GlobalName}) deleted a note for this user";
                    embed.AddField("Note ID", entryid);
                    embed.AddField("Note contents", userprofile.BehaviourLogs.GetByID(entryid).Format());
                    embed.AddField("Event Time: ", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>");
                    embed.WithFooter($"Moderator ID: {Context.User.Id} | Person ID: {user.Id} | Unix Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                    await (Channels.GetModerationChannel(user.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                }
                ModerationFunctions.RemoveNote(userprofile, entryid);
                await RespondAsync($"Deleted notes ");
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "note delete", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "note delete", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Parameter 'user'", $"{user.Mention}")
                    .WithAdditonalField("Parameter 'entryid'", $"{entryid}")
                );
            }
        }

        [SlashCommand("purge", "Deletes all notes for this user.")]
        public async Task PurgeNotes(SocketGuildUser user)
        {
            try
            {
                var userprofile = ProfileManager.GetUserProfile(user.Id);
                var maxconfirm = DateTime.UtcNow.AddMinutes(1);
                var transactiondata = new PurgeNoteTransaction();
                transactiondata.user = userprofile;
                var transaction = transactions.StartTransaction(maxconfirm, transactiondata);
                await RespondAsync($"Please confirm deletion of all notes for this user. (<t:{Math.Floor(maxconfirm.Subtract(DateTime.UnixEpoch).TotalSeconds)}:R>) ||(Transaction ID: {transaction})||", components:
                new ComponentBuilder().WithButton("Confirm", $"confirmpurgenote_{transaction}")
                .WithButton("Cancel", $"cancelpurgenote_{transaction}")
                .Build()
                , ephemeral: true);
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "note purge", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "note purge", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Parameter 'user'", $"{user.Mention}")
                );
            }
        }

        [ComponentInteraction("confirmpurgenote_*", true)]
        public async Task PurgeButton(string transactionid)
        {
            try
            {
                if (!transactions.CheckTransaction(transactionid, false))
                {
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandWarningLogEntry(Context.User.Id, "[note purge button]", DateTime.UtcNow, Context.Guild as SocketGuild, "Transaction expired.")
                        .WithAdditonalField("Transaction ID", $"{transactionid}")
                    );
                    await RespondAsync("Transaction expired.", ephemeral: true);
                    return;
                }
                var transdata = transactions.GetTransactionById(transactionid, true).TransactionData;
                var userprofile = ((PurgeNoteTransaction)transdata).user;
                var checknote = ModerationFunctions.DeleteAllNotes(userprofile);
                if (!checknote)
                {

                    await RespondAsync($"No notes found for this user. ||(Transaction ID: {transactionid})||", ephemeral: true);
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandSuccessLogEntry(Context.User.Id, "[note purge button]", DateTime.UtcNow, Context.Guild as SocketGuild)
                        .WithAdditonalField("Additional remarks: ", $"No notes are present for this user.")
                        .WithAdditonalField("Transaction ID", $"{transactionid}")
                    );
                    return;
                }
                {
                    var embed = new EmbedBuilder();
                    var user = await Context.Guild.GetUserAsync(userprofile.UserID);
                    if (user != null)
                    {
                        embed.WithAuthor(user);
                    }
                    embed.Title = $"Moderator {Context.User.Mention} ({Context.User.GlobalName}) deleted all notes for this user";
                    embed.AddField("Transaction ID", transactionid);
                    embed.AddField("Event Time: ", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>");
                    embed.WithFooter($"Moderator ID: {Context.User.Id} | Person ID: {userprofile.UserID} | Unix Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                    await (Channels.GetModerationChannel(Context.Guild as SocketGuild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                }
                await RespondAsync($"Deleted all notes for <@{userprofile.UserID}>. ||(Transaction ID: {transactionid})||");
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "[note purge button]", DateTime.UtcNow, Context.Guild as SocketGuild)
                    .WithAdditonalField("Transaction ID", $"{transactionid}")
                );
            }
            catch (Exception e)
            {

                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "[note purge button]", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Transaction ID", $"{transactionid}")
                );
            }
        }

        [ComponentInteraction("cancelpurgenote_*", true)]
        public async Task CancelButton(string transactionid)
        {
            try
            {
                if (!transactions.CheckTransaction(transactionid, true))
                {
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandWarningLogEntry(Context.User.Id, "[note purge cancel button]", DateTime.UtcNow, Context.Guild as SocketGuild, "Transaction expired.")
                        .WithAdditonalField("Transaction ID", $"{transactionid}")
                    );
                    await RespondAsync("Transaction expired.", ephemeral: true);
                    return;
                }
                await RespondAsync($"Transaction cancelled. ||(Transaction ID: {transactionid})||", ephemeral: true);
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "[note purge cancel button]", DateTime.UtcNow, Context.Guild as SocketGuild)
                    .WithAdditonalField("Transaction ID", $"{transactionid}")
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "[note purge cancel button]", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Transaction ID", $"{transactionid}")
                );
            }
        }

        public override Requirements GetRequirements()
        {

            return ModerationConstants.ModeratorRequirements;
        }
    }

    [Requirements(typeof(InfractionModule))]
    [Group("infraction", "Infraction commands")]
    public class InfractionModule : OricordCommand
    {

        private static TransactionContainer transactions = new();

        [SlashCommand("get", "Infractions for this user page by page.")]
        public async Task GetInfractions(SocketGuildUser user, int page)
        {
            try
            {
                var userprofile = ProfileManager.GetUserProfile(user.Id);
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
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "infraction get", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "infraction get", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("User", $"{user.Mention}")
                    .WithAdditonalField("Page", $"{page}")

                );
            }
        }

        [SlashCommand("delete", "Deletes an infraction")]
        public async Task DeleteInfraction(SocketGuildUser user, ulong entryid)
        {
            try
            {
                var userprofile = ProfileManager.GetUserProfile(user.Id);
                var checknote = ModerationFunctions.IsInfractionPresent(userprofile, entryid);
                if (!checknote)
                {
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandSuccessLogEntry(Context.User.Id, "infraction delete", DateTime.UtcNow, Context.Guild as SocketGuild)
                        .WithAdditonalField("Additional remarks", $"No infraction found with ID {entryid}.")
                    );
                    await RespondAsync($"No infraction found with ID {entryid}.", ephemeral: true);
                    return;
                }
                {
                    var embed = new EmbedBuilder().WithAuthor(user);
                    embed.Title = $"Moderator {Context.User.Mention} ({Context.User.GlobalName}) deleted an infraction for this user";
                    embed.AddField("Infraction ID", entryid);
                    embed.AddField("Infraction contents", userprofile.BehaviourLogs.GetByID(entryid).Format());
                    embed.AddField("Event Time: ", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>");
                    embed.WithFooter($"Moderator ID: {Context.User.Id} | Person ID: {user.Id} | Unix Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                    await (Channels.GetModerationChannel(user.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                }
                ModerationFunctions.RemoveInfraction(userprofile, entryid);
                await RespondAsync($"Deleted infraction.");
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "infraction delete", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "infraction delete", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("User", $"{user.Mention}")
                    .WithAdditonalField("Entry ID", $"{entryid}")
                );
            }


        }

        [SlashCommand("purge", "Deletes all infractions for this user.")]
        public async Task PurgeInfractions(SocketGuildUser user)
        {
            try
            {
                var userprofile = ProfileManager.GetUserProfile(user.Id);
                var maxconfirm = DateTime.UtcNow.AddMinutes(1);
                var transactiondata = new PurgeInfractionTransaction();
                transactiondata.user = userprofile;
                var transaction = transactions.StartTransaction(maxconfirm, transactiondata);
                await RespondAsync($"Please confirm deletion of all infractions for this user. (<t:{Math.Floor(maxconfirm.Subtract(DateTime.UnixEpoch).TotalSeconds)}:R>) ||(Transaction ID: {transaction})||", components:
                new ComponentBuilder().WithButton("Confirm", $"confirmpurgeinfractions_{transaction}")
                .WithButton("Cancel", $"cancelpurgeinfractions_{transaction}")
                .Build()
                , ephemeral: true);
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "infraction purge", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "infraction purge", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Parameter 'user'", $"{user.Mention}")
                );
            }
        }

        [ComponentInteraction("confirmpurgeinfractions_*", true)]
        public async Task PurgeButton(string transactionid)
        {
            try
            {
                if (!transactions.CheckTransaction(transactionid, false))
                {
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                            new CommandWarningLogEntry(Context.User.Id, "[infraction purge button]", DateTime.UtcNow, Context.Guild as SocketGuild, "Transaction expired.")
                            .WithAdditonalField("Transaction ID", $"{transactionid}")
                        );
                    await RespondAsync("Transaction expired.", ephemeral: true);
                    return;
                }
                var transdata = transactions.GetTransactionById(transactionid, true).TransactionData;
                var userprofile = ((PurgeInfractionTransaction)transdata).user;
                var checknote = ModerationFunctions.DeleteAllInfractions(userprofile);
                if (!checknote)
                {
                    await RespondAsync($"No infractions found for this user. ||(Transaction ID: {transactionid})||", ephemeral: true);
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandSuccessLogEntry(Context.User.Id, "[infraction purge button]", DateTime.UtcNow, Context.Guild as SocketGuild)
                        .WithAdditonalField("Additional remarks: ", $"No infractions are present for this user.")
                        .WithAdditonalField("Transaction ID", $"{transactionid}")
                    );
                    return;
                }
                {
                    var embed = new EmbedBuilder();
                    var user = await Context.Guild.GetUserAsync(userprofile.UserID);
                    if (user != null)
                    {
                        embed.WithAuthor(user);
                    }
                    embed.Title = $"Moderator {Context.User.Mention} ({Context.User.GlobalName}) deleted all infractions for this user";
                    embed.AddField("Transaction ID", transactionid);
                    embed.AddField("Event Time: ", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>");
                    embed.WithFooter($"Moderator ID: {Context.User.Id} | Person ID: {userprofile.UserID} | Unix Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                    await (Channels.GetModerationChannel(Context.Guild as SocketGuild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                }
                await RespondAsync($"Deleted all infractions for <@{userprofile.UserID}> ||(Transaction ID: {transactionid})||");
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "[infraction purge button]", DateTime.UtcNow, Context.Guild as SocketGuild)
                    .WithAdditonalField("Transaction ID", $"{transactionid}")
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "[infraction purge button]", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Transaction ID", $"{transactionid}")
                );
            }
        }

        [ComponentInteraction("cancelpurgeinfractions_*", true)]
        public async Task CancelButton(string transactionid)
        {
            try
            {
                if (!transactions.CheckTransaction(transactionid, true))
                {
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandWarningLogEntry(Context.User.Id, "[infraction purge cancel button]", DateTime.UtcNow, Context.Guild as SocketGuild, "Transaction expired.")
                        .WithAdditonalField("Transaction ID", $"{transactionid}")
                    );
                    await RespondAsync("Transaction expired.", ephemeral: true);
                    return;
                }
                await RespondAsync($"Transaction cancelled. ||(Transaction ID: {transactionid})||", ephemeral: true);
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "[infraction purge cancel button]", DateTime.UtcNow, Context.Guild as SocketGuild)
                    .WithAdditonalField("Transaction ID", $"{transactionid}")
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "[infraction purge cancel button]", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Transaction ID", $"{transactionid}")
                );
            }
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

        private static TransactionContainer transactions = new();

        [SlashCommand("warn", "Warns a user")]
        public async Task Warn(WarnType warntype, SocketGuildUser user, string reason)
        {
            try
            {
                var userprofile = ProfileManager.GetUserProfile(user.Id);
                var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorWarnLogEntry>();
                if (userprofile.BehaviourLogs.Logs.Count == 0)
                {
                    logentry.ID = 1;
                }
                else
                {
                    logentry.ID = userprofile.BehaviourLogs.Logs.Select(x => x.ID).Max() + 1;
                }
                logentry.WarningType = warntype;
                logentry.Reason = reason;
                logentry.ModeratorId = Context.User.Id;
                userprofile.BehaviourLogs.AddLogEntry(logentry);
                try
                {
                    {
                        var embed = new EmbedBuilder().WithAuthor(user);
                        embed.Title = $"Moderator {Context.User.Mention} ({Context.User.GlobalName}) issued a {warntype} for this user";
                        embed.AddField("Warning ID", logentry.ID);
                        embed.AddField("Formatted warning: ", logentry.Format());
                        embed.AddField("Reason: ", reason);
                        embed.AddField("Event Time: ", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>");
                        embed.WithFooter($"Moderator ID: {Context.User.Id} | Person ID: {user.Id} | Unix Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                        await (Channels.GetModerationChannel(user.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                    }
                    _ = user.SendMessageAsync($"You have been warned by {Context.User.Mention} for {reason}.");

                    await RespondAsync($"Warned {user.Mention} for {reason}.", ephemeral: true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    await RespondAsync("Could not send message to user.", ephemeral: true);
                }
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "warn", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "warn", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Parameter 'user'", $"{user.Mention}")
                    .WithAdditonalField("Parameter 'reason'", $"{reason}")
                    .WithAdditonalField("Parameter 'warntype'", $"{warntype}")
                );
            }
        }

        [SlashCommand("unban", "Unbans a user")]
        public async Task Unban(string useridentifier)
        {
            try
            {
                ulong userid = 0;
                try
                {
                    userid = ulong.Parse(useridentifier);
                }
                catch
                {

                    await RespondAsync("Invalid user ID.", ephemeral: true);
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                            new CommandWarningLogEntry(Context.User.Id, "unban", DateTime.UtcNow, Context.Guild as SocketGuild, "Invalid User ID.")
                            .WithAdditonalField("User ID", $"{useridentifier}")
                        );
                    return;
                }
                var userprofile = ProfileManager.GetUserProfile(userid);
                if (!userprofile.IsBanned)
                {

                    await RespondAsync($"User <@{userid}> is not banned.", ephemeral: true);
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                            new CommandWarningLogEntry(Context.User.Id, "unban", DateTime.UtcNow, Context.Guild as SocketGuild, "User is not banned.")
                            .WithAdditonalField("User ID", $"{useridentifier}")
                        );
                    return;
                }
                var maxconfirm = DateTime.UtcNow.AddMinutes(1);
                var transactiondata = new UnbanTransaction();
                transactiondata.user = userprofile;
                var transaction = transactions.StartTransaction(maxconfirm, transactiondata);
                await RespondAsync($"Please confirm unbanning this user. (<t:{Math.Floor(maxconfirm.Subtract(DateTime.UnixEpoch).TotalSeconds)}:R>) ||(Transaction ID: {transaction})||", components:
                new ComponentBuilder().WithButton("Confirm", $"confirmunban_{transaction}")
                .WithButton("Cancel", $"cancelunban_{transaction}")
                .Build()
                , ephemeral: true);
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandSuccessLogEntry(Context.User.Id, "unban", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "note purge", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Parameter 'useridentifier'", $"{useridentifier}")
                );
            }
        }

        [ComponentInteraction("confirmunban_*", true)]
        public async Task UnbanButton(string transactionid)
        {
            try
            {
                if (!transactions.CheckTransaction(transactionid, false))
                {
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandWarningLogEntry(Context.User.Id, "[unban button]", DateTime.UtcNow, Context.Guild as SocketGuild, "Transaction expired.")
                        .WithAdditonalField("Transaction ID", $"{transactionid}")
                    );
                    await RespondAsync("Transaction expired.", ephemeral: true);
                    return;
                }
                var transdata = transactions.GetTransactionById(transactionid, true).TransactionData;
                var userprofile = ((UnbanTransaction)transdata).user;
                await Context.Guild.RemoveBanAsync(userprofile.UserID);
                userprofile.IsBanned = false;
                {
                    var embed = new EmbedBuilder();
                    var user = await Context.Guild.GetUserAsync(userprofile.UserID);
                    if (user != null)
                    {
                        embed.WithAuthor(user);
                    }
                    embed.Title = $"Moderator {Context.User.Mention} ({Context.User.GlobalName}) unbanned this user";
                    embed.AddField("Transaction ID", transactionid);
                    embed.AddField("Event Time: ", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>");
                    embed.WithFooter($"Moderator ID: {Context.User.Id} | Person ID: {userprofile.UserID} | Unix Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                    await (Channels.GetModerationChannel(Context.Guild as SocketGuild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                }
                await RespondAsync($"Unbanned {userprofile.UserID}. ||(Transaction ID: {transactionid})||");
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "[unban button]", DateTime.UtcNow, Context.Guild as SocketGuild)
                    .WithAdditonalField("Transaction ID", $"{transactionid}")
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "[unban button]", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Transaction ID", $"{transactionid}")
                );
            }
        }

        [ComponentInteraction("cancelunban_*", true)]
        public async Task CancelButton(string transactionid)
        {
            try
            {
                if (!transactions.CheckTransaction(transactionid, true))
                {
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandWarningLogEntry(Context.User.Id, "[unban cancel button]", DateTime.UtcNow, Context.Guild as SocketGuild, "Transaction expired.")
                        .WithAdditonalField("Transaction ID", $"{transactionid}")
                    );
                    await RespondAsync("Transaction expired.", ephemeral: true);
                    return;
                }
                await RespondAsync($"Transaction cancelled. ||(Transaction ID: {transactionid})||", ephemeral: true);
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "[unban cancel button]", DateTime.UtcNow, Context.Guild as SocketGuild)
                    .WithAdditonalField("Transaction ID", $"{transactionid}")
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "[unban cancel button]", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                                    .WithAdditonalField("Transaction ID", $"{transactionid}")
                                );
            }
        }

        [SlashCommand("ban", "Warns a user")]
        public async Task Ban(SocketGuildUser user, string reason, int messageprunedays)
        {
            try
            {

                var userprofile = ProfileManager.GetUserProfile(user.Id);
                if (userprofile.IsBanned)
                {
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandWarningLogEntry(Context.User.Id, "unban", DateTime.UtcNow, Context.Guild as SocketGuild, "User is already banned.")
                        .WithAdditonalField("User", $"{user.Mention}")
                        .WithAdditonalField("Reason", $"{reason}")
                        .WithAdditonalField("Message Prune Days", $"{messageprunedays}")
                    );
                    await RespondAsync($"{user.Mention} is already banned.", ephemeral: true);
                    return;
                }
                var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorBanLogEntry>();
                if (userprofile.BehaviourLogs.Logs.Count == 0)
                {
                    logentry.ID = 1;
                }
                else
                {
                    logentry.ID = userprofile.BehaviourLogs.Logs.Select(x => x.ID).Max() + 1;
                }

                logentry.Reason = reason;
                logentry.ModeratorId = Context.User.Id;
                userprofile.BehaviourLogs.AddLogEntry(logentry);
                try
                {
                    {
                        var embed = new EmbedBuilder().WithAuthor(user);
                        embed.Title = $"Moderator {Context.User.Mention} ({Context.User.GlobalName}) banned this user";
                        embed.AddField("Ban Entry ID", logentry.ID);
                        embed.AddField("Formatted ban: ", logentry.Format());
                        embed.AddField("Message Prune days: ", messageprunedays);
                        embed.AddField("Reason: ", reason);
                        embed.AddField("Event Time: ", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>");
                        embed.WithFooter($"Moderator ID: {Context.User.Id} | Person ID: {user.Id} | Unix Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                        await (Channels.GetModerationChannel(user.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                    }
                    _ = user.SendMessageAsync($"You have been banned by {Context.User.Mention} for {reason}.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    await RespondAsync("Could not send message to user.", ephemeral: true);
                }

                await user.Guild.AddBanAsync(user, messageprunedays, reason);
                await RespondAsync($"Banned {user.Mention} for {reason}.", ephemeral: true);
                userprofile.IsBanned = true;
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "ban", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "ban", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("User", $"{user.Mention}")
                    .WithAdditonalField("Reason", $"{reason}")
                    .WithAdditonalField("Message Prune Days", $"{messageprunedays}")
                );
            }
        }

        [SlashCommand("mute", "Mutes a user for a certain time with a reason.")]
        public async Task Mute(SocketGuildUser user, string reason, TimeSpan duration)
        {
            try
            {
                var userprofile = ProfileManager.GetUserProfile(user.Id);
                if (userprofile.IsMuted)
                {
                    await RespondAsync($"{user.Mention} is already muted.", ephemeral: true);
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                            new CommandWarningLogEntry(Context.User.Id, "mute", DateTime.UtcNow, Context.Guild as SocketGuild, "User is already muted.")
                            .WithAdditonalField("User", $"{user.Mention}")
                            .WithAdditonalField("Reason", $"{reason}")
                            .WithAdditonalField("Duration", $"<t:{Math.Floor(DateTime.UtcNow.Add(duration).Subtract(DateTime.UnixEpoch).TotalSeconds)}:R>")
                        );
                    return;
                }

                var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorMuteLogEntry>();
                if (userprofile.BehaviourLogs.Logs.Count == 0)
                {
                    logentry.ID = 1;
                }
                else
                {
                    logentry.ID = userprofile.BehaviourLogs.Logs.Select(x => x.ID).Max() + 1;
                }
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
                    await (Channels.GetModerationChannel(user.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                }
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "mute", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            } catch (Exception e) {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "mute", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("User", $"{user.Mention}")
                    .WithAdditonalField("Reason", $"{reason}")
                    .WithAdditonalField("Duration", $"<t:{Math.Floor(DateTime.UtcNow.Add(duration).Subtract(DateTime.UnixEpoch).TotalSeconds)}:R>")
                );
            }
        }

        [SlashCommand("unmute", "Unmutes a user.")]
        public async Task Unmute(SocketGuildUser user, bool removefromrecord = false)
        {
            try {
                var userprofile = ProfileManager.GetUserProfile(user.Id);
                if (!userprofile.IsMuted)
                {
                    await RespondAsync($"{user.Mention} is already unmuted.", ephemeral: true);
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandWarningLogEntry(Context.User.Id, "unmute", DateTime.UtcNow, Context.Guild as SocketGuild, "User is already unmuted.")
                        .WithAdditonalField("User", $"{user.Mention}")
                        .WithAdditonalField("Remove from record", $"{removefromrecord}")
                    );
                    return;
                }
                GlobalTimerStorage.GetTimerByID(userprofile.MutedTimerID).OnTarget();
                if (removefromrecord)
                {
                    userprofile.BehaviourLogs.RemoveByID(userprofile.BehaviourLogs.Logs.Where(x => x is ModeratorMuteLogEntry).Select(x => x.ID).Last());
                }
                await RespondAsync($"Unmuted {user.Mention}.", ephemeral: true);
                {
                    var embed = new EmbedBuilder().WithAuthor(user);
                    embed.Title = $"Moderator {Context.User.Mention} ({Context.User.GlobalName}) unmuted for this user";
                    embed.AddField("Mute timer ID: ", $"{userprofile.MutedTimerID}");
                    embed.AddField("Event Time: ", $"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}>");
                    embed.WithFooter($"Moderator ID: {Context.User.Id} | Person ID: {user.Id} | Unix Timestamp: {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
                    await (Channels.GetModerationChannel(user.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                }
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "unmute", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            } catch (Exception e) {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "unmute", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("User", $"{user.Mention}")
                    .WithAdditonalField("Remove from record", $"{removefromrecord}")
                );
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
                return result[page - 1];
            }
        }
    }
}