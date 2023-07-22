using Discord.WebSocket;
using OriBot.Framework;

namespace OriBot.PassiveHandlers2
{
    public abstract class OricordPassiveHandler : BasePassiveHandler
    {
        public OricordPassiveHandler(DiscordSocketClient client, SocketMessage message) : base(client, message)
        {
            _requirements = new Requirements(
                (client, messageParam) =>
                {
                    
                    var message = messageParam as SocketUserMessage;
                    if (message == null) return false;
                    var user = message.Author as SocketGuildUser;
                    if (user == null) return false;
                    if (user.Guild.Id != 1005355539447959552) return false;
                    
                    return true;
                }
            );
        }

        public OricordContext DataContext
        {
            get
            {
                return (OricordContext)main.Memory.ContextStorage["oricord"];
            }
        }
    }
}