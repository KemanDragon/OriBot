using System;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using OriBot.Commands.RequirementEngine;
// using OriBot.Framework;
using OriBot.Framework.UserProfiles;

namespace OriBot.Commands
{
    public class AdminModule : OricordCommand
    {
        [SlashCommand("admintest", "Gets your current user profile")]
        public async Task Profile()
        {
            var tmp = UserProfile.GetOrCreateUserProfile(this.Context.User as SocketGuildUser);

            var embed = new EmbedBuilder
            {
                Title = "Title",
                Description = "Desc"
            };

            embed.AddField($"Profile id: <@{Context.User.Id}>", $"Badge count: {tmp.Badges.Count}\n Permission Level: {tmp.GetPermissionLevel(this.Context.Guild.Id)}")
                .WithAuthor(this.Context.User)
                .WithFooter(footer => footer.Text = $"Oribot v{Constants.OriBotVersion}")
                .WithColor(Color.Default)
                .WithDescription("User Profile")
                .WithCurrentTimestamp();

            // await this.RespondAsync(UseEmbed.);
            await this.RespondAsync(embed: embed.Build());
            // await this.RespondAsync(
            //     $"Profile id: <@{Context.User.Id}\n>" +
            //          $"Badge count: {tmp.Badges.Count}\n" +
            //          $"Permission Level: {tmp.GetPermissionLevel(this.Context.Guild.Id)}",ephemeral: true
            //     );
        }

        public override Requirements GetRequirements()
        {
            return new Requirements((context, commandinfo, services) =>
            {
                ulong[] servers = { 1005355539447959552, 988594970778804245, 1131908192004231178, 927439277661515776 };
                return servers.Contains(context.Guild.Id);
            }, (context, commandinfo, services) =>
            {
                if (ProfileManager.GetUserProfile(context.User as SocketUser).GetPermissionLevel(context.Guild.Id) >= PermissionLevel.Moderator)
                {
                    return true;
                }
                return false;
            });
        }
    }
}