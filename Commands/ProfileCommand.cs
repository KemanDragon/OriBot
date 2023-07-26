using System.Threading.Tasks;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
// using OriBot.Framework;
using OriBot.Framework.UserProfiles;

namespace OriBot.Commands
{
    public class ProfileModule : OricordCommand
    {
        [SlashCommand("profile", "Gets your current user profile")]
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
    }
}