using Discord.WebSocket;
using OriBot.Framework;

namespace OriBot.PassiveHandlers
{
    public class TestPassiveHandler : OricordPassiveHandler
    {
        public TestPassiveHandler(DiscordSocketClient client, SocketMessage message) : base(client, message)
        {
        }

        [PassiveHandler]
        public void Test() {
            Logging.Debug("YES");
        }
    }
}