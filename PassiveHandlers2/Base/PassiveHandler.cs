using Discord.WebSocket;

namespace OriBot.PassiveHandlers2
{
    public abstract class BasePassiveHandler
    {
        internal Requirements _requirements = new Requirements();

        public Requirements Requirements => _requirements;
        private readonly DiscordSocketClient _client;

        private readonly SocketMessage _message;

        public BasePassiveHandler(DiscordSocketClient client, SocketMessage message)
        {
            _client = client;
            _message = message;
        }
    }
}