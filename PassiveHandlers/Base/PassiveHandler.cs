using Discord.WebSocket;

namespace OriBot.PassiveHandlers
{
    public abstract class BasePassiveHandler
    {
        internal Requirements _requirements = new Requirements();

        public Requirements Requirements => _requirements;
        internal readonly DiscordSocketClient client;

        internal readonly SocketMessage message;

        internal readonly EventType type;

        public BasePassiveHandler(DiscordSocketClient client, SocketMessage message, EventType type)
        {
            this.client = client;
            this.message = message;
            this.type = type;
        }
    }

    public enum EventType
    {
        MessageSent,
        ReactionAdded,
    }
}