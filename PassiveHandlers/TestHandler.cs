using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord.WebSocket;

namespace OriBot.PassiveHandlers
{
    public class TestHandler : PassiveHandler
    {
        public TestHandler(DiscordSocketClient client, SocketUserMessage message) : base(client, message)
        {
        }

        public override async Task Run()
        {
           // await _message.Channel.SendMessageAsync("Test");
        }
    }
}
