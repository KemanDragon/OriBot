using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord.WebSocket;

namespace OriBot.PassiveHandlers
{
    public abstract class PassiveHandler
    {
        public readonly DiscordSocketClient _client;
        public readonly SocketUserMessage _message;
        
        /// <summary>
        /// Passive handlers work in a pretty similar way to how commands work in the discord.net framework. 
        /// In that anytime that a passive handler is run, The constructor for the passive handler class is instantiated. 
        /// After the constructor is run. The Run method of the passive handler is called. Which is what defines what the passive handler does. 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        public PassiveHandler(DiscordSocketClient client, SocketUserMessage message)
        {
            _client = client;
            _message = message;
            Run();
        }

        public abstract Task Run();
    }
}
