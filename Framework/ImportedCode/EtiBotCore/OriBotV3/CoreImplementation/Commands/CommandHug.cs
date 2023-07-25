using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.Interaction;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Responding;

namespace OldOriBot.CoreImplementation.Commands {
	public class CommandHug : Command {
		public override string Name { get; } = "hug";
		public override string Description { get; } = "Sometimes you just need a hug.";
		public override ArgumentMapProvider Syntax { get; }
		public override string[] Aliases { get; } = {
			"<:ori_hug_ku:693635899312963605>"
		};
		public CommandHug(BotContext ctx) : base(ctx) { }

		public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			return ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "<:ori_hug_ku:693635899312963605>", null, AllowedMentions.Reply);
		}
	}
}
