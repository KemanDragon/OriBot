using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using OriBot.Commands;
using OriBot.Framework.UserProfiles;
using OriBot.GuildData;
using OriBot.Utilities;

namespace OriBot.Commands
{
    [Requirements(typeof(UserSupportCommands))]
    public class UserSupportCommands : OricordCommand {

        public static SocketGuildChannel GetTicketsChannel(SocketGuild guild)
        {
            if (!GlobalGuildData.GetPerGuildData(guild.Id).ContainsKey("tickets"))
            {
                return guild.Channels.Where(x => x.Name == Config.properties["channels"]["tickets"].ToObject<string>()).FirstOrDefault() as SocketGuildChannel;
            }
            return guild.Channels.FirstOrDefault(x => x.Id == GlobalGuildData.GetValueFromData<ulong>(guild.Id, "tickets"));
        }

        public static SocketRole GetModsRole(SocketGuild guild)
        {
            return guild.Roles.Where(x => x.Name == Config.properties["rolenames"]["moderators"].ToObject<string>()).FirstOrDefault();
        }
        
        [SlashCommand("ticket","Opens a ticket that the moderators can address")]
        public async Task TicketCommand(string reason) {
            var userprofile = ProfileManager.GetUserProfile(Context.User.Id);
            if (userprofile.TicketManager.CanOpenTicket((SocketGuild)Context.Guild)) {
                await DeferAsync(ephemeral: true);
                SocketTextChannel ticketchannel = (SocketTextChannel)GetTicketsChannel((SocketGuild)Context.Guild);
                var thread = await ticketchannel.CreateThreadAsync(
                    $"Ticket {((SocketGuildUser)Context.User).DisplayName} #{new Random().NextInt64(1111,9999)}",
                    type: Discord.ThreadType.PrivateThread,
                    autoArchiveDuration: Discord.ThreadArchiveDuration.OneWeek
                );

                userprofile.TicketManager.SetTicketChannel(Context.Guild.Id, thread.Id);
                var modrole = GetModsRole((SocketGuild)Context.Guild);
                await thread.SendMessageAsync($"{Context.User.Mention}");
                await thread.SendMessageAsync($"{modrole.Mention}");
                await FollowupAsync("A ticket was opened.", ephemeral: true);
            } else {
                await DeferAsync(ephemeral: true);
                var latest = userprofile.TicketManager.GetLatestOpenTicket(Context.Guild.Id);
                await FollowupAsync(
                    $"Sorry, please resolve your current ticket first before opening another one. (\"https://discord.com/channels/{Context.Guild.Id}/{latest.Id}\")"
                    ,ephemeral: true
                );
                
            }

        }
    }
}