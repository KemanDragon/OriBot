using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using OldOriBot.Data;
using OldOriBot.Data.Commands.ArgData;
using OldOriBot.Data.Persistence;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.PermissionData;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Responding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OldOriBot.CoreImplementation.Commands {
	public class CommandBan : Command {

		public override string Name { get; } = "ban";
		public override string Description { get; } = "Bans a member from the server and logs it.";
		public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Person, string, uint>("user", "reason", "deleteMessageHistoryDays").SetRequiredState(true, true, false);
		public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
		public CommandBan(BotContext container) : base(container) { }

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			if (argArray.Length < 2) {
				throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(1)} and/or {Syntax.GetArgName(0)}"));
			} else if (argArray.Length > 3) {
				throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
			}

			ArgumentMap<Person, string, uint> args = Syntax.SetContext(executionContext).Parse<Person, string, uint>(argArray[0], argArray[1], argArray.ElementAtOrDefault(2));
			if (args.Arg1.Member == null) throw new CommandException(this, Personality.Get("cmd.err.noMemberFound"));
			if (string.IsNullOrWhiteSpace(args.Arg2)) throw new CommandException(this, "Please provide an actual reason for this ban, not blank text.");
			if (args.Arg3 > 7) throw new CommandException(this, new ArgumentOutOfRangeException(Syntax.GetArgName(2) + " cannot be greater than 7 or less than 0!"));

			if (executionContext is BotContextOriTheGame ctxOri) {
				ctxOri.IgnoreBannedIDs.Add(args.Arg1.Member.ID);
			}
			await args.Arg1.Member.BanAsync(args.Arg2);
			InfractionLogProvider logProvider = InfractionLogProvider.GetProvider(executionContext);
			logProvider.AppendBan(executor.ID, args.Arg1.Member.ID, args.Arg2, DateTimeOffset.UtcNow);
			await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Administering last rites to <@{args.Arg1.Member.ID}> c:\nThis member has been banned. Message history up to {args.Arg3} days old has been deleted. Log has been updated.");
		}
	}
}
