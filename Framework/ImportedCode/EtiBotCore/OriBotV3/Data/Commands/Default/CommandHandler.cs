using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Responding;

namespace OldOriBot.Data.Commands.Default {
	public class CommandHandler : Command {
		public override string Name { get; } = "handler";
		public override string Description { get; } = "Base command pertaining to the Passive Handler system.";
		public override ArgumentMapProvider Syntax { get; }

		public override Command[] Subcommands { get; }

		public override bool IsExclusiveBase { get; } = true;

		public CommandHandler() : base(null) {
			Subcommands = new Command[] {
				new CommandListHandlers(null, this),
				new CommandGetHandler(null, this)
			};
		}

		public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) => throw new NotImplementedException();


		public class CommandListHandlers : Command {
			public override string Name { get; } = "list";
			public override string Description { get; } = "Lists all PassiveHandlers instantiated by this server's BotContext";
			public override ArgumentMapProvider Syntax { get; }
			public override bool RequiresContext { get; } = true;
			public CommandListHandlers(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				EmbedBuilder builder = new EmbedBuilder {
					Title = "PassiveHandler Instances",
					Description = ""
				};
				foreach (PassiveHandler handler in executionContext.Handlers) {
					builder.Description += "- " + handler.Name + "\n";
				}
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, builder.Build(), AllowedMentions.Reply);
			}
		}

		public class CommandGetHandler : Command {
			public override string Name { get; } = "get";
			public override string Description { get; } = "Displays information about a specific PassiveHandler instance.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string>("handlerName").SetRequiredState(true);

			public CommandGetHandler(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length > 1) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				} else if (argArray.Length == 0) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
				}

				ArgumentMap<string> args = Syntax.Parse<string>(argArray[0]);
				string name = args.Arg1;
				PassiveHandler instance = executionContext.FindPassiveHandlerInstance(name);
				if (instance == null) {
					throw new CommandException(this, "Unable to find a PassiveHandler with the given name!");
				}
				EmbedBuilder builder = new EmbedBuilder {
					Title = "Passive Handler: " + instance.Name,
					Description = instance.Description
				};
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, builder.Build(), AllowedMentions.Reply);
			}
		}
	}
}
