using Discord.WebSocket;
using Oribot.Utilities;
using OriBot.Framework;

namespace OriBot.PassiveHandlers
{
    public class TestPassiveHandler : OricordPassiveHandler
    {
        public TestPassiveHandler(DiscordSocketClient client, SocketMessage message, EventType type) : base(client, message, type)
        {
        }

        [PassiveHandler]
        public void Test()
        {
            Logger.Info("YES");
        }
    }
}