using System;
using System.Collections.Generic;
using System.Linq;
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
using OriBot.Utilities;

namespace OriBot.Commands
{
    public enum LogType
    {
        Major,
        Minor
    }

    [Requirements(typeof(ModerationModule))]
    public class ModerationModule : OricordCommand
    {
        public static string NormalRoleName => Config.properties["rolenames"]["normal"];

        public static string MutedRoleName => Config.properties["rolenames"]["muted"];

        public const int ItemsPerPage = 15;

        [SlashCommand("warn", "Warns a user")]
        public async Task Warn(WarnType warntype, SocketGuildUser user, string reason)
        {
            var userprofile = ProfileManager.GetUserProfile(user);
            var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorWarnLogEntry>();
            logentry.ID = (ulong)(userprofile.BehaviourLogs.Logs.Count + 1);
            logentry.WarningType = warntype;
            logentry.Reason = reason;
            logentry.ModeratorId = Context.User.Id;
            userprofile.BehaviourLogs.AddLogEntry(logentry);
            try
            {
                _ = user.SendMessageAsync($"You have been warned by {Context.User.Mention} for {reason}.");
                await RespondAsync($"Warned {user.Mention} for {reason}.", ephemeral: true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await RespondAsync("Could not send message to user.", ephemeral: true);
            }
        }

        [SlashCommand("note", "Makes a private note for a user")]
        public async Task Note(SocketGuildUser user, string note)
        {
            var userprofile = ProfileManager.GetUserProfile(user);
            var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorNoteLogEntry>();
            logentry.ID = (ulong)(userprofile.BehaviourLogs.Logs.Count + 1);
            logentry.Note = note;
            logentry.ModeratorId = Context.User.Id;
            userprofile.BehaviourLogs.AddLogEntry(logentry);
            await RespondAsync($"Made a private note for {user.Mention}, contents: {note}", ephemeral: true);
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
            logentry.ID = (ulong)(userprofile.BehaviourLogs.Logs.Count + 1);
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
        }


        [SlashCommand("logs", "Review a users logs.")]
        public async Task ViewLogs(LogType log, SocketGuildUser user, int page)
        {
            var userprofile = ProfileManager.GetUserProfile(user);
            var embed = new EmbedBuilder().WithAuthor(user);
            var pagestart = ItemsPerPage * (page - 1);
            var pageend = ItemsPerPage * page;
            await DeferAsync();
            if (log == LogType.Major)
            {
                embed.Title = "Major user moderation logs: ";
                {
                    // Notes
                    var builtstring = "";
                    var listof = from x in userprofile.BehaviourLogs.Logs where x is ModeratorNoteLogEntry select x;
                    var count = listof.Count();
                    var paginated = PaginateArray(listof.ToArray(), ItemsPerPage, page);
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
                    embed.AddField($"Notes (showing from {Math.Max(Math.Min(pageend, count) - ItemsPerPage, 0)}-{Math.Min(pageend, count)}) {(truncatedduetolength ? "(Truncated due to 1024 char limit)" : "")}", builtstring);
                }
                {
                    // Warnings
                    var builtstring = "";
                    var listof = from x in userprofile.BehaviourLogs.Logs where x is ModeratorWarnLogEntry select x;
                    var count = listof.Count();
                    var paginated = PaginateArray(listof.ToArray(), ItemsPerPage, page);
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

                    embed.AddField($"Warnings (showing from {Math.Max(Math.Min(pageend, count) - ItemsPerPage, 0)}-{Math.Min(pageend, count)}) {(truncatedduetolength ? "(Truncated due to 1024 char limit)" : "")}", builtstring);
                }
                {
                    // Mutes
                    var builtstring = "";

                    var listof = from x in userprofile.BehaviourLogs.Logs where x is ModeratorMuteLogEntry select x;
                    var count = listof.Count();
                    var paginated = PaginateArray(listof.ToArray(), ItemsPerPage, page);
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
                        builtstring += "This user has no Mutes.";
                    }

                    embed.AddField($"Mutes (showing from {Math.Max(Math.Min(pageend, count) - ItemsPerPage, 0)}-{Math.Min(pageend, count)}) {(truncatedduetolength ? "(Truncated due to 1024 char limit)" : "")}", builtstring);
                }
            }
            else
            {
            }
            await FollowupAsync(embed: embed.Build(), ephemeral: true);
        }

        public override Requirements GetRequirements()
        {
            return new Requirements((context, commandinfo, services) =>
            {
                ulong[] servers = { 1005355539447959552, 988594970778804245, 1131908192004231178, 927439277661515776 };
                return servers.Contains(context.Guild.Id);
            });
        }

        private List<T> PaginateArray<T>(T[] array, int pageSize, int page)
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