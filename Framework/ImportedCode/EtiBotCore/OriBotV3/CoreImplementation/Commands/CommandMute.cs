using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Utility.Marshalling;
using OldOriBot.Data;
using OldOriBot.Data.Commands.ArgData;
using OldOriBot.Data.MemberInformation;
using OldOriBot.Data.Persistence;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.PermissionData;
using OldOriBot.Utility;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Extensions;
using OldOriBot.Utility.Responding;

namespace OldOriBot.CoreImplementation.Commands {
	public class CommandMute : Command {
		public override string Name { get; } = "mute";
		public override string Description { get; } = "Controls the muted state of a given user. :warning: Note that this variant is obsolete! `>> mute add` should be used in favor of this alone.";
		public override ArgumentMapProvider Syntax { get; }// = new ArgumentMapProvider<Person, Duration, string>("user", "length", "reason").SetRequiredState(true, true, true);
		public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
		public override bool RequiresContext { get; } = true;
		public override bool IsExclusiveBase { get; } = true;
		public override Command[] Subcommands { get; }
		public CommandMute(BotContext container) : base(container) {
			Subcommands = new Command[] {
				new CommandMuteAdd(container, this),
				new CommandMuteRemove(container, this),
				new CommandMuteTweak(container, this),
				new CommandGetMuteInfo(container, this),
				new CommandMuteList(container, this)
			};
		}

		public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			/*
			await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"{EmojiLookup.GetEmoji("warning")} This variant of the mute command is **obsolete**! Use `>> mute add` instead", null, AllowedMentions.Reply);
			// Tell mods to stop using old fashioned mute

			CommandMuteAdd addCmd = (CommandMuteAdd)Subcommands.FirstOrDefault(cmd => cmd is CommandMuteAdd);
			await addCmd.ExecuteCommandAsync(executor, executionContext, originalMessage, argArray, rawArgs, isConsole);
			*/
			return Task.CompletedTask;
		}

		#region Add / Remove Mutes

		public class CommandMuteAdd : Command {
			public override string Name { get; } = "add";
			public override string Description { get; } = "Mutes someone by adding them to the mute registry.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Person, string, Duration>("user", "reason", "length").SetRequiredState(true, true, true);
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override bool RequiresContext { get; } = true;
			public CommandMuteAdd(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length < 3) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(1)} and/or {Syntax.GetArgName(0)}"));
				} else if (argArray.Length > 3) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}

				await originalMessage.ServerChannel.StartTypingAsync();
				ArgumentMap<Person, string, Duration> args = Syntax.SetContext(executionContext).Parse<Person, string, Duration>(argArray[0], argArray[1], argArray[2]);
				MemberMuteUtility muter = MemberMuteUtility.GetOrCreate(executionContext);

				if (args.Arg3.Malformed) {
					throw new CommandException(this, "Invalid formatting for duration string. Say `>> typeinfo duration` to get more information.");
				}
				Member mbr = args.Arg1?.Member;
				if (mbr == null) {
					throw new CommandException(this, "I was unable to find a member from your query.");
				}
				if (muter.IsMutedInRegistry(mbr.ID)) {
					throw new CommandException(this, $"User {mbr.Mention} is already muted! Use the tweak subcommand to change the duration of their mute.");
				}
				if (mbr.GetPermissionLevel() == PermissionLevel.Bot) {
					throw new CommandException(this, "no");
				}
				if (mbr.GetPermissionLevel() >= PermissionLevel.Operator) {
					throw new CommandException(this, $"I can't mute {mbr.Mention} because they are at {PermissionLevel.Operator.GetFullName()} or above!");
				}

				//InfractionLogProvider provider = InfractionLogProvider.GetProvider(executionContext);

				MemberMuteUtility.ActionType muteType = await muter.MuteMemberAsync(mbr, args.Arg3.TimeSpan, args.Arg2, executor);
				if (muteType == MemberMuteUtility.ActionType.Muted || muteType == MemberMuteUtility.ActionType.OnlyRegistered) {
					//provider.AppendMute(executor, mbr.ID, args.Arg2, false);
				}
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "Done.", mentions: AllowedMentions.Reply);
			}
		}

		public class CommandMuteRemove : Command {
			public override string Name { get; } = "remove";
			public override string Description { get; } = "Unmutes the given user.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Variant<Snowflake, Person>, string>("user", "reason").SetRequiredState(true, true);
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override bool RequiresContext { get; } = true;
			public CommandMuteRemove(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length < 2) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(1)} and/or {Syntax.GetArgName(0)}"));
				} else if (argArray.Length > 2) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}

				await originalMessage.ServerChannel.StartTypingAsync();
				ArgumentMap<Variant<Snowflake, Person>, string> args = Syntax.SetContext(executionContext).Parse<Variant<Snowflake, Person>, string>(argArray[0], argArray[1]);
				Variant<Snowflake, Person> target = args.Arg1;
				MemberMuteUtility muter = MemberMuteUtility.GetOrCreate(executionContext);
				if (target.ArgIndex == 1) {
					if (!muter.IsMutedInRegistry(target.Value1)) {
						throw new CommandException(this, $"User <@{target.Value1}> is not muted!");
					}
					await muter.UnmuteMemberByIDAsync(target.Value1, args.Arg2);
				} else {
					Member mbr = target.Value2.Member;
					if (mbr == null) {
						throw new CommandException(this, "I was unable to find a member from your query.");
					}
					if (!muter.IsMutedInRegistry(mbr.ID)) {
						throw new CommandException(this, $"User {mbr.Mention} is not muted!");
					}
					//InfractionLogProvider provider = InfractionLogProvider.GetProvider(executionContext);
					MemberMuteUtility.ActionType action = await muter.UnmuteMemberAsync(mbr, args.Arg2, executor);
					if (action == MemberMuteUtility.ActionType.Unmuted || action == MemberMuteUtility.ActionType.OnlyUnregistered) {
						//provider.AppendUnmute(executor, mbr.ID, args.Arg2, false);
					}
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "Done.", mentions: AllowedMentions.Reply);
				}
			}
		}

		#endregion

		#region Get Mute Information

		public class CommandGetMuteInfo : Command {
			public override string Name { get; } = "info";
			public override string Description { get; } = "Displays information about the muted status of a given user, or if no user is given, lists all ongoing mutes.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Variant<Snowflake, Person>>("user").SetRequiredState(true);
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override bool RequiresContext { get; } = true;
			public CommandGetMuteInfo(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length == 0) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
				} else if (argArray.Length > 1) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}

				await originalMessage.ServerChannel.StartTypingAsync();
				ArgumentMap<Variant<Snowflake, Person>> args = Syntax.SetContext(executionContext).Parse<Variant<Snowflake, Person>>(argArray[0]);
				Variant<Snowflake, Person> target = args.Arg1;
				MemberMuteUtility muter = MemberMuteUtility.GetOrCreate(executionContext);
				EmbedBuilder builder = new EmbedBuilder {
					Title = "Mute Information",
					Description = "The status of this member and whether or not they are muted."
				};
				if (target.ArgIndex == 1) {
					// by ID
					Snowflake id = target.Value1;
					if (!muter.IsMutedInRegistry(id)) {
						builder.AddField("Muted", "No");
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, builder.Build(), AllowedMentions.Reply);
						return;
					}
					builder.AddField("Muted", "Yes", true);
					builder.AddField("Muted At", muter.GetMutedAt(id).InEUFormat(), true);
					builder.AddField("Unmuted At", muter.GetUnmutedAt(id).InEUFormat(), true);
					builder.AddField("Mute Length", muter.GetMuteDuration(id).GetTimeDifference());
					builder.AddTimeFormatFooter();
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, builder.Build(), AllowedMentions.Reply);
					return;
				} else {
					// by member
					Member person = target.Value2.Member;
					if (person == null) throw new CommandException(this, Personality.Get("cmd.err.noMemberFound"));
					Snowflake id = person.ID;
					if (!muter.IsMutedInRegistry(id)) {
						builder.AddField("Muted", "No");
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, builder.Build(), AllowedMentions.Reply);
						return;
					}
					builder.AddField("Muted", "Yes", true);
					builder.AddField("Muted At", muter.GetMutedAt(id).InEUFormat(), true);
					builder.AddField("Unmuted At", muter.GetUnmutedAt(id).InEUFormat(), true);
					builder.AddField("Mute Length", muter.GetMuteDuration(id).GetTimeDifference());
					builder.AddField("Remaining Time", muter.GetRemainingMuteTime(id).ToString());
					builder.AddTimeFormatFooter();
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, builder.Build(), AllowedMentions.Reply);
					return;
				}
			}
		}

		public class CommandMuteList : Command {
			public override string Name { get; } = "list";
			public override string Description { get; } = "Lists all ongoing mutes.";
			public override ArgumentMapProvider Syntax { get; }
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override bool RequiresContext { get; } = true;
			public CommandMuteList(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				// Storage.SetValue("MUTE_NAME_CACHE_" + mbr.ID, mbr.FullName);
				MemberMuteUtility muter = MemberMuteUtility.GetOrCreate(executionContext);
				EmbedBuilder listBuilder = new EmbedBuilder {
					Title = "Mute List",
					Description = "**All Muted Users:**\n"
				};
				foreach (Snowflake memberId in muter.MutedUserIDs) {
					listBuilder.Description += $"• [{memberId}] (<@!{memberId}>) | {muter.GetCachedNameOf(memberId)}\n";
				}
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, listBuilder.Build(), AllowedMentions.Reply);
				return;
			}
		}

		#endregion

		#region Tweak Mutes

		public class CommandMuteTweak : Command {

			public override string Name { get; } = "tweak";
			public override string Description { get; } = "Change the amount of time the user will be muted for.";
			public override ArgumentMapProvider Syntax { get; }
			public override bool IsExclusiveBase { get; } = true;
			public override Command[] Subcommands { get; }
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override bool RequiresContext { get; } = true;

			public CommandMuteTweak(BotContext container, Command parent) : base(container, parent) {
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

					await originalMessage.ServerChannel.StartTypingAsync();
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
						TimeSpan originalTime = muter.GetMuteDuration(id);

						InfractionLogProvider provider = InfractionLogProvider.GetProvider(executionContext);
						muter.AddMuteTime(id, args.Arg2);
						provider.AppendMuteTweak(executor, id, "<<No Reason Required>>", originalTime, originalTime + args.Arg2.TimeSpan, false);

						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Added {args.Arg2.TimeInGivenUnit} {args.Arg2.Unit} to <@{id}>'s mute time.");
					} else {
						Member mbr = args.Arg1.Value2?.Member;
						if (mbr == null) {
							throw new CommandException(this, "I was unable to find a member from your query.");
						}
						if (!muter.IsMutedInRegistry(mbr.ID)) {
							throw new CommandException(this, $"Cannot add mute time to user {mbr.Mention} because they aren't muted!");
						}

						TimeSpan originalTime = muter.GetMuteDuration(mbr.ID);
						InfractionLogProvider provider = InfractionLogProvider.GetProvider(executionContext);
						muter.AddMuteTime(mbr.ID, args.Arg2);
						provider.AppendMuteTweak(executor, mbr.ID, "<<No Reason Required>>", originalTime, originalTime + args.Arg2.TimeSpan, false);

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

					await originalMessage.ServerChannel.StartTypingAsync();
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
						TimeSpan originalTime = muter.GetMuteDuration(id);
						InfractionLogProvider provider = InfractionLogProvider.GetProvider(executionContext);

						if (await muter.SubtractMuteTime(id, args.Arg2)) {
							provider.AppendMuteTweak(executor, id, "<<No Reason Required>>", originalTime, originalTime + args.Arg2.TimeSpan, false);
							await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Subtracted {args.Arg2.TimeInGivenUnit} {args.Arg2.Unit} from <@{id}>'s mute time.");
						} else {
							provider.AppendUnmute(executor, id, "<<Mute Tweak Issued // New Duration Below Zero>>", false);
							await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Subtracted {args.Arg2.TimeInGivenUnit} {args.Arg2.Unit} from <@{id}>'s mute time, which subsequently caused the amount of time they need to be muted for to go below zero. They have been unmuted.");
						}
					} else {
						Member mbr = args.Arg1.Value2?.Member;
						if (mbr == null) {
							throw new CommandException(this, "I was unable to find a member from your query.");
						}
						if (!muter.IsMutedInRegistry(mbr.ID)) {
							throw new CommandException(this, $"Cannot subtract mute time from user {mbr.Mention} because they aren't muted!");
						}
						TimeSpan originalTime = muter.GetMuteDuration(mbr.ID);
						InfractionLogProvider provider = InfractionLogProvider.GetProvider(executionContext);

						if (await muter.SubtractMuteTime(mbr.ID, args.Arg2)) {
							provider.AppendMuteTweak(executor, mbr.ID, "<<No Reason Required>>", originalTime, originalTime + args.Arg2.TimeSpan, false);
							await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Subtracted {args.Arg2.TimeInGivenUnit} {args.Arg2.Unit} from {mbr.Mention}'s mute time.");
						} else {
							provider.AppendUnmute(executor, mbr.ID, "<<Mute Tweak Issued // New Duration Below Zero>>", false);
							await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Subtracted {args.Arg2.TimeInGivenUnit} {args.Arg2.Unit} from {mbr.Mention}'s mute time, which subsequently caused the amount of time they need to be muted for to go below zero. They have been unmuted.");
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

					await originalMessage.ServerChannel.StartTypingAsync();
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
						TimeSpan originalTime = muter.GetMuteDuration(id);
						InfractionLogProvider provider = InfractionLogProvider.GetProvider(executionContext);

						if (await muter.SetMuteDuration(id, args.Arg2)) {
							provider.AppendMuteTweak(executor, id, "<<No Reason Required>>", originalTime, originalTime + args.Arg2.TimeSpan, false);
							await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Set the duration of <@{id}>'s mute to {args.Arg2.TimeInGivenUnit} {args.Arg2.Unit}.");
						} else {
							provider.AppendUnmute(executor, id, "<<Mute Tweak Issued // New Duration Below Zero>>", false);
							await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Set the duration of <@{id}>'s mute to {args.Arg2.TimeInGivenUnit} {args.Arg2.Unit}, which subsequently caused the amount of time they need to be muted for to go below zero. They have been unmuted.");
						}
					} else {
						Member mbr = args.Arg1.Value2?.Member;
						if (mbr == null) {
							throw new CommandException(this, "I was unable to find a member from your query.");
						}
						if (!muter.IsMutedInRegistry(mbr.ID)) {
							throw new CommandException(this, $"Cannot set the mute duration of user {mbr.Mention} because they aren't muted!");
						}
						TimeSpan originalTime = muter.GetMuteDuration(mbr.ID);
						InfractionLogProvider provider = InfractionLogProvider.GetProvider(executionContext);

						if (await muter.SetMuteDuration(mbr.ID, args.Arg2)) {
							provider.AppendMuteTweak(executor, mbr.ID, "<<No Reason Required>>", originalTime, originalTime + args.Arg2.TimeSpan, false);
							await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Set the duration of {mbr.Mention}'s mute to {args.Arg2.TimeInGivenUnit} {args.Arg2.Unit}.");
						} else {
							provider.AppendUnmute(executor, mbr.ID, "<<Mute Tweak Issued // New Duration Below Zero>>", false);
							await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Set the duration of {mbr.Mention}'s mute to {args.Arg2.TimeInGivenUnit} {args.Arg2.Unit}, which subsequently caused the amount of time they need to be muted for to go below zero. They have been unmuted.");
						}
					}
				}
			}
		}

		#endregion
	}
}
