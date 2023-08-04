using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.Interaction;
using OldOriBot.PermissionData;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Responding;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OldOriBot.CoreImplementation.Commands {
	public class CommandTestThreads : Command {
		public override string Name { get; } = "threadtest";
		public override string Description { get; } = "Test the bot's interaction with threads.";
		public override ArgumentMapProvider Syntax { get; }
		public override PermissionLevel RequiredPermissionLevel => PermissionLevel.BotDeveloper;
		public CommandTestThreads(BotContext ctx) : base(ctx) { }

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			TextChannel channel = (TextChannel)originalMessage.Channel;
			Thread thread = await channel.CreateNewThread("Thread Test Invocation", ThreadArchiveDuration.Minutes60, true, "Testing thread interactions.");
			await thread.SendMessageAsync("Hello, world!");
			Member testDummy2 = await executionContext.Server.GetMemberAsync(114163433980559366);
			await thread.TryAddMemberToThread(testDummy2);

			await Task.Delay(2000);
			await thread.SendMessageAsync("This thread will self destruct in 5 seconds. I lied, discord doesn't let bots do that.");
			await Task.Delay(5000);
			await thread.DeleteAsync();
			await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "Debug: Thread deleted? " + thread.Deleted, null, AllowedMentions.Reply);
		}
	}
}
