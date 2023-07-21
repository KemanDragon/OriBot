using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace OriBot.Commands
{
    public class CommandSystem
    {
        private readonly CommandService _commands = new();
        private readonly DiscordSocketClient _client;

        public CommandSystem(DiscordSocketClient client, CommandService commands)
        {
            _commands = commands;
            _client = client;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                            services: null);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // TODO:
            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!message.HasStringPrefix(">> ", ref argPos) || message.Author.IsBot || message.IsTTS) {
                return;
            }
                

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);
            await _commands.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: null);

        }
    }
}
