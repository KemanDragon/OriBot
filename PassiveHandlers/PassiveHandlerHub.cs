using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Discord.WebSocket;

namespace OriBot.PassiveHandlers
{
    public class PassiveHandlerHub
    {
        private readonly DiscordSocketClient _client;
        private readonly List<Type> _passiveHandlers = new();
        public PassiveHandlerHub(DiscordSocketClient client)
        {
            _client = client;
        }

        public void RegisterPassiveHandlers()
        {
            Assembly.GetExecutingAssembly().GetTypes().ToList().ForEach(type =>
            {
                if (type.BaseType == typeof(PassiveHandler))
                {
                    _passiveHandlers.Add(type);
                }
            });
            _client.MessageReceived += HandleMessageAsync;
        }

        private async Task HandleMessageAsync(SocketMessage messageParam)
        {
            
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            
            // Create a number to track where the prefix ends and the command begins
            foreach (var item in _passiveHandlers)
            {
                Activator.CreateInstance(item, _client, message);
            }   
        }
    }
}
