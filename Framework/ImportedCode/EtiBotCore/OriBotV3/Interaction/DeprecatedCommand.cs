using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Responding;

namespace OldOriBot.Interaction {

	/// <summary>
	/// Represents a command that is obsolete and has been replaced by another command.
	/// </summary>
	public abstract class DeprecatedCommand : Command {

		public DeprecatedCommand(Command target) : base(target.Context) {
			Target = target;
		}

		/// <summary>
		/// The command that should be used instead. This can point to a subcommand of a command too.
		/// </summary>
		public Command Target { get; }

		public override string Description => Target.Description;

		public override ArgumentMapProvider Syntax => Target.Syntax;

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"{EmojiLookup.GetEmoji("warning")} This command is **obsolete**! You should use `>> {Target.FullName}` instead.", null, AllowedMentions.Reply);
			await Target.ExecuteCommandAsync(executor, executionContext, originalMessage, argArray, rawArgs, isConsole);
		}


	}
}
