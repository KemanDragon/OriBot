using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using OriBot.Commands.RequirementEngine;
using OriBot.Framework;
using OriBot.Framework.UserBehaviour;
using OriBot.Framework.UserProfiles;

namespace OriBot.Commands
{
    [Requirements(typeof(ModerationModule))]
    public class ModerationModule : OricordCommand
    {
        [SlashCommand("warn", "Warns a user")]
        public async Task Warn(WarnType warntype, SocketGuildUser user, string reason)
        {
            var userprofile = ProfileManager.GetUserProfile(user);
            var logentry = UserBehaviourLogRegistry.CreateLogEntry<ModeratorWarnLogEntry>();
            logentry.WarningType = warntype;
            logentry.Reason = reason;
            logentry.ModeratorId = Context.User.Id;
            try {
                await user.SendMessageAsync($"You have been warned by {Context.User.Mention} for {reason}.");
                await RespondAsync("User warned successfully.", ephemeral: true);
            } catch (Exception e)
            {
                Console.WriteLine(e);
                await Context.Interaction.RespondAsync("Could not send message to user.");
            }
            await ReplyAsync($"Warned {user.Mention} for {reason}.");
            userprofile.BehaviourLogs.AddLogEntry(logentry);
        }

        [SlashCommand("review","Review a users logs.")]
        public async Task Review(SocketGuildUser user)
        {
            var userprofile = ProfileManager.GetUserProfile(user);
            var builtstring = "";
            foreach (var item in userprofile.BehaviourLogs.Logs)
            {
                if (item is MajorLog)
                {
                    builtstring += $"{item.Format()}\n ";
                }

            }
            var embed = new EmbedBuilder
            {
                Title = "Minor warnings: ",
                Description = builtstring,
            };
            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }





        public override Requirements GetRequirements()
        {
            return new Requirements((context, commandinfo, services) =>
            {
                ulong[] servers = { 1005355539447959552, 988594970778804245, 1131908192004231178, 927439277661515776 };
                return servers.Contains(context.Guild.Id);
            });
        }
    }
}