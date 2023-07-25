using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Container;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Extension;
using EtiLogger.Data.Structs;
using EtiLogger.Logging;
using Test_Implementation.OutputArea;

namespace Test_Implementation {
	class Program {

		#region Tha Token
		public static readonly string BOT_TOKEN = File.ReadAllText(@".\token.txt");
		#endregion

		static void Main() {
			//Logger.DefaultTarget = new ServerRelay();
			Logger.LoggingLevel = LogLevel.Trace;
			Logger.Default.Target = Logger.DefaultTarget;


			//Snowflake me = 326467566572797952;
			//DateTimeOffset time = me.GetTimestamp();
			//Logger.Default.WriteLine($"Created on {time.Day} {CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(time.Month)} {time.Year} at {time.Hour:D2}:{time.Minute:D2}:{time.Second:D2} + {time.Millisecond:D4}ms");
			MainAsync().Wait();
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("Press any key to quit.");
			Console.ReadKey();
		}

		public static async Task MainAsync() {
			DiscordClient sys = null;
			try {
				GatewayIntent intents = GatewayIntent.DIRECT_MESSAGES |
					GatewayIntent.GUILDS |
					GatewayIntent.GUILD_BANS |
					GatewayIntent.GUILD_MEMBERS |
					GatewayIntent.GUILD_MESSAGES |
					GatewayIntent.GUILD_MESSAGE_REACTIONS |
					GatewayIntent.GUILD_VOICE_STATES;

				DiscordClient.Log.Target = Logger.DefaultTarget;
				DiscordClient.LoggingLevel = LogLevel.Trace;
				sys = new DiscordClient(BOT_TOKEN, intents);

				await sys.ConnectAsync();

				Console.ReadKey();
				await sys.DisconnectAsync();
			} catch (Exception genericExc) {
				await sys?.DisconnectAsync();
				Logger.Default.WriteException(genericExc);
			}
		}
	}
}
