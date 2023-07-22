using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord.WebSocket;




using OriBot.PassiveHandlers;

namespace OriBot.PassiveHandlers
{
    public class PingHandler : PassiveHandler
    {
        public PingHandler(DiscordSocketClient client, SocketUserMessage message) : base(client, message)
        {
        }
        public override async Task Run()
        {

            if (_message.Content == "@ping")
            {
                
                
               // await _message.Channel.SendMessageAsync("Pong!, milliseconds: " + TestCommandModule.pingtime.ElapsedMilliseconds);
              //  await _message.DeleteAsync();
            }
        }
    }
}
