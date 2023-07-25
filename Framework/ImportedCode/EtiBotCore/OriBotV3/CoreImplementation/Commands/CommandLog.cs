using EtiBotCore.Data;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Utility.Marshalling;
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfractionLog = OldOriBot.Data.Persistence.InfractionLogProvider.InfractionLog;
using InfractionLogEntry = OldOriBot.Data.Persistence.InfractionLogProvider.InfractionLog.InfractionLogEntry;
using LogType = OldOriBot.Data.Persistence.InfractionLogProvider.LogType;

namespace OldOriBot.CoreImplementation.Commands {
	public class CommandLog : Command {

		public override string Name { get; } = "log";
		public override string Description { get; } = "Provides methods to read and write persistent logs for all moderation actions.";
		public override ArgumentMapProvider Syntax { get; }
		public override bool IsExclusiveBase { get; } = true;
		public override Command[] Subcommands { get; }
		public override PermissionLevel RequiredPermissionLevel => PermissionLevel.Operator;
		public CommandLog(BotContext ctx) : base(ctx) {
			Subcommands = new Command[] {
				new CommandLogAdd(ctx, this),
				new CommandLogToggle(ctx, this),
				new CommandLogView(ctx, this),
				new CommandLogSearch(ctx, this),
				new CommandLogUpdateTime(ctx, this),
				new CommandLogMarkCompleted(ctx, this),
			};
		}

		public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			throw new NotImplementedException();
		}

		public class CommandLogAdd : Command {
			public override string Name { get; } = "add";
			public override string Description { get; } = @"Manually append an entry to the log. Valid entry types (parameter `entryType`) are:
• minor (for minor warnings)
• warn (for standard warnings)
• major (for major warnings / harsh warnings)
• mute (for mutes, automatically performed)
• unmute (same case as above)
• alt (for authorized alt accounts)
• removealt (for un-authorizing alt accounts)
• note (for notes in general)
• alert (for more severe notes, such as suspected behavior or things to watch out for.)
• ban (for manually logging bans)

The bot will attempt to automatically populate logs when entries are put into the log chat. It will inform you if the action was successful or not.

Generally speaking, you won't need to worry about adding time to the entries unless they are particularly late.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Variant<Snowflake, Person>, string, string, Snowflake, Variant<Snowflake, DateAndTime>>("targetUser", "entryType", "reason", "altId", "atDateTime").SetRequiredState(true, true, true, false, false);
			public override PermissionLevel RequiredPermissionLevel => PermissionLevel.Operator;
			public CommandLogAdd(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length < 3) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(2)}, {Syntax.GetArgName(1)}, and/or {Syntax.GetArgName(0)}"));
				} else if (argArray.Length > 5) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}

				ArgumentMap<Variant<Snowflake, Person>, string, string, Snowflake, Variant<Snowflake, DateAndTime>> args = Syntax.SetContext(executionContext).Parse<Variant<Snowflake, Person>, string, string, Snowflake, Variant<Snowflake, DateAndTime>>(argArray[0], argArray[1], argArray[2], argArray.ElementAtOrDefault(3), argArray.ElementAtOrDefault(4));
				Variant<Snowflake, Person> target = args.Arg1;
				if (target.ArgIndex == 2 && target.Value2.Member == null) throw new CommandException(this, Personality.Get("cmd.err.noMemberFound"));

				Snowflake targetID = target.ArgIndex == 1 ? target.Value1 : target.Value2.Member.ID;
				string argType = args.Arg2.ToLower();
				string reason = args.Arg3;
				Snowflake altId = args.Arg4; // May be an invalid snowflake


				DateTimeOffset? time;
				Variant<Snowflake, DateAndTime> timeContainer = args.Arg5;
				if (timeContainer != null) {
					if (timeContainer.ArgIndex == 1) {
						time = timeContainer.Value1.ToDateTimeOffset();
					} else {
						time = timeContainer.Value2;
					}
				} else {
					time = null;
				}

				if (string.IsNullOrWhiteSpace(reason) && !argType.Contains("alt")) {
					throw new CommandException(this, Personality.Get("cmd.log.err.missingReason"));
				}

				if (argType.Contains("alt") && !altId.IsValid) {
					throw new CommandException(this, Personality.Get("cmd.log.err.missingArgs", Syntax.GetArgName(3)));
				}

				InfractionLogProvider logProvider = InfractionLogProvider.GetProvider(executionContext);

				if (argType == "note" || argType == "noted") {
					logProvider.AppendInfo(executor, targetID, reason, time);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Logged a note to <@!{targetID}> ({targetID})", mentions: AllowedMentions.Reply);
					return;

				} else if (argType == "minor" || argType == "minorwarning") {
					logProvider.AppendMinorWarn(executor, targetID, reason, time);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Logged a minor warning to <@!{targetID}> ({targetID})", mentions: AllowedMentions.Reply);
					return;

				} else if (argType == "warn" || argType == "warned" || argType == "warning") {
					logProvider.AppendWarn(executor, targetID, reason, time);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Logged a warning to <@!{targetID}> ({targetID})", mentions: AllowedMentions.Reply);
					return;

				} else if (argType == "major" || argType == "harsh" || argType == "majorwarning" || argType == "harshwarning") {
					logProvider.AppendMajorWarn(executor, targetID, reason, time);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Logged a major warning to <@!{targetID}> ({targetID})", mentions: AllowedMentions.Reply);
					return;

				} else if (argType == "mute" || argType == "muted") {
					logProvider.AppendMute(executor, targetID, reason, false, time);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Logged a mute to <@!{targetID}> ({targetID})", mentions: AllowedMentions.Reply);
					return;

				} else if (argType == "unmute" || argType == "unmuted") {
					logProvider.AppendUnmute(executor, targetID, reason, false, time);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Logged an unmute to <@!{targetID}> ({targetID})", mentions: AllowedMentions.Reply);
					return;

				} else if (argType == "alt") {
					if (!altId.IsValid) throw new CommandException(this, "The given ID for the alt account is not valid.");
					logProvider.AppendAuthorizeAlt(executor, targetID, altId, reason, time);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Logged the authorization of <@!{altId}> ({altId}) as an alt of <@!{targetID}> ({targetID})", mentions: AllowedMentions.Reply);
					return;

				} else if (argType == "removealt" || argType == "removedalt" || argType == "revokealt" || argType == "revokedalt") {
					if (!altId.IsValid) throw new CommandException(this, "The given ID for the alt account is not valid.");
					logProvider.AppendRevokeAlt(executor, targetID, altId, reason, time);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Logged the de-authorization of <@!{altId}> ({altId}) as an alt of <@!{targetID}> ({targetID})", mentions: AllowedMentions.Reply);
					return;

				} else if (argType == "flag" || argType == "alert") {
					logProvider.AppendAlert(executor, targetID, reason, time);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Logged an alert to <@!{targetID}> ({targetID})", mentions: AllowedMentions.Reply);
					return;
				} else if (argType == "ban") {
					logProvider.AppendBan(executor.ID, targetID, reason, time);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Logged a ban applied to <@!{targetID}> ({targetID})", mentions: AllowedMentions.Reply);
					return;
				}

				throw new CommandException(this, $"Invalid action type provided. Use `>> help {FullName}` to see valid types.");
			}
		}

		public class CommandLogToggle : Command {
			public override string Name { get; } = "toggle";
			public override string Description { get; } = "Removes or restores a log entry. This should never be done unless the entry is an error. For security, the log entry will never be removed from data, simply flagged as \"unused\" which will hide it from lists. Using this command on an entry that is already flagged as such will restore it to visible status.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Variant<Snowflake, Person>, int, string>("targetUser", "entryNumber", "reason").SetRequiredState(true, true, true);
			public override PermissionLevel RequiredPermissionLevel => PermissionLevel.Operator;
			public CommandLogToggle(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length < 3) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(2)}, {Syntax.GetArgName(1)}, and/or {Syntax.GetArgName(0)}"));
				} else if (argArray.Length > 3) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}
				InfractionLogProvider logProvider = InfractionLogProvider.GetProvider(executionContext);

				ArgumentMap<Variant<Snowflake, Person>, int, string> args = Syntax.SetContext(executionContext).Parse<Variant<Snowflake, Person>, int, string>(argArray[0], argArray[1], argArray[2]);
				Variant<Snowflake, Person> target = args.Arg1;
				if (target.ArgIndex == 2 && target.Value2.Member == null) throw new CommandException(this, Personality.Get("cmd.err.noMemberFound"));

				InfractionLog log;
				if (target.ArgIndex == 1) {
					log = logProvider.For(target.Value1);
				} else {
					log = logProvider.For(target.Value2.Member);
				}

				if (!log.HasEntry(args.Arg2)) {
					throw new CommandException(this, Personality.Get("cmd.log.err.invalidEntry", args.Arg2));
				}

				InfractionLogEntry entry = log.GetEntry(args.Arg2);
				if (InfractionLogEntry.IsLogTypeAlwaysHidden(entry.Type)) {
					throw new CommandException(this, Personality.Get("cmd.log.err.typeIsAlwaysHidden", entry.Type));
				}

				if (entry.Deleted) {
					log.SetDeleted(entry, false);
					log.AddEntry(executor.ID, DateTimeOffset.UtcNow, LogType.RestoreEntry, args.Arg3, false); // Log the restoration.
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.log.success.entryRestored", args.Arg2));
				} else {
					log.SetDeleted(entry, true);
					log.AddEntry(executor.ID, DateTimeOffset.UtcNow, LogType.RemoveEntry, args.Arg3, false); // Log the deletion
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.log.success.entryHidden", args.Arg2));
				}
			}
		}
		
		public class CommandLogView : Command {
			public override string Name { get; } = "view";
			public override string Description { get; } = "Views all logged offenses from the given user, or a specific offense if a number is given.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Variant<Snowflake, Person>, Variant<bool, int>>("targetUser", "entryNumberOrShowHidden").SetRequiredState(true, false);
			public override PermissionLevel RequiredPermissionLevel => PermissionLevel.Operator;
			public CommandLogView(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length < 1) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
				} else if (argArray.Length > 3) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}

				ArgumentMap<Variant<Snowflake, Person>, Variant<bool, int>> args = Syntax.SetContext(executionContext).Parse<Variant<Snowflake, Person>, Variant<bool, int>>(argArray[0], argArray.ElementAtOrDefault(1), argArray.ElementAtOrDefault(2));
				Variant<Snowflake, Person> target = args.Arg1;
				if (target.ArgIndex == 2 && target.Value2.Member == null) throw new CommandException(this, Personality.Get("cmd.err.noMemberFound"));

				Snowflake targetID;
				if (target.ArgIndex == 1) {
					targetID = target.Value1;
				} else {
					targetID = target.Value2.Member.ID;
				}

				InfractionLogProvider logProvider = InfractionLogProvider.GetProvider(executionContext);
				InfractionLog log = logProvider.For(targetID);

				Variant<bool, int> data = args.Arg2 ?? new Variant<bool, int>(false);
				if (data.ArgIndex == 1) {
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, log.ToEmbed(data.Value1), AllowedMentions.Reply);
				} else {
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, log.GetEntry(data.Value2).ToString(log), mentions: AllowedMentions.Reply);
				}
			}
		}

		public class CommandLogSearch : Command {
			public override string Name { get; } = "find";
			public override string Description { get; } = "Searches logs for all users for a given query. The `generalActionType` parameter can be used to narrow down the search. The following terms are valid: `note`, `mute` (which also includes unmutes), `warn` (which includes all three types of warnings), `alt` (which includes both adding and removing authorized alts), and `all` which is the default. **It is generally a better idea to include a query, despite not being required.**";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string, string, Person>("searchQuery", "generalActionType", "moderator").SetRequiredState(true, false, false);
			public override PermissionLevel RequiredPermissionLevel => PermissionLevel.Operator;
			public CommandLogSearch(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length == 0) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
				} else if (argArray.Length > 3) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}

				ArgumentMap<string, string, Person> args = Syntax.SetContext(executionContext).Parse<string, string, Person>(argArray[0], argArray.ElementAtOrDefault(1), argArray.ElementAtOrDefault(2));
				string query = args.Arg1;
				string actionType = (args.Arg2 ?? "all").ToLower();
				Member mod = args.Arg3?.Member;

				if (string.IsNullOrWhiteSpace(query)) throw new CommandException(this, "Query must contain some sort of text.");

				InfractionLogProvider logProvider = InfractionLogProvider.GetProvider(executionContext);
				List<InfractionLog> logs = logProvider.GetAllLogs();

				IEnumerable<InfractionLog> isolatedLogs = logs.Where(log => !log.IsAlt);
				if (mod != null) {
					isolatedLogs = isolatedLogs.Where(log => log.HasEntry(entry => entry.ModeratorID == mod.ID));
				}
				if (actionType != null && actionType != "all") {
					if (actionType == "note") {
						isolatedLogs = isolatedLogs.Where(log => log.HasEntry(entry => entry.Type == LogType.Note));

					} else if (actionType == "warn") {
						isolatedLogs = isolatedLogs.Where(log => log.HasEntry(entry => entry.Type == LogType.MinorWarning || entry.Type == LogType.Warning || entry.Type == LogType.MajorWarning));

					} else if (actionType == "mute") {
						isolatedLogs = isolatedLogs.Where(log => log.HasEntry(entry => entry.Type == LogType.Mute || entry.Type == LogType.Unmute || entry.Type == LogType.ChangeMute));

					} else if (actionType == "alt") {
						isolatedLogs = isolatedLogs.Where(log => log.HasEntry(entry => entry.Type == LogType.AuthorizeAlt || entry.Type == LogType.RevokeAlt));

					} else if (actionType == "ban") {
						isolatedLogs = isolatedLogs.Where(log => log.HasEntry(entry => entry.Type == LogType.Ban || entry.Type == LogType.Pardon));

					} else if (actionType == "edit") {
						isolatedLogs = isolatedLogs.Where(log => log.HasEntry(entry => entry.Type == LogType.RemoveEntry || entry.Type == LogType.RestoreEntry || entry.Type == LogType.ChangeEntryTime));

					} else {
						throw new CommandException(this, "Invalid argument. Expected a type of `note`, `warn`, `mute`, `alt`, `ban`, `edit`, or `all`.");
					}
				}

				// Now that the logs have (ideally) been narrowed down, we can search by the actual string query given.
				isolatedLogs = isolatedLogs.Where(log => log.HasEntry(entry => entry.Information.Contains(query, StringComparison.CurrentCultureIgnoreCase)));

				StringBuilder description = new StringBuilder();
				foreach (InfractionLog log in isolatedLogs) {
					User user = await User.GetOrDownloadUserAsync(log.UserID);
					string display;
					if (user != null) {
						display = $"`[{log.UserID}]` {user?.FullName}";
					} else {
						display = log.UserID.ToString();
					}
					description.AppendLine(display);
				}

				if (description.Length <= 2048) {
					EmbedBuilder embed = new EmbedBuilder {
						Title = "Possible Matches: " + isolatedLogs.Count(),
						Description = description.ToString()
					};
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, embed.Build(), AllowedMentions.Reply);
					return;
				} else {
					EmbedBuilder embed = new EmbedBuilder {
						Title = "Possible Matches: " + isolatedLogs.Count(),
						Description = description.ToString().Substring(0, 2048)
					};
					embed.AddField("Truncated", "This list has been truncated. A text document containing all possibilities will be uploaded now.");
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, embed.Build(), AllowedMentions.Reply);

					long v = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
					FileInfo target = new FileInfo(@$"C:\botTemp\{v}.txt");
					File.WriteAllText(target.FullName, description.ToString());
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "Full contents here:", null, AllowedMentions.Reply, true, false, -1, target);
					await Task.Delay(3000);
					target.Delete();
				}
			}
		}

		public class CommandLogUpdateTime : Command {
			public override string Name { get; } = "changetime";
			public override string Description { get; } = "Sets the time associated with the given log entry.";
			public override string[] Aliases { get; } = {
				"settime",
				"time"
			};
			public override PermissionLevel RequiredPermissionLevel => PermissionLevel.Operator;

			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Variant<Snowflake, Person>, int, string, Variant<Snowflake, DateAndTime>>("targetUser", "logEntryNumber", "reason", "newTime");
			public CommandLogUpdateTime(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length < 4) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(3)}, {Syntax.GetArgName(2)}, {Syntax.GetArgName(1)}, and/or {Syntax.GetArgName(0)}"));
				} else if (argArray.Length > 4) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}

				ArgumentMap<Variant<Snowflake, Person>, int, string, Variant<Snowflake, DateAndTime>> args = Syntax.SetContext(executionContext).Parse<Variant<Snowflake, Person>, int, string, Variant<Snowflake, DateAndTime>>(argArray[0], argArray[1], argArray[2], argArray[3]);

				Variant<Snowflake, Person> target = args.Arg1;
				if (target.ArgIndex == 2 && target.Value2.Member == null) throw new CommandException(this, Personality.Get("cmd.err.noMemberFound"));

				Snowflake targetID;
				if (target.ArgIndex == 1) {
					targetID = target.Value1;
				} else {
					targetID = target.Value2.Member.ID;
				}

				Variant<Snowflake, DateAndTime> timeContainer = args.Arg4;
				DateTimeOffset newTime;
				if (timeContainer.ArgIndex == 1) {
					newTime = timeContainer.Value1.ToDateTimeOffset();
				} else {
					newTime = timeContainer.Value2;
				}

				string reason = args.Arg3;
				if (string.IsNullOrWhiteSpace(reason)) {
					throw new CommandException(this, Personality.Get("cmd.log.err.missingReasonUnconditional"));
				}

				InfractionLogProvider logProvider = InfractionLogProvider.GetProvider(executionContext);
				InfractionLog log = logProvider.For(targetID);

				if (!log.HasEntry(args.Arg2)) {
					throw new CommandException(this, Personality.Get("cmd.log.err.invalidEntry", args.Arg2));
				}

				InfractionLogEntry entry = log.GetEntry(args.Arg2);
				if (InfractionLogEntry.IsLogTypeImmutable(entry.Type)) {
					throw new CommandException(this, Personality.Get("cmd.log.err.typeIsImmutable", entry.Type));
				}

				DateTimeOffset currentTime = entry.Time;

				// First log the time change
				log.SetTime(entry, newTime);
				log.AddEntry(executor.ID, DateTimeOffset.UtcNow, LogType.ChangeEntryTime, reason + $" // Changed entry #{args.Arg2}'s logged time from {currentTime} to {newTime}", false);

				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.log.success.changedTime", args.Arg2, currentTime.ToString(), newTime.ToString()));
			}
		}

		public class CommandLogMarkCompleted : Command {
			public override string Name { get; } = "markcomplete";
			public override string Description { get; } = "Marks a log's complete state, which signifies whether or not it is up to date with the legacy log chat.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Variant<Snowflake, Person>, bool>("user", "isComplete").SetRequiredState(true, true);
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public CommandLogMarkCompleted(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length < 2) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(1)} and/or {Syntax.GetArgName(0)}"));
				} else if (argArray.Length > 2) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}

				ArgumentMap<Variant<Snowflake, Person>, bool> args = Syntax.SetContext(executionContext).Parse<Variant<Snowflake, Person>, bool>(argArray[0], argArray[1]);
				Variant<Snowflake, Person> target = args.Arg1;
				if (target.ArgIndex == 2 && target.Value2.Member == null) throw new CommandException(this, Personality.Get("cmd.err.noMemberFound"));

				Snowflake targetID;
				if (target.ArgIndex == 1) {
					targetID = target.Value1;
				} else {
					targetID = target.Value2.Member.ID;
				}

				InfractionLogProvider provider = InfractionLogProvider.GetProvider(executionContext);
				InfractionLog log = provider.For(targetID);
				log.IsComplete = args.Arg2;

				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"This user's log has been marked as {(args.Arg2 ? "complete" : "incomplete")}");
			}
		}
	}
}
