using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EtiBotCore.Data;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.Utility.Marshalling;
using OldOriBot.Data;
using OldOriBot.Data.Commands.ArgData;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.PermissionData;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Responding;

namespace OldOriBot.CoreImplementation.Commands {
	public class CommandPurge : Command {
		public override string Name { get; } = "purge";
		public override string Description { get; } = "Bulk deletes a number of messages. The channel argument is optional, and you can ignore it if needed (e.g. `>> purge 50` will get rid of 50 messages from the channel you use it in). **The maximum amount of messages is 100.**";
		public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Channel, byte>("channel", "amount").SetRequiredState(false, true);
		public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
		public override bool RequiresContext { get; } = true;
		public override Command[] Subcommands { get; }
		public CommandPurge(BotContext ctx) : base(ctx) {
			Subcommands = new Command[] {
				new CommandPurgeFrom(ctx, this),
				new CommandPurgeAfter(ctx, this)
			};
		}

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			if (argArray.Length > 2) {
				throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
			}

			Snowflake channelId = default;
			int numMessages = default;
			if (isConsole) {
				// Console will require both args, because the console doesn't run in a Discord channel. Duh.
				if (argArray.Length < 2) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(1)} and/or {Syntax.GetArgName(0)}"));
				}
				ArgumentMap<Channel, byte> args = Syntax.SetContext(executionContext).Parse<Channel, byte>(argArray[0], argArray[1]);
				channelId = args.Arg1.Target.ID;
				numMessages = args.Arg2;
				if (channelId < Snowflake.MinValue) {
					throw new CommandException(this, "Invalid channel!");
				}
				if (numMessages > 100) {
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, EmojiLookup.GetEmoji("warning") + " The amount of messages cannot be greater than 100! It has been reduced to 100.");
					numMessages = 100;
				}
			} else {
				// Channel only requires one, but can use both.
				if (argArray.Length < 1) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(1))); // yes 1
				}
				if (argArray.Length == 1) {
					channelId = originalMessage.Channel.ID;
					if (byte.TryParse(argArray[0], out byte msgs)) {
						numMessages = msgs;
						if (numMessages > 100) {
							await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, EmojiLookup.GetEmoji("warning") + " The amount of messages cannot be greater than 100! It has been reduced to 100.");
							numMessages = 100;
						}
					} else {
						throw new CommandException(this, "Unable to convert the input parameter to a number of messages!");
					}
				} else if (argArray.Length == 2) {
					ArgumentMap<Channel, byte> args = Syntax.SetContext(executionContext).Parse<Channel, byte>(argArray[0], argArray[1]);
					channelId = args.Arg1.Target.ID;
					numMessages = args.Arg2;
					if (channelId < Snowflake.MinValue) {
						throw new CommandException(this, "Invalid channel!");
					}
					if (numMessages > 100) {
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, EmojiLookup.GetEmoji("warning") + " The amount of messages cannot be greater than 100! It has been reduced to 100.");
						numMessages = 100;
					}
				}
			}

			TextChannel target = executionContext.Server.GetChannel<TextChannel>(channelId);
			if (target == null) {
				throw new CommandException(this, "Invalid channel ID! Input channel was not a text channel.");
			}
			await target.DeleteMessagesAsync(null, numMessages, $"Moderator issued the purge command: Delete the latest {numMessages} message(s).");
		}

		public class CommandPurgeFrom : Command {
			public override string Name { get; } = "from";
			public override string Description { get; } = "Targets an amount of the most recent messages sent by the given user.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Channel, Variant<Snowflake, Person>, byte>("channel", "user", "amount").SetRequiredState(false, true, true);
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override bool RequiresContext { get; } = true; 
			public CommandPurgeFrom(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length > 3) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}
				TextChannel target = default;
				int numMessages = default;
				Snowflake personId = default;
				if (isConsole) {
					if (argArray.Length < 3) {
						throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(2)}, {Syntax.GetArgName(1)}, and/or {Syntax.GetArgName(0)}"));
					}
					ArgumentMap<Channel, Variant<Snowflake, Person>, byte> args = Syntax.SetContext(executionContext).Parse<Channel, Variant<Snowflake, Person>, byte>(argArray.ElementAtOrDefault(0), argArray.ElementAtOrDefault(1), argArray.ElementAtOrDefault(2));
					target = args.Arg1.Target as TextChannel;
					Variant<Snowflake, Person> prsn = args.Arg2;
					if (prsn.ArgIndex == 1) {
						personId = prsn.Value1;
					} else {
						personId = prsn.Value2.Member?.ID ?? default;
					}
					numMessages = args.Arg3;
				} else {
					if (argArray.Length < 2) {
						throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(2)}, and/or {Syntax.GetArgName(1)}")); // yes skip 0
					}
					if (argArray.Length == 3) {
						ArgumentMap<Channel, Variant<Snowflake, Person>, byte> args = Syntax.SetContext(executionContext).Parse<Channel, Variant<Snowflake, Person>, byte>(argArray.ElementAtOrDefault(0), argArray.ElementAtOrDefault(1), argArray.ElementAtOrDefault(2));

					} else if (argArray.Length == 2) {
						ArgumentMap<Channel, Variant<Snowflake, Person>, byte> args = Syntax.SetContext(executionContext).Parse<Channel, Variant<Snowflake, Person>, byte>(null, argArray.ElementAtOrDefault(0), argArray.ElementAtOrDefault(1));
						target = originalMessage.ServerChannel;
						Variant<Snowflake, Person> prsn = args.Arg2;
						if (prsn.ArgIndex == 1) {
							personId = prsn.Value1;
						} else {
							personId = prsn.Value2.Member?.ID ?? default;
						}
						numMessages = args.Arg3;
					}
				}
				if (target == null) {
					throw new CommandException(this, "Invalid channel ID! Input channel was not a text channel.");
				}
				if (personId < Snowflake.MinValue) {
					throw new CommandException(this, "Invalid user!");
				}
				User user = await User.GetOrDownloadUserAsync(personId);
				await target.DeleteMessagesAsync(channelMessage => channelMessage.Author?.ID == personId, numMessages, $"Moderator issued the purge command: Delete the last {numMessages} message(s) sent by {user?.FullName ?? personId.ToString()}");
			}
		}

		public class CommandPurgeAfter : Command {
			public override string Name { get; } = "after";
			public override string Description { get; } = "Deletes all messages sent after the given message ID. This will also delete the given message too.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Variant<Snowflake, string>>("messageIdOrJumpLink").SetRequiredState(true);
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override bool RequiresContext { get; } = true; 
			public CommandPurgeAfter(BotContext ctx, Command parent) : base(ctx, parent) { }
			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length > 1) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}
				ArgumentMap<Variant<Snowflake, string>> args = Syntax.SetContext(executionContext).Parse<Variant<Snowflake, string>>(argArray[0]);
				Variant<Snowflake, string> target = args.Arg1;

				Snowflake channelId = default;
				Snowflake messageId = default;
				if (target.ArgIndex == 1) {
					messageId = target.Value1;
					channelId = originalMessage.Channel.ID;
				} else {
					Match match = Regex.Match(target.Value2, @"https://discord.com/channels/(\d+)/(\d+)/(\d+)");
					if (match.Success) {
						Snowflake.TryParse(match.Groups[1].Value, out Snowflake server);
						channelId = Snowflake.Parse(match.Groups[2].Value);
						messageId = Snowflake.Parse(match.Groups[3].Value);
						if (server != executionContext.Server.ID) {
							throw new CommandException(this, "The given jump link does not point to the server you are running this command in!");
						}
					} else {
						throw new CommandException(this, "Expecting either a message ID or a jump link.");
					}
				}

				TextChannel targetChannel = executionContext.Server.GetChannel<TextChannel>(channelId);
				if (targetChannel == null) {
					throw new CommandException(this, "Invalid channel ID! Input channel was not a text channel.");
				}

				await targetChannel.DeleteMessagesAsync(channelMessage => channelMessage.ID.ToDateTimeOffset() >= messageId.ToDateTimeOffset(), 100, $"Moderator issued purge command: Delete all messages sent after {messageId} ({messageId.GetDisplayTimestampMS()})");
			}
		}
	}
}
