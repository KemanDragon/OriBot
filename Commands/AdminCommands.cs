using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using OriBot.Commands.RequirementEngine;

// using OriBot.Framework;
using OriBot.Framework.UserProfiles;
using OriBot.Utilities;

namespace OriBot.Commands
{
    public class AdminModule : OricordCommand
    {
        [SlashCommand("reset", "Unregisters all slash commands at next restart.")]
        public async Task ResetCommand()
        {
            File.CreateText("reset.txt").Close();
            Logger.Info("Commands Reset Triggered, type 'exit' to confirm. (there's no going back)");
            await RespondAsync("Reset triggered, restart the bot from CLI or IDE to unregister all slash commands.");
        }

        public override Requirements GetRequirements()
        {
            return new Requirements((context, commandinfo, services) =>
            {
                ulong[] servers = { 1005355539447959552, 988594970778804245, 1131908192004231178, 927439277661515776 };
                return servers.Contains(context.Guild.Id);
            }, (context, commandinfo, services) =>
            {
                if (ProfileManager.GetUserProfile(context.User.Id).GetPermissionLevel(context.Guild.Id) >= PermissionLevel.Moderator)
                {
                    return true;
                }
                return false;
            });
        }
    }
}