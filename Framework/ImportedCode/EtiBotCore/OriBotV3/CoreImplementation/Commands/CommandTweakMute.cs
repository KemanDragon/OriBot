using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.Utility.Marshalling;
using OldOriBot.Data;
using OldOriBot.Data.Commands.ArgData;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.PermissionData;
using OldOriBot.Utility;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Responding;

namespace OldOriBot.CoreImplementation.Commands {

	[Obsolete("This is now a subcommand of mute", true)] 
	public class CommandTweakMute : Command {

		public override string Name { get; } = "tweakmute";
		public override string Description { get; } = "Change the amount of time the user will be muted for.";
		public override ArgumentMapProvider Syntax { get; }
		public override bool IsExclusiveBase { get; } = true;
		public override Command[] Subcommands { get; }
		public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
		public override bool RequiresContext { get; } = true;

		public CommandTweakMute(BotContext container) : base(container) {
			Subcommands = new Command[] {
				new CommandTweakMuteAdd(container, this),
				new CommandTweakMuteSub(container, this),
				new CommandTweakMuteSet(container, this),
			};
		}

		public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) => throw new NotImplementedException();

		public class CommandTweakMuteAdd : Command {
			public override string Name { get; } = "add";
			public override string Description { get; } = "Adds the given duration to this user's mute time.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Variant<Snowflake, Person>, Duration>("mutedUser", "addTime").SetRequiredState(true, true);
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override bool RequiresContext { get; } = true;
			public CommandTweakMuteAdd(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length < 2) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(1)} and/or {Syntax.GetArgName(0)}"));
				} else if (argArray.Length > 2) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}

				MemberMuteUtility muter = MemberMuteUtility.GetOrCreate(executionContext);
				ArgumentMap<Variant<Snowflake, Person>, Duration> args = Syntax.SetContext(executionContext).Parse<Variant<Snowflake, Person>, Duration>(argArray[0], argArray[1]);
				if (args.Arg2.Malformed) {
					throw new ArgumentException("Invalid formatting for duration string. Say `>> typeinfo duration` to get more information.");
				}
				if (args.Arg1.ArgIndex == 1) {
					Snowflake id = args.Arg1.Value1;
					if (!muter.IsMutedInRegistry(id)) {
						throw new CommandException(this, $"Cannot add mute time to user <@{id}> because they aren't muted!");
					}
					muter.AddMuteTime(id, args.Arg2);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Added {args.Arg2.TimeInGivenUnit} {args.Arg2.Unit} to <@{id}>'s mute time.");
				} else {
					Member mbr = args.Arg1.Value2?.Member;
					if (mbr == null) {
						throw new CommandException(this, "I was unable to find a member from your query.");
					}
					if (!muter.IsMutedInRegistry(mbr.ID)) {
						throw new CommandException(this, $"Cannot add mute time to user {mbr.Mention} because they aren't muted!");
					}
					muter.AddMuteTime(mbr.ID, args.Arg2);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Added {args.Arg2.TimeInGivenUnit} {args.Arg2.Unit} to {mbr.Mention}'s mute time.");
				}
			}
		}

		public class CommandTweakMuteSub : Command {
			public override string Name { get; } = "sub";
			public override string Description { get; } = "Change the amount of time the user will be muted for, providing a new duration relative to now. For instance, if 24h is input, the user will be unmuted 24h from when you ran this command.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Variant<Snowflake, Person>, Duration>("mutedUser", "removeTime").SetRequiredState(true, true);
			public override string[] Aliases { get; } = {
				"subtract"
			};
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override bool RequiresContext { get; } = true;
			public CommandTweakMuteSub(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length < 2) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(1)} and/or {Syntax.GetArgName(0)}"));
				} else if (argArray.Length > 2) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}

				MemberMuteUtility muter = MemberMuteUtility.GetOrCreate(executionContext);
				ArgumentMap<Variant<Snowflake, Person>, Duration> args = Syntax.SetContext(executionContext).Parse<Variant<Snowflake, Person>, Duration>(argArray[0], argArray[1]);
				if (args.Arg2.Malformed) {
					throw new ArgumentException("Invalid formatting for duration string. Say `>> typeinfo duration` to get more information.");
				}
				if (args.Arg1.ArgIndex == 1) {
					Snowflake id = args.Arg1.Value1;
					if (!muter.IsMutedInRegistry(id)) {
						throw new CommandException(this, $"Cannot subtract mute time from user <@{id}> because they aren't muted!");
					}
					if (await muter.SubtractMuteTime(id, args.Arg2)) {
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Subtracted {args.Arg2.TimeInGivenUnit} {args.Arg2.Unit} from <@{id}>'s mute time.");
					}
				} else {
					Member mbr = args.Arg1.Value2?.Member;
					if (mbr == null) {
						throw new CommandException(this, "I was unable to find a member from your query.");
					}
					if (!muter.IsMutedInRegistry(mbr.ID)) {
						throw new CommandException(this, $"Cannot subtract mute time from user {mbr.Mention} because they aren't muted!");
					}
					if (await muter.SubtractMuteTime(mbr.ID, args.Arg2)) {
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Subtracted {args.Arg2.TimeInGivenUnit} {args.Arg2.Unit} from {mbr.Mention}'s mute time.");
					}
				}
			}
		}

		public class CommandTweakMuteSet : Command {
			public override string Name { get; } = "set";
			public override string Description { get; } = "Change the amount of time the user will be muted for, providing a *replacement* duration for the original time. If this is set to a duration shorter than the amount of time they have been muted so far, it will unmute them.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Variant<Snowflake, Person>, Duration>("mutedUser", "replacementDuration").SetRequiredState(true, true);
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override bool RequiresContext { get; } = true;
			public CommandTweakMuteSet(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length < 2) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(1)} and/or {Syntax.GetArgName(0)}"));
				} else if (argArray.Length > 2) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}

				MemberMuteUtility muter = MemberMuteUtility.GetOrCreate(executionContext);
				ArgumentMap<Variant<Snowflake, Person>, Duration> args = Syntax.SetContext(executionContext).Parse<Variant<Snowflake, Person>, Duration>(argArray[0], argArray[1]);
				if (args.Arg2.Malformed) {
					throw new ArgumentException("Invalid formatting for duration string. Say `>> typeinfo duration` to get more information.");
				}
				if (args.Arg1.ArgIndex == 1) {
					Snowflake id = args.Arg1.Value1;
					if (!muter.IsMutedInRegistry(id)) {
						throw new CommandException(this, $"Cannot set the mute duration of user <@{id}> because they aren't muted!");
					}
					if (await muter.SetMuteDuration(id, args.Arg2)) {
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Set the duration of <@{id}>'s mute to {args.Arg2.TimeInGivenUnit} {args.Arg2.Unit}.");
					}
				} else {
					Member mbr = args.Arg1.Value2?.Member;
					if (mbr == null) {
						throw new CommandException(this, "I was unable to find a member from your query.");
					}
					if (!muter.IsMutedInRegistry(mbr.ID)) {
						throw new CommandException(this, $"Cannot set the mute duration of user {mbr.Mention} because they aren't muted!");
					}
					if (await muter.SetMuteDuration(mbr.ID, args.Arg2)) {
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Set the duration of {mbr.Mention}'s mute to {args.Arg2.TimeInGivenUnit} {args.Arg2.Unit}.");
					}
				}
			}
		}
	}
}
