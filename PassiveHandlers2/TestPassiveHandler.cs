using Discord.WebSocket;
using OriBot.Framework;

namespace OriBot.PassiveHandlers2
{
    public class TestPassiveHandler : OricordPassiveHandler
    {
        public TestPassiveHandler(DiscordSocketClient client, SocketMessage message) : base(client, message)
        {
        }

        [PassiveHandler]
        public void Test() {
            Logging.Debug("YES",Origin.MAIN);
        }
    }
}