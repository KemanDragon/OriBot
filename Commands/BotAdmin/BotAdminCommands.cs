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
using OriBot.Framework.UserProfiles.PerGuildData;
using OriBot.Framework.UserProfiles.SaveableTimer;
using OriBot.GuildData;
using OriBot.Transactions;
using OriBot.Utilities;

namespace OriBot.Commands
{

    
    [Requirements(typeof(BotAdminCommands))]
    public class BotAdminCommands : OricordCommand
    {

        public enum ChannelType {
            ModerationLogging,
            BotEventLogging,
            Ticket,
            VoiceLogging,
            MessageLogging
        }
        [SlashCommand("promote", "Promotes a user to a higher or lower permission level")]
        public async Task PromoteUser(SocketGuildUser user, PermissionLevel level)
        {
            try
            {
                var targetprofile = ProfileManager.GetUserProfile(user.Id);
                var myprofile = ProfileManager.GetUserProfile(Context.User.Id);
                if (targetprofile.UserID == myprofile.UserID) 
                {
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandWarningLogEntry(Context.User.Id, "promote", DateTime.UtcNow, Context.Guild as SocketGuild, $"You cannot change your own permission level.")
                        .WithAdditonalField("User", $"{user.Mention}")
                        .WithAdditonalField("Permission Level", $"{level}")
                    );
                    await RespondAsync($"You cannot change your own permission level", ephemeral: true);
                    return;
                }
                if (targetprofile.GetPermissionLevel(Context.Guild.Id) >= myprofile.GetPermissionLevel(Context.Guild.Id))
                {
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandWarningLogEntry(Context.User.Id, "promote", DateTime.UtcNow, Context.Guild as SocketGuild, $"User is at a higher level or the same level as you. You cannot manage their permission level.")
                        .WithAdditonalField("User", $"{user.Mention}")
                        .WithAdditonalField("Permission Level", $"{level}")
                    );
                    await RespondAsync($"User is at a higher level or the same level as you. You cannot manage their permission level.", ephemeral: true);
                    return;
                }
                if (level >= myprofile.GetPermissionLevel(Context.User.Id)) 
                {
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandWarningLogEntry(Context.User.Id, "promote", DateTime.UtcNow, Context.Guild as SocketGuild, $"You cannot elevate this user to a higher level than your level.")
                        .WithAdditonalField("User", $"{user.Mention}")
                        .WithAdditonalField("Permission Level", $"{level}")
                    );
                    await RespondAsync($"You cannot elevate this user to a higher level than yours.", ephemeral: true);
                }
                var logentry = UserBehaviourLogRegistry.CreateLogEntry<UserPromotedLogEntry>();
                logentry.ModeratorId = Context.User.Id;
                logentry.AfterLevel = level;
                logentry.PreviousLevel = targetprofile.GetPermissionLevel(Context.Guild.Id);
                if (targetprofile.BehaviourLogs.Logs.Count == 0)
                {
                    logentry.ID = 1;
                }
                else
                {
                    logentry.ID = targetprofile.BehaviourLogs.Logs.Select(x => x.ID).Max() + 1;
                }
                {
                    var embed = logentry.FormatDetailed();
                    embed = embed.WithAuthor(user);
                    embed = embed.WithFooter(embed.Footer.Text + $" | Person ID: {user.Id}");
                    await (Channels.GetModerationChannel(user.Guild) as SocketTextChannel).SendMessageAsync("", embed: embed.Build());
                }
                targetprofile.SetPermissionLevel(level, Context.Guild.Id);
                await RespondAsync($"User permission level changed to {level}.", ephemeral: true);
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "promote", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            }
            catch (Exception e)
            {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "promote", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("User", $"{user.Mention}")
                    .WithAdditonalField("Permission Level", $"{level}")
                );
            }
        }

        [SlashCommand("setchannel", "Configures what channel the bot will use for various things")]
        public async Task SetChannel(SocketTextChannel channel, ChannelType channelType) {
            try {
                switch (channelType)
                {
                    case ChannelType.ModerationLogging:
                        GlobalGuildData.SetValue(Context.Guild.Id, "moderationlogs", channel.Id);
                        break;
                    case ChannelType.BotEventLogging:
                        GlobalGuildData.SetValue(Context.Guild.Id, "boteventlogs", channel.Id);
                        break;
                    case ChannelType.Ticket:
                        GlobalGuildData.SetValue(Context.Guild.Id, "tickets", channel.Id);
                        break;
                    case ChannelType.VoiceLogging:
                        GlobalGuildData.SetValue(Context.Guild.Id, "voiceactivity", channel.Id);
                        break;
                    case ChannelType.MessageLogging:
                        GlobalGuildData.SetValue(Context.Guild.Id, "messageactivity", channel.Id);
                        break;
                    default:
                        break;
                }
                await RespondAsync($"Successfully set channel https://discord.com/channels/{Context.Guild.Id}/{channel.Id} for the purpose of {channelType}", ephemeral: true);
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandSuccessLogEntry(Context.User.Id, "setchannel", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            } catch (Exception e) {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "setchannel", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Channel ID", $"{channel.Id}")
                    .WithAdditonalField("Channel Type", $"{channelType}")
                );
            }
        }

        public override Requirements GetRequirements()
        {
            var tmp = ModerationConstants.ModeratorRequirements;
            tmp.AddRequirement(
                (context, _, _) => {
                    var userprofile = ProfileManager.GetUserProfile(context.User.Id);
                    if (userprofile.GetPermissionLevel(context.Guild.Id) < PermissionLevel.BotAdmin)
                    {
                        _ = context.Interaction.RespondAsync("You must be a BotAdmin or higher to execute this command.", ephemeral: true);
                        return false;
                    }
                    return true;
                }
            );
            return tmp;
        }
    }
}