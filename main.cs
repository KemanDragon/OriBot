using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Commands.Builders;
using System;
using System.IO;
using System.Threading.Tasks;
using OriBot.Commands;
using OriBot.PassiveHandlers;
using OriBot.Storage2;
using System.Collections.Generic;
using OriBot.Framework;

namespace main
{
    public class Memory
    {
        public static Dictionary<ulong,GeneralServerContext> contextStorage = new();
    }


    internal class Program
    {
        public static Task Main(string[] args) => new Program().MainAsync();

        private DiscordSocketClient _client;

        private CommandSystem _commandhub;

        private PassiveHandlerHub _passiveHandlerHub;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _client.Log += Log;

            //  You can assign your bot token to a string, and pass that in to connect.
            //  This is, however, insecure, particularly if you plan to have your code hosted in a public repository.
            var token = File.ReadAllText("token.txt");

            // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
            // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
            // var token = File.ReadAllText("token.txt");
            // var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;
            Console.WriteLine(JObject.Load(File.ReadAllText("test.json")).ToString());
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            await InitializeOtherSystems();
            main.Memory.contextStorage.Add(1005355539447959552, new OricordContext());
            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        public async Task InitializeOtherSystems()
        {
            _commandhub = new OriBot.Commands.CommandSystem(_client, new CommandService());
            await _commandhub.RegisterCommandsAsync();
            _passiveHandlerHub = new PassiveHandlerHub(_client);
            _passiveHandlerHub.RegisterPassiveHandlers();
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());

            return Task.CompletedTask;
        }
    }
}