using Discord.WebSocket;
using OriBot.Utilities;
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
            // FIXME: implement a debug channel in Logging util
            // Logger.Info("YES");
        }
    }
}