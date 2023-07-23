using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using OriBot.Commands;
using OriBot.Framework;
using Oribot.Utilities;

using OriBot.PassiveHandlers;
using OriBot.Storage2;

namespace main
{
    public static class Memory
    {
        public static Dictionary<string, Context> ContextStorage = new Dictionary<string, Context>();
    }

    internal class Program
    {
        public static Task Main(string[] args) => new Program().MainAsync();

        private DiscordSocketClient _client;

       // private PassiveHandlerHub _passiveHandlerHub;

        public async Task MainAsync()
        {
            using var ct = new CancellationTokenSource();
            var task = Login(ct.Token);
            var inputTask = ReadConsoleInputAsync(ct.Token);
            await Task.WhenAny(task, inputTask);

            ct.Cancel();
            await inputTask.ContinueWith(_ => { });
            await task;
        }

        private async Task ReadConsoleInputAsync(CancellationToken cancellationToken)
        {
            // may wanna fix this
            var exit = "exit";
            var help = "help";
            var sel = 0;

            while (!cancellationToken.IsCancellationRequested)
            {

                // Asynchronously read the next line from the console
                var input = await Task.Run(Console.ReadLine);

                if (input.ToLower() == exit)
                {
                    sel = 1;
                }

                if (input.ToLower() == help)
                {
                    sel = 2;
                }


                switch (sel)
                {
                    case 1:
                        Logger.Log("Be gone");
                        sel = 0;
                        await Cleanup();
                        break;
                    case 2:
                        Logger.Log("define help here please lol");
                        sel = 0;
                        break;
                    default:
                        Logger.Log("'" + input + "' is not reconized as an internal command. Try 'help' for more information.");
                        sel = 0;
                        break;
                }
            }
        }

        public async Task Login(CancellationToken ct)
        {
            try
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

                // FIXME: perhaps.. remove this? xd
                Logger.Log("##### Login Successful! #####");

                // Block this task until the program is closed.
                await Task.Delay(-1);
            }
            catch (System.Exception e)
            {
                Logger.Error("Task Terminated");
                Logger.Error(e.ToString());
            }
        }

        private void RegisterSlashCommands()
        {
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

        private void AddAllContexts()
        {
            Memory.ContextStorage.Add("oricord", new OricordContext());
        }

        private async Task Cleanup()
        {
            //Logging.Cleanup();
            await Task.CompletedTask;
        }

        private Task Log(LogMessage msg)
        {
            Logger.Log(msg.ToString());
            return Task.CompletedTask;
        }
    }
}