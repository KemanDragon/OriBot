using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord.WebSocket;

namespace OriBot.EventHandlers.Base
{
    public abstract class BaseEventHandler
    {
        public BaseEventHandler()
        { }

        public abstract void RegisterEventHandler(DiscordSocketClient client);
    }
}