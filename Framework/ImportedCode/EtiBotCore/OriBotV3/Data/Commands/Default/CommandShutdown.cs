using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiLogger.Logging;
using OldOriBot.Interaction;
using OldOriBot.PermissionData;
using OldOriBot.UserProfiles;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Music;

namespace OldOriBot.Data.Commands.Default {
	public class CommandShutdown : Command {
		public override string Name { get; } = "shutdown";
		public override string Description { get; } = "Disconnects the bot from Discord's gateway and exits the app (unless told not to).";
		public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<bool>("keepOpen").SetRequiredState(false);
		public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;

		public CommandShutdown() : base(null) { }

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			if (!isConsole) await originalMessage.ReplyAsync(Personality.Get("cmd.shutdown.success"));
			CommandLogger.WriteLine("Shutting down.");
			ArgumentMap<bool> args = Syntax.Parse<bool>(argArray.ElementAtOrDefault(0));
			await MusicController.StopAll();
			await DiscordClient.Current.DisconnectAsync();
			CommandLogger.WriteLine("§aSaving all user profiles (and disabling verbose logging because that'll lag)...", LogLevel.Info);
			Logger.LoggingLevel = LogLevel.Info;
			UserProfile.SaveAll();
			if (!args.Arg1) {
				Environment.Exit(0);
			} else {
				CommandLogger.WriteLine("§aDone.", LogLevel.Info);
			}
		}
	}
}
