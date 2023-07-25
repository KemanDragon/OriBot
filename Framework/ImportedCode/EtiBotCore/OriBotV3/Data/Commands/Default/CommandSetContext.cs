using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.Interaction.CommandData;
using OldOriBot.PermissionData;
using OldOriBot.Utility.Arguments;

namespace OldOriBot.Data.Commands.Default {
	public class CommandSetContext : Command {
		public CommandSetContext() : base(null) { }

		public override string Name { get; } = "setcontext";
		public override string Description { get; } = "Sets the target BotContext that console commands will run in.";
		public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string>("contextName").SetRequiredState(false);
		/*public override CommandUsagePacket CanRunCommand(Member member) {
			return new CommandUsagePacket(false, Personality.Get("cmd.err.consoleOnly"));
		}*/
		public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.BotDeveloper;

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			ArgumentMapProvider<string> syntax = (ArgumentMapProvider<string>)Syntax;
			ArgumentMap<string> map = syntax.Parse(argArray.FirstOrDefault());

			string data = map.Arg1;
			if (data == null || data == "null") {
				CommandMarshaller.TargetContext = null;
				if (isConsole) {
					CommandLogger.WriteLine("§aSet target context to §dnull");
				} else {
					await originalMessage.ReplyAsync("Set target context to `null`", null, AllowedMentions.Reply);
				}
				return;
			}

			foreach (BotContext ctx in BotContextRegistry.GetContexts()) {
				if (ctx.Name.ToLower() == data.ToLower() || ctx.DataPersistenceName.ToLower() == data.ToLower()) {
					CommandMarshaller.TargetContext = ctx;
					if (isConsole) {
						CommandLogger.WriteLine("§aSet target context to §d" + ctx.Name);
					} else {
						await originalMessage.ReplyAsync("Set target context to `" + ctx.Name + "`", null, AllowedMentions.Reply);
					}
					return;
				}
			}
		}
	}
}
