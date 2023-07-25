using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.Data.Commands.ArgData;
using OldOriBot.Data.MemberInformation;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.Interaction.CommandData;
using OldOriBot.PermissionData;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Extensions;
using OldOriBot.Utility.Responding;

namespace OldOriBot.Data.Commands.Default {
	public class CommandPerms : Command {

		public override string Name { get; } = "perms";
		public override string Description { get; } = "Commands pertaining to the permission level of users.";
		public override ArgumentMapProvider Syntax { get; }

		public override Command[] Subcommands { get; }

		public override bool IsExclusiveBase { get; } = true;

		public CommandPerms() : base(null) {
			Subcommands = new Command[] {
				new CommandSetPerms(null, this),
				new CommandGetPerms(null, this),
				new CommandListPerms(null, this)
			};
		}

		public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) => throw new NotImplementedException();

		public class CommandSetPerms : Command {
			public override string Name { get; } = "set";
			public override string Description { get; } = "Sets the permission level of a given member.";
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Person, byte>("user", "permissionLevel").SetRequiredState(true, true);

			public override CommandUsagePacket CanRunCommand(Member member) {
				if (member.ID == 114163433980559366) {
					// It's me.
					return CommandUsagePacket.Success;
				}
				return base.CanRunCommand(member);
			}

			public CommandSetPerms(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length < 2) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(1)} and/or {Syntax.GetArgName(0)}"));
				} else if (argArray.Length > 2) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}

				ArgumentMap<Person, byte> args = Syntax.SetContext(executionContext).Parse<Person, byte>(argArray[0], argArray[1]);
				Person person = args.Arg1;
				if (person?.Member == null) {
					throw new CommandException(this, Personality.Get("cmd.err.noMemberFound"));
				}

				Member target = person.Member;

				if (target.IsShallow) {
					throw new CommandException(this, Personality.Get("cmd.err.nonMember"));
				}

				if (target == executor && target.ID == 114163433980559366) {
					PermissionLevel newPermsPre = (PermissionLevel)args.Arg2;
					//target.SetPermissionLevel(newPermsPre);
					executionContext.SetPermissionsOf(target, newPermsPre);
					//await originalMessage.ReplyAsync(Personality.Get("cmd.setPerms.success", target.Mention, newPermsPre.GetFullName()) + "\n\n(This is an override - you have the ability to modify your own permissions no matter what.)", null, AllowedMentions.Reply);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.setPerms.success", target.Mention, newPermsPre.GetFullName()) + "\n\n(This is an override - you have the ability to modify your own permissions no matter what.)", null, AllowedMentions.Reply);
					return;
				}

				if (target.IsSelf) {
					throw new CommandException(this, Personality.Get("cmd.setPerms.changebot", PermissionLevel.Bot.GetFullName()));
				} else if (target == executor) {
					throw new CommandException(this, Personality.Get("cmd.setPerms.self"));
				} else if (target.GetPermissionLevel() >= executor.GetPermissionLevel()) {
					throw new CommandException(this, Personality.Get("cmd.setPerms.otherEqualOrHigher"));
				}

				if (args.Arg2 == (byte)PermissionLevel.Nonmember) {
					throw new CommandException(this, Personality.Get("cmd.setPerms.nonmember", PermissionLevel.Nonmember.GetFullName(), PermissionLevel.Blacklisted.GetFullName()));
				} else if (args.Arg2 == (byte)PermissionLevel.Bot) {
					throw new CommandException(this, Personality.Get("cmd.setPerms.max", PermissionLevel.Bot.GetFullName()));
				} else if (args.Arg2 >= (byte)executor.GetPermissionLevel()) {
					throw new CommandException(this, Personality.Get("cmd.setPerms.newValueEqualOrHigher", ((PermissionLevel)args.Arg2).GetFullName()));
				}

				PermissionLevel newPerms = (PermissionLevel)args.Arg2;
				//target.SetPermissionLevel(newPerms);
				executionContext.SetPermissionsOf(target, newPerms);
				//await originalMessage.ReplyAsync(Personality.Get("cmd.setPerms.success", target.Mention, newPerms.GetFullName()), null, AllowedMentions.Reply);
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.setPerms.success", target.Mention, newPerms.GetFullName()), null, AllowedMentions.Reply);
			}
		}

		public class CommandGetPerms : Command {
			public override string Name { get; } = "get";
			public override string Description { get; } = "Acquires the permission level of a given member, or whoever run the command if no user was provided.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Person>("user").SetRequiredState(false);
			public CommandGetPerms(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length == 0) {
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.getPerms.success", executor.Mention, executionContext.GetPermissionsOf(executor).GetFullName()), null, AllowedMentions.Reply);
					return;
				} else if (argArray.Length > 1) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}

				ArgumentMap<Person> args = Syntax.SetContext(executionContext).Parse<Person>(argArray[0]);
				Person person = args.Arg1;
				if (person?.Member == null) {
					throw new CommandException(this, Personality.Get("cmd.err.noMemberFound"));
				}

				Member target = person.Member;
				string permName;
				if (target.IsShallow) {
					permName = PermissionLevel.Nonmember.GetFullName();
				} else {
					permName = executionContext.GetPermissionsOf(target).GetFullName();
				}
				//await originalMessage.ReplyAsync(Personality.Get("cmd.getPerms.success", target.Mention, executionContext.GetPermissionsOf(target).GetFullName()), null, AllowedMentions.Reply);
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.getPerms.success", target.Mention, permName), null, AllowedMentions.Reply);
			}
		}

		public class CommandListPerms : Command {
			public override string Name { get; } = "list";
			public override string Description { get; } = "Lists all defined permission levels, that is, permission levels with an associated name.";
			public override ArgumentMapProvider Syntax { get; }
			public CommandListPerms(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				StringBuilder tx = new StringBuilder();
				if (isConsole) {
					foreach (PermissionLevel level in Enum.GetValues(typeof(PermissionLevel))) {
						tx.AppendLine(level.GetFullNameConsole());
					}
					CommandLogger.WriteLine("All permissions:");
					CommandLogger.WriteLine("§2" + tx.ToString());
					return;
				} else {
					foreach (PermissionLevel level in Enum.GetValues(typeof(PermissionLevel))) {
						tx.AppendLine(level.GetFullName());
					}
					EmbedBuilder builder = new EmbedBuilder {
						Title = "All Named Permission Levels",
						Description = tx.ToString()
					};
					await originalMessage.ReplyAsync(null, builder.Build(), AllowedMentions.Reply);
				}
			}
		}
	}

	public class CommandSetPermsObsolete : DeprecatedCommand {
		public override string Name { get; } = "setperms";
		public CommandSetPermsObsolete(CommandPerms.CommandSetPerms permsSetCmd) : base(permsSetCmd) { }
	}

}
