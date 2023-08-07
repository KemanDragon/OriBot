using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord.WebSocket;

using OriBot.EventHandlers.Base;

namespace OriBot.EventHandlers
{
    public class UnregisterCommandsHandler : BaseEventHandler
    {
        public DiscordSocketClient Client { get; private set; }

        public override void RegisterEventHandler(DiscordSocketClient client)
        {
            client.Ready += Client_Ready;
            Client = client;
        }

        private async Task Client_Ready()
        {
            if (File.Exists("reset.txt"))
            {
                var commands = (await Client.GetGlobalApplicationCommandsAsync()).ToList();
                for (int i = 0; i < commands.Count; )
                {
                    try
                    {
                        await commands[i].DeleteAsync();
                        i++;
                    }
                    catch (Exception e)
                    {
                        continue;
                    }
                }
                File.Delete("reset.txt");
            }
        }
    }
}