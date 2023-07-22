using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Discord;

using Discord.Interactions;
using Discord.WebSocket;
using OriBot.Commands2;
using OriBot.Framework;

using OriBot.PassiveHandlers2;
using OriBot.Storage2;

namespace main
{
    public static class Memory {
        public static Dictionary<string, Context> ContextStorage = new Dictionary<string, Context>();
    }
    internal class Program
    {
        public static Task Main(string[] args) => new Program().MainAsync();

        private DiscordSocketClient _client;

       // private PassiveHandlerHub _passiveHandlerHub;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _client.Log += Log;
            AddAllContexts();
            RegisterSlashCommands();
            PassiveHandlerHub.RegisterPassiveHandlers(_client);

            //  You can assign your bot token to a string, and pass that in to connect.
            //  This is, however, insecure, particularly if you plan to have your code hosted in a public repository.
            var token = File.ReadAllText("token.txt");

            // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
            // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
            // var token = File.ReadAllText("token.txt");
            // var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;
            // // Console.WriteLine(JObject.Load(File.ReadAllText("test.json")).ToString());
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private void RegisterSlashCommands() {
            _client.Ready += async () =>
            {
                var _interactionService = new InteractionService(_client.Rest);
                await _interactionService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
                                                services: null);

                await _interactionService.RegisterCommandsGloballyAsync(false);
                
                _client.InteractionCreated += async (x) =>
                {
                    var ctx = new SocketInteractionContext(_client, x);
                    await _interactionService.ExecuteCommandAsync(ctx, null);
                };
            };
        }

        private void AddAllContexts() {
            Memory.ContextStorage.Add("oricord", new OricordContext());
        }



        private Task Log(LogMessage msg)
        {
            Logging.Info(msg.ToString(), Origin.MAIN);
            return Task.CompletedTask;
        }
    }
}