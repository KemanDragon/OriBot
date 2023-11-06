using System;
using Discord.WebSocket;
using OriBot.Framework;
using OriBot.PassiveHandlers.RequirementEngine;
using OriBot.Utilities;

namespace OriBot.PassiveHandlers
{
    public class TestPassiveHandler : OricordPassiveHandler
    {
        public TestPassiveHandler(DiscordSocketClient client, SocketMessage message) : base(client, message)
        {
        }

        [PassiveHandler]
        public void Test() { }
    }
}