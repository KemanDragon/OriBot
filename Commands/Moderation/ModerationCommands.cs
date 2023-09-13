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
using OriBot.Framework.UserProfiles.Badges;
using OriBot.Framework.UserProfiles.SaveableTimer;
using OriBot.GuildData;
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

        public string reason;
    }

    public class PurgeNoteTransaction : TransactionData
    {
        public UserProfile user;

        public string reason;
    }

    public class UnbanTransaction : TransactionData
    {
        public UserProfile user;

        public string reason;
    }

    public static class Channels
    {
        public static SocketGuildChannel GetModerationChannel(SocketGuild guild)
        {
            if (!GlobalGuildData.GetPerGuildData(guild.Id).ContainsKey("moderationlogs"))
            {
                return guild.Channels.Where(x => x.Name == Config.properties["auditing"]["moderationlogs"].ToObject<string>()).FirstOrDefault() as SocketGuildChannel;
            }
            return guild.Channels.FirstOrDefault(x => x.Id == GlobalGuildData.GetValueFromData<ulong>(guild.Id, "moderationlogs"));
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
            var searched = user.BehaviourLogs.Logs.FirstOrDefault(x => x.ID == entryid && x is MajorLog);
            return searched != null;
        }

        public static bool RemoveNote(UserProfile user, ulong entryid)
        {
            if (!IsNotePresent(user, entryid) || (user.BehaviourLogs.GetByID(entryid) is not ModeratorNoteLogEntry))
            {
                return false;
            }
            var removed = user.BehaviourLogs.RemoveByID(entryid);
            return removed > 0;
        }

        public static bool RemoveInfraction(UserProfile user, ulong entryid)
        {
            if (!IsInfractionPresent(user, entryid) || (user.BehaviourLogs.GetByID(entryid) is not MajorLog))
            {
                return false;
            }
            var removed = user.BehaviourLogs.RemoveByID(entryid);
            return removed > 0;
        }

        public static bool DeleteAllNotes(UserProfile user)
        {
            var prevlength = user.BehaviourLogs.Logs.Count;
            var removed = user.BehaviourLogs.Logs.ToList();
            removed.RemoveAll(x => x is ModeratorNoteLogEntry);
            user.BehaviourLogs.Logs = removed;
            return removed.Count() < prevlength;
        }

        public static bool DeleteAllInfractions(UserProfile user)
        {
            var prevlength = user.BehaviourLogs.Logs.Count;
            var removed = user.BehaviourLogs.Logs.ToList();
            removed.RemoveAll(x => x is MajorLog);
            user.BehaviourLogs.Logs = removed;
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
            (context, _, _) =>
            {
                var res = ((List<long>)Config.properties["oricordServers"].ToObject<List<long>>()).Contains((long)context.Guild.Id);
                return res;
            },
            (context, _, _) => {
                var userprofile = ProfileManager.GetUserProfile(context.User.Id);
                if (userprofile.GetPermissionLevel(context.Guild.Id) < PermissionLevel.Moderator) {
                    _ = context.Interaction.RespondAsync("You must be a Moderator or higher to execute this command.", ephemeral: true);
                    return false;
                }
                return true;
            });
    }

    [Requirements(typeof(NoteModule))]
    
    public class NoteModule : OricordCommand
    {
        private static TransactionContainer transactions = new();

       

        [SlashCommand("notes", "View details for an individual note, or a list of all notes for ths user.")]
        public async Task ViewNote(SocketGuildUser user, int page = 1, ulong? noteid = null)
        {
            try
            {
                var userprofile = ProfileManager.GetUserProfile(user.Id);
                var embed = new EmbedBuilder().WithAuthor(user);
                var pagestart = ModerationModule.ItemsPerPage * (page - 1);
                var pageend = ModerationModule.ItemsPerPage * page;
                if (noteid == null)
                {
                    {
                        // Notes
                        var builtstring = "";
                        var listof = from x in userprofile.BehaviourLogs.Logs where x is ModeratorNoteLogEntry select x;
                        var count = listof.Count();
                        var paginated = ModerationModule.PaginateArray(listof.ToArray(), ModerationModule.ItemsPerPage, page);
                        var truncatedduetolength = false;
                        foreach (var item in paginated)
                        {
                            var added = builtstring + item.FormatSimple() + "\n";
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
                }
                else
                {
                    var logentry = userprofile.BehaviourLogs.GetByID(noteid.Value);
                    if (logentry == null)
                    {
                        await RespondAsync($"No such note ID: {noteid.Value}", ephemeral: true);
                        await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                            new CommandWarningLogEntry(Context.User.Id, "note", DateTime.UtcNow, Context.Guild as SocketGuild, $"No such note ID: {noteid}")
                            .WithAdditonalField("User", $"{user.Mention}")
                            .WithAdditonalField("Page", $"{page}")
                            .WithAdditonalField("Infraction ID", $"{noteid.Value}")
                        );
                        return;
                    }
                    embed = logentry.FormatDetailed();
                    embed = embed.WithAuthor(user);
                    embed = embed.WithFooter(embed.Footer.Text + $" | Person ID: {user.Id}");

                }
                await RespondAsync("", embed: embed.Build());
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "note", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "note", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("User", $"{user.Mention}")
                    .WithAdditonalField("Page", $"{page}")
                    .WithAdditonalField("Note ID", $"#{noteid}")
                );
            }
        }

        [SlashCommand("createnote", "Creates moderator only private note for this user.")]
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
                    var embed = logentry.FormatDetailed();
                    embed = embed.WithAuthor(user);
                    embed = embed.WithFooter(embed.Footer.Text + $" | Person ID: {user.Id}");
                    await (Channels.GetModerationChannel(user.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                }

                await RespondAsync($"Made a private note for {user.Mention}, contents: {note}. Entry ID: {logentry.ID}", ephemeral: true);
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "createnote", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "createnote", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Parameter 'user'", $"{user.Mention}")
                    .WithAdditonalField("Parameter 'note'", $"{note}")
                );
            }
        }

        [SlashCommand("delnote", "Deletes a note.")]
        public async Task DeleteNote(SocketGuildUser user, ulong? entryid = null, string reason = "No reason was given.")
        {

            try
            {
                if (!entryid.HasValue) {
                    if (ModerationModule.EnableConfirmations) {
                        var userprofile = ProfileManager.GetUserProfile(user.Id);
                        var maxconfirm = DateTime.UtcNow.AddMinutes(1);
                        var transactiondata = new PurgeNoteTransaction
                        {
                            reason = reason,
                            user = userprofile
                        };
                        var transaction = transactions.StartTransaction(maxconfirm, transactiondata);
                        await RespondAsync($"Please confirm deletion of all notes for this user. (<t:{Math.Floor(maxconfirm.Subtract(DateTime.UnixEpoch).TotalSeconds)}:R>) ||(Transaction ID: {transaction})||", components:
                        new ComponentBuilder().WithButton("Confirm", $"confirmpurgenote_{transaction}")
                        .WithButton("Cancel", $"cancelpurgenote_{transaction}")
                        .Build()
                        , ephemeral: true);
                        await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                            new CommandSuccessLogEntry(Context.User.Id, "delnote", DateTime.UtcNow, Context.Guild as SocketGuild)
                        );
                    } else {
                        var userprofile = ProfileManager.GetUserProfile(user.Id);
                        var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorPurgeNoteLogEntry>();
                        logentry.AmountDeleted = (ulong)userprofile.BehaviourLogs.Logs.Where(x => x is ModeratorNoteLogEntry).Count();
                        logentry.TransactionID = "";
                        var checknote = ModerationFunctions.DeleteAllNotes(userprofile);
                        if (userprofile.BehaviourLogs.Logs.Count == 0)
                        {
                            logentry.ID = 1;
                        }
                        else
                        {
                            logentry.ID = userprofile.BehaviourLogs.Logs.Select(x => x.ID).Max() + 1;
                        }
                        if (!checknote)
                        {

                            await RespondAsync($"No notes found for this user. ||(Transaction ID: \"Transactions are disabled, due to config flag\")||", ephemeral: true);
                            await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                                new CommandSuccessLogEntry(Context.User.Id, "delnote", DateTime.UtcNow, Context.Guild as SocketGuild)
                                .WithAdditonalField("Additional remarks: ", $"No notes are present for this user.")
                                .WithAdditonalField("Transaction ID", $"Transactions are disabled, due to config flag")
                            );
                            return;
                        }
                        logentry.Reason = reason;
                        logentry.ModeratorId = Context.User.Id;
                        {
                            var embed = logentry.FormatDetailed();
                            embed = embed.WithAuthor(user);
                            embed = embed.WithFooter(embed.Footer.Text + $" | Person ID: {user.Id}");
                            await (Channels.GetModerationChannel(user.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                        }
                        userprofile.BehaviourLogs.AddLogEntry(logentry);
                        await RespondAsync($"Deleted all notes for <@{userprofile.UserID}>. ||(Transaction ID: \"Transactions are disabled, due to config flag\")||");
                        await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                            new CommandSuccessLogEntry(Context.User.Id, "delnote", DateTime.UtcNow, Context.Guild as SocketGuild)
                            .WithAdditonalField("Transaction ID", $"Transactions are disabled, due to config flag")
                        );
                    }
                } else {
                    var userprofile = ProfileManager.GetUserProfile(user.Id);
                    var checknote = ModerationFunctions.IsNotePresent(userprofile, entryid.Value);
                    if (!checknote)
                    {

                        await RespondAsync($"No note found with ID {entryid.Value}.", ephemeral: true);
                        await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                            new CommandSuccessLogEntry(Context.User.Id, "delnote", DateTime.UtcNow, Context.Guild as SocketGuild)
                            .WithAdditonalField("Additional remarks: ", $"No note found with ID {entryid.Value}.")
                        );
                        return;
                    }
                    var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorDeleteNoteLogEntry>();
                    if (userprofile.BehaviourLogs.Logs.Count == 0)
                    {
                        logentry.ID = 1;
                    }
                    else
                    {
                        logentry.ID = userprofile.BehaviourLogs.Logs.Select(x => x.ID).Max() + 1;
                    }
                    ModeratorNoteLogEntry note = (ModeratorNoteLogEntry)userprofile.BehaviourLogs.GetByID(entryid.Value);
                    logentry.ModeratorId = Context.User.Id;
                    logentry.NoteID = entryid.Value;
                    logentry.NoteContent = note.Note;
                    logentry.Reason = reason;
                    userprofile.BehaviourLogs.AddLogEntry(logentry);
                    {
                        var embed = logentry.FormatDetailed();
                        embed = embed.WithAuthor(user);
                        embed = embed.WithFooter(embed.Footer.Text + $" | Person ID: {user.Id}");
                        await (Channels.GetModerationChannel(user.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                    }
                    ModerationFunctions.RemoveNote(userprofile, entryid.Value);
                    await RespondAsync($"Deleted note");
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandSuccessLogEntry(Context.User.Id, "delnote", DateTime.UtcNow, Context.Guild as SocketGuild)
                    );
                }
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "note delete", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Parameter 'user'", $"{user.Mention}")
                    .WithAdditonalField("Parameter 'entryid'", $"{(entryid.HasValue ? entryid.Value : "no value")}")
                    .WithAdditonalField("Parameter 'reason'", $"{reason}")
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
                var transdata = (PurgeNoteTransaction)transactions.GetTransactionById(transactionid, true).TransactionData;
                var userprofile = transdata.user;
                var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorPurgeNoteLogEntry>();
                logentry.AmountDeleted = (ulong)userprofile.BehaviourLogs.Logs.Where(x => x is ModeratorNoteLogEntry).Count();
                logentry.TransactionID = transactionid;
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
                logentry.Reason = transdata.reason;
                logentry.ModeratorId = Context.User.Id;
                if (userprofile.BehaviourLogs.Logs.Count == 0)
                {
                    logentry.ID = 1;
                }
                else
                {
                    logentry.ID = userprofile.BehaviourLogs.Logs.Select(x => x.ID).Max() + 1;
                }
                {
                    var user = await Context.Guild.GetUserAsync(userprofile.UserID);
                    var embed = logentry.FormatDetailed();
                    if (user != null) {
                        embed = embed.WithAuthor(user);
                    }
                    embed = embed.WithFooter(embed.Footer.Text + $" | Person ID: {user.Id}");
                    await (Channels.GetModerationChannel((SocketGuild)Context.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                }
                userprofile.BehaviourLogs.AddLogEntry(logentry);
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
                        var embed = logentry.FormatDetailed();
                        embed = embed.WithAuthor(user);
                        embed = embed.WithFooter(embed.Footer.Text + $" | Person ID: {user.Id}");
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
        public async Task Unban(string useridentifier, string reason = "No reason was given.")
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
                transactiondata.reason = reason;
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
                var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorUnbanLogEntry>();
                if (userprofile.BehaviourLogs.Logs.Count == 0)
                {
                    logentry.ID = 1;
                }
                else
                {
                    logentry.ID = userprofile.BehaviourLogs.Logs.Select(x => x.ID).Max() + 1;
                }
                logentry.ModeratorId = Context.User.Id;
                logentry.Reason = ((UnbanTransaction)transdata).reason;
                await Context.Guild.RemoveBanAsync(userprofile.UserID);
                userprofile.IsBanned = false;
                {
                    var embed = logentry.FormatDetailed();
                    embed = embed.WithFooter(embed.Footer.Text + $" | Person ID: {userprofile.UserID}");
                    await (Channels.GetModerationChannel((SocketGuild)Context.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                }
                userprofile.BehaviourLogs.AddLogEntry(logentry);
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

        [SlashCommand("infractions", "View infractions for this user, or one particular one")]
        public async Task ViewInfractions(SocketGuildUser user, int page = 1, ulong? infractionid = null)
        {
            var userprofile = ProfileManager.GetUserProfile(user.Id);
            var embed = new EmbedBuilder().WithAuthor(user);
            var pagestart = ItemsPerPage * (page - 1);
            var pageend = ItemsPerPage * page;
            if (infractionid == null)
            {
                {
                    // Notes
                    var builtstring = "";
                    var listof = from x in userprofile.BehaviourLogs.Logs where x is MajorLog select x;
                    var count = listof.Count();
                    var paginated = PaginateArray(listof.ToArray(), ItemsPerPage, page);
                    var truncatedduetolength = false;
                    foreach (var item in paginated)
                    {
                        var added = builtstring + item.FormatSimple() + "\n";
                        if (added.Length > 1024)
                        {
                            truncatedduetolength = true;
                            break;
                        }
                        builtstring = added;
                    }
                    if (builtstring.Length < 1)
                    {
                        builtstring += "This user has no Infractions.";
                    }
                    embed.AddField($"Infractions (showing from {Math.Max(Math.Min(pageend, count) - ItemsPerPage, 0)}-{Math.Min(pageend, count)}) {(truncatedduetolength ? "(Truncated due to 1024 char limit)" : "")}", builtstring);

                }
            } else
            {
                var logentry = userprofile.BehaviourLogs.GetByID(infractionid.Value);
                if (logentry == null)
                {
                    await RespondAsync($"No such infraction ID: {infractionid.Value}", ephemeral: true);
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandWarningLogEntry(Context.User.Id, "infractions", DateTime.UtcNow, Context.Guild as SocketGuild, $"No such infraction ID: {infractionid}")
                        .WithAdditonalField("User", $"{user.Mention}")
                        .WithAdditonalField("Page", $"{page}")
                        .WithAdditonalField("Infraction ID", $"{infractionid}")
                    );
                    return;
                }
                embed = logentry.FormatDetailed();
                embed = embed.WithAuthor(user);
                embed = embed.WithFooter(embed.Footer.Text + $" | Person ID: {user.Id}");

            }
            await RespondAsync("", embed: embed.Build());
            await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                new CommandSuccessLogEntry(Context.User.Id, "infractions", DateTime.UtcNow, Context.Guild as SocketGuild)
            );
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
                logentry.MessagePruneDays = (ulong)messageprunedays;
                try
                {
                    {
                        var embed = logentry.FormatDetailed();
                        embed = embed.WithAuthor(user);
                        embed = embed.WithFooter(embed.Footer.Text + $" | Person ID: {user.Id}");
                        await (Channels.GetModerationChannel(user.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                    }
                    _ = user.SendMessageAsync($"You have been banned by {Context.User.Mention} for {reason}.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    await RespondAsync("Could not send message to user.", ephemeral: true);
                }
                userprofile.BehaviourLogs.AddLogEntry(logentry);
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
                logentry.MuteTimerID = timer.InstanceUID;
                userprofile.BehaviourLogs.AddLogEntry(logentry);
                GlobalTimerStorage.AddTimer(timer);
                {
                    var embed = logentry.FormatDetailed();
                    embed = embed.WithAuthor(user);
                    embed = embed.WithFooter(embed.Footer.Text + $" | Person ID: {user.Id}");
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
        public async Task Unmute(SocketGuildUser user, string reason = "")
        {
            try {
                var userprofile = ProfileManager.GetUserProfile(user.Id);
                if (!userprofile.IsMuted)
                {
                    await RespondAsync($"{user.Mention} is already unmuted.", ephemeral: true);
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandWarningLogEntry(Context.User.Id, "unmute", DateTime.UtcNow, Context.Guild as SocketGuild, "User is already unmuted.")
                        .WithAdditonalField("User", $"{user.Mention}")
                        .WithAdditonalField("Reason", $"{reason}")
                    );
                    return;
                }
                GlobalTimerStorage.GetTimerByID(userprofile.MutedTimerID).OnTarget();
                var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorUnmuteLogEntry>();
                if (userprofile.BehaviourLogs.Logs.Count == 0)
                {
                    logentry.ID = 1;
                }
                else
                {
                    logentry.ID = userprofile.BehaviourLogs.Logs.Select(x => x.ID).Max() + 1;
                }
                logentry.ModeratorId = Context.User.Id;
                logentry.Reason = reason;
                await RespondAsync($"Unmuted {user.Mention}.", ephemeral: true);
                {
                    var embed = logentry.FormatDetailed();
                    embed = embed.WithAuthor(user);
                    embed = embed.WithFooter(embed.Footer.Text + $" | Person ID: {user.Id}");
                    await (Channels.GetModerationChannel(user.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                }
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "unmute", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            } catch (Exception e) {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "unmute", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("User", $"{user.Mention}")
                    .WithAdditonalField("Reason", reason)
                );
            }
            
        }

        #region Delete Infraction
        [SlashCommand("delinfraction", "Deletes an infraction")]
        public async Task DeleteInfraction(SocketGuildUser user, ulong? entryid = null, string reason = "No reason was given.")
        {

            try
            {
                if (!entryid.HasValue)
                {
                    if (EnableConfirmations)
                    {
                        var userprofile = ProfileManager.GetUserProfile(user.Id);
                        var maxconfirm = DateTime.UtcNow.AddMinutes(1);
                        var transactiondata = new PurgeInfractionTransaction
                        {
                            reason = reason,
                            user = userprofile
                        };
                        var transaction = transactions.StartTransaction(maxconfirm, transactiondata);
                        await RespondAsync($"Please confirm deletion of all infractions for this user. (<t:{Math.Floor(maxconfirm.Subtract(DateTime.UnixEpoch).TotalSeconds)}:R>) ||(Transaction ID: {transaction})||", components:
                        new ComponentBuilder().WithButton("Confirm", $"confirmpurgeinfraction_{transaction}")
                        .WithButton("Cancel", $"cancelpurgeinfraction_{transaction}")
                        .Build()
                        , ephemeral: true);
                        await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                            new CommandSuccessLogEntry(Context.User.Id, "delinfraction", DateTime.UtcNow, Context.Guild as SocketGuild)
                        );
                    }
                    else
                    {
                        var userprofile = ProfileManager.GetUserProfile(user.Id);
                        var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorPurgeInfractionLogEntry>();
                        logentry.AmountDeleted = (ulong)userprofile.BehaviourLogs.Logs.Where(x => x is MajorLog).Count();
                        logentry.TransactionID = "";
                        var checknote = ModerationFunctions.DeleteAllInfractions(userprofile);
                        if (userprofile.BehaviourLogs.Logs.Count == 0)
                        {
                            logentry.ID = 1;
                        }
                        else
                        {
                            logentry.ID = userprofile.BehaviourLogs.Logs.Select(x => x.ID).Max() + 1;
                        }
                        if (!checknote)
                        {

                            await RespondAsync($"No infractions found for this user. ||(Transaction ID: \"Transactions are disabled, due to config flag\")||", ephemeral: true);
                            await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                                new CommandSuccessLogEntry(Context.User.Id, "delinfraction", DateTime.UtcNow, Context.Guild as SocketGuild)
                                .WithAdditonalField("Additional remarks: ", $"No infractions are present for this user.")
                                .WithAdditonalField("Transaction ID", $"Transactions are disabled, due to config flag")
                            );
                            return;
                        }
                        logentry.Reason = reason;
                        logentry.ModeratorId = Context.User.Id;
                        {
                            var embed = logentry.FormatDetailed();
                            embed = embed.WithAuthor(user);
                            embed = embed.WithFooter(embed.Footer.Text + $" | Person ID: {user.Id}");
                            await (Channels.GetModerationChannel(user.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                        }
                        userprofile.BehaviourLogs.AddLogEntry(logentry);
                        await RespondAsync($"Deleted all infractions for <@{userprofile.UserID}>. ||(Transaction ID: \"Transactions are disabled, due to config flag\")||");
                        await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                            new CommandSuccessLogEntry(Context.User.Id, "delinfraction", DateTime.UtcNow, Context.Guild as SocketGuild)
                            .WithAdditonalField("Transaction ID", $"Transactions are disabled, due to config flag")
                        );
                    }
                }
                else
                {
                    var userprofile = ProfileManager.GetUserProfile(user.Id);
                    var checknote = ModerationFunctions.IsInfractionPresent(userprofile, entryid.Value);
                    if (!checknote)
                    {

                        await RespondAsync($"No infraction found with ID {entryid.Value}.", ephemeral: true);
                        await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                            new CommandSuccessLogEntry(Context.User.Id, "delinfraction", DateTime.UtcNow, Context.Guild as SocketGuild)
                            .WithAdditonalField("Additional remarks: ", $"No infraction found with ID {entryid.Value}.")
                        );
                        return;
                    }
                    var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorDeleteInfractionLogEntry>();
                    if (userprofile.BehaviourLogs.Logs.Count == 0)
                    {
                        logentry.ID = 1;
                    }
                    else
                    {
                        logentry.ID = userprofile.BehaviourLogs.Logs.Select(x => x.ID).Max() + 1;
                    }
                    MajorLog infraction = (MajorLog)userprofile.BehaviourLogs.GetByID(entryid.Value);
                    logentry.ModeratorId = Context.User.Id;
                    logentry.InfractionFormatted = infraction.FormatSimple();
                    logentry.Reason = reason;
                    userprofile.BehaviourLogs.AddLogEntry(logentry);
                    {
                        var embed = logentry.FormatDetailed();
                        embed = embed.WithAuthor(user);
                        embed = embed.WithFooter(embed.Footer.Text + $" | Person ID: {user.Id}");
                        await (Channels.GetModerationChannel(user.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                    }
                    ModerationFunctions.RemoveInfraction(userprofile, entryid.Value);
                    await RespondAsync($"Deleted infraction");
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandSuccessLogEntry(Context.User.Id, "delinfraction", DateTime.UtcNow, Context.Guild as SocketGuild)
                    );
                }
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "note delete", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Parameter 'user'", $"{user.Mention}")
                    .WithAdditonalField("Parameter 'entryid'", $"{(entryid.HasValue ? entryid.Value : "no value")}")
                    .WithAdditonalField("Parameter 'reason'", $"{reason}")
                );
            }
        }

        [ComponentInteraction("confirmpurgeinfraction_*", true)]
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
                var transdata = (PurgeInfractionTransaction)transactions.GetTransactionById(transactionid, true).TransactionData;
                var userprofile = transdata.user;
                var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorPurgeInfractionLogEntry>();
                logentry.AmountDeleted = (ulong)userprofile.BehaviourLogs.Logs.Where(x => x is MajorLog).Count();
                logentry.TransactionID = transactionid;
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
                logentry.ModeratorId = Context.User.Id;
                logentry.Reason = transdata.reason;
                if (userprofile.BehaviourLogs.Logs.Count == 0)
                {
                    logentry.ID = 1;
                }
                else
                {
                    logentry.ID = userprofile.BehaviourLogs.Logs.Select(x => x.ID).Max() + 1;
                }
                {
                    var user = await Context.Guild.GetUserAsync(userprofile.UserID);
                    var embed = logentry.FormatDetailed();
                    if (user != null)
                    {
                        embed = embed.WithAuthor(user);
                    }
                    embed = embed.WithFooter(embed.Footer.Text + $" | Person ID: {user.Id}");
                    await (Channels.GetModerationChannel((SocketGuild)Context.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                }
                userprofile.BehaviourLogs.AddLogEntry(logentry);
                await RespondAsync($"Deleted all infractions for <@{userprofile.UserID}>. ||(Transaction ID: {transactionid})||");
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

        [ComponentInteraction("cancelpurgeinfraction_*", true)]
        public async Task CancelButton2(string transactionid)
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
        #endregion

        [AutocompleteCommand("badgename", "addbadge")]
        public async Task Autocomplete()
        {
            var possiblekeys = new List<AutocompleteResult>() {};
            foreach (var item in BadgeRegistry.AllBadges)
            {
                possiblekeys.Add(new AutocompleteResult($"{item.Name}", item.Name));
            }
            string userInput = (Context.Interaction as SocketAutocompleteInteraction).Data.Current.Value.ToString();
            IEnumerable<AutocompleteResult> results = possiblekeys.Where(x => x.Name.StartsWith(userInput, StringComparison.InvariantCultureIgnoreCase));


            // max - 25 suggestions at a time
            await (Context.Interaction as SocketAutocompleteInteraction).RespondAsync(results.Take(25));
        }

        [SlashCommand("addbadge", "Adds a badge for this user")]
        public async Task AddBadge(SocketGuildUser user, [Discord.Interactions.Summary("badgename"), Autocomplete] string badgename)
        {
            try
            {
                var userprofile = ProfileManager.GetUserProfile(user.Id);
                if (!BadgeRegistry.AllBadges.Any(x => x.Name == badgename)) {
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandWarningLogEntry(Context.User.Id, "addbadge", DateTime.UtcNow, Context.Guild as SocketGuild, $"No such badge with name {badgename} was found")
                        .WithAdditonalField("User", $"{user.Mention}")
                        .WithAdditonalField("Badge name", badgename)
                    );
                    await RespondAsync($"No such badge with name {badgename} was found");
                    return;
                }
                userprofile.GrantBadge(BadgeRegistry.GetBadgeFromPredefinedRegistry(badgename));
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "addbadge", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
                await RespondAsync($"{user.Mention} has been given the badge {badgename}.");
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "addbadge", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("User", $"{user.Mention}")
                    .WithAdditonalField("Badge", $"{badgename}")
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