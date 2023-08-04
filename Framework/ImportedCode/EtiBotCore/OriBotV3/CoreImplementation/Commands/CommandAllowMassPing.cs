using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.CoreImplementation.Handlers;
using OldOriBot.Data;
using OldOriBot.Data.Commands.ArgData;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.PermissionData;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Responding;

namespace OldOriBot.CoreImplementation.Commands {
	public class CommandAllowMassPing : Command {
		public override string Name { get; } = "allowmassping";
		public override string[] Aliases { get; } = { "amp" };
		public override string Description { get; } = "Causes the anti-spam system's mention limiter to ignore the next message the provided user sends.";
		public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Person>("user").SetRequiredState(true);
		public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
		public override bool RequiresContext { get; } = true;
		public CommandAllowMassPing(BotContext ctx) : base(ctx) { }

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			if (argArray.Length < 1) {
				throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
			} else if (argArray.Length > 1) {
				throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
			}

			HandlerAntiSpamSystem antispam = executionContext.GetPassiveHandlerInstance<HandlerAntiSpamSystem>();
			if (antispam == null) throw new CommandException(this, Personality.Get("cmd.ori.allowMassPing.notImplemented"));

			ArgumentMap<Person> args = Syntax.SetContext(executionContext).Parse<Person>(argArray[0]);
			Member mbr = args.Arg1?.Member;
			if (mbr == null) {
				throw new CommandException(this, Personality.Get("cmd.err.noMemberFound"));
			}

			if (!antispam.UsersWhoCanBypassPingLimits.Contains(mbr.ID)) {
				antispam.UsersWhoCanBypassPingLimits.Add(mbr.ID);
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.allowMassPing.success", mbr.Mention), mentions: AllowedMentions.Reply);
			} else {
				throw new CommandException(this, Personality.Get("cmd.ori.allowMassPing.alreadyAllowed", mbr.Mention));
			}
		}
	}
}
