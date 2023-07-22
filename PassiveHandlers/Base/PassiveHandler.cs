using Discord.WebSocket;

namespace OriBot.PassiveHandlers
{
    public abstract class BasePassiveHandler
    {
        internal Requirements _requirements = new Requirements();

        public Requirements Requirements => _requirements;
        internal readonly DiscordSocketClient client;

        internal readonly SocketMessage message;

        

        public BasePassiveHandler(DiscordSocketClient client, SocketMessage message)
        {
            this.client = client;
            this.message = message;
        }
    }
}