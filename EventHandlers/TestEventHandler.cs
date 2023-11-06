using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord.WebSocket;

using OriBot.EventHandlers.Base;

namespace OriBot.EventHandlers
{
    public class TestEventHandler : BaseEventHandler
    {
        public override void RegisterEventHandler(DiscordSocketClient client)
        {
            client.ReactionAdded += Client_ReactionAdded;
        }

        private async Task Client_ReactionAdded(Discord.Cacheable<Discord.IUserMessage, ulong> arg1, Discord.Cacheable<Discord.IMessageChannel, ulong> arg2, SocketReaction arg3)
        {
            //  await arg2.Value.SendMessageAsync("12313");
        }
    }
}