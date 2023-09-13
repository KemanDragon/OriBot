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
using OriBot.Transactions;
using OriBot.Utilities;

namespace OriBot.Commands {
    [Requirements(typeof(ModerationCommands2))]
    public class ModerationCommands2 : OricordCommand {

        [SlashCommand("verify", "Verifies a user (changes permission from NewUser to Member)")]
        public async Task VerifyUser(SocketGuildUser user)
        {
            try
            {
                var userprofile = ProfileManager.GetUserProfile(user.Id);
                if (userprofile.GetPermissionLevel(Context.Guild.Id) >= PermissionLevel.Member)
                {

                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandWarningLogEntry(Context.User.Id, "verify", DateTime.UtcNow, Context.Guild as SocketGuild, $"User is already Member or higher level.")
                        .WithAdditonalField("User", $"{user.Mention}")
                    );
                    await RespondAsync($"User is already Member or higher level.", ephemeral: true);
                    return;
                }
                var logentry = UserBehaviourLogRegistry.CreateLogEntry<UserVerifiedLogEntry>();
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
                    var embed = logentry.FormatDetailed();
                    embed = embed.WithAuthor(user);
                    embed = embed.WithFooter(embed.Footer.Text + $" | Person ID: {user.Id}");
                    await (Channels.GetModerationChannel(user.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                }
                userprofile.SetPermissionLevel(PermissionLevel.Member, Context.Guild.Id);
                await RespondAsync($"User elevated to Member.", ephemeral: true);
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "verify", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "verify", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("User", $"{user.Mention}")
                );
            }
        }

        public override Requirements GetRequirements()
        {
            return ModerationConstants.ModeratorRequirements;
        }
    }
}