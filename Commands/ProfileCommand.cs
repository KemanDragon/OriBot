using System.Threading.Tasks;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using OriBot.Framework;
using OriBot.Framework.UserProfiles;

namespace OriBot.Commands
{
    public class ProfileModule : OricordCommand
    {
        [SlashCommand("profile", "Gets your current user profile")]
        public async Task Profile()
        {
            
            var tmp = UserProfile.GetOrCreateUserProfile(this.Context.User as SocketGuildUser);
            await RespondAsync(tmp.SerializedBadges);
        }
    }
}