using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;
using Oribot.Utilities;


namespace main {
    class Program
    {
        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        //private DiscordSocketClient _client;
        //private Logger logger = new Logger();

        public async Task MainAsync()
        {
            //logger.Log("Some logs");
            //logger.Warn("Some warning");
            //logger.Error("Some error");

            // Creating config
            //var _config = new DiscordSocketConfig {
            //    MessageCacheSize = 100
            //};

            // Creating the client
            //_client = new DiscordSocketClient(_config);

            ////_client.Log += Log;

            //// Start the bot
            //await _client.LoginAsync(TokenType.Bot, "MTExNzg4ODQ4MjU0Nzg2Mzc2Mw.Go5KNk.EQ2_8YMjyWXuWLY4XW6ONfF8VUOZYa9PBcxpJA");//Environment.GetEnvironmentVariable("TOKEN"));
            //await _client.StartAsync();

            //_client.MessageUpdated += MessageUpdated;
            //_client.Ready += BotReady; 
                
            //// Block this task until the program is closed.
            //await Task.Delay(-1);
        }
        
     //   private Task BotReady()
     //   {
     //       logger.Log("Bot is ready");
            
     //       return Task.CompletedTask;
     //   }

     //   private async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
	    //{
		   // // If the message was not in the cache, downloading it will result in getting a copy of `after`.
		   // var message = await before.GetOrDownloadAsync();
		   // Console.WriteLine($"{message} -> {after}");
	    //}

    }
}   
