using EtiBotCore.Client;
using EtiBotCore.Data;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.Data.Commands.ArgData;
using OldOriBot.Data.MemberInformation;
using OldOriBot.Data.Persistence;
using OldOriBot.Interaction;
using OldOriBot.Utility.Responding;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InfractionLog = OldOriBot.Data.Persistence.InfractionLogProvider.InfractionLog;
using InfractionLogEntry = OldOriBot.Data.Persistence.InfractionLogProvider.InfractionLog.InfractionLogEntry;
using LogType = OldOriBot.Data.Persistence.InfractionLogProvider.LogType;

namespace OldOriBot.CoreImplementation.Handlers {
	public class HandlerModLogAssistant : PassiveHandler {
		public override string Name { get; } = "Moderator Log Assistant";
		public override string Description { get; } = "Observes logged actions from moderators and attempts to discern what action was taken, automatically storing it to the permanent log.";
		public HandlerModLogAssistant(BotContext ctx) : base(ctx) { }

		private static readonly Dictionary<LogType, string[]> Phrases = new Dictionary<LogType, string[]> {
			[LogType.Warning] = new string[] {
				$"warned {Constants.REGEX_USER_MENTION}",
				@"warned (\d+)",
				@"warned (.+#\d{4})",
				$"warning given to {Constants.REGEX_USER_MENTION}",
				@"warning given to (\d+)",
				@"warning given to (.+#\d{4})",
			},
		};

		private static readonly string[] IDMatches = {
			Constants.REGEX_USER_MENTION,
			@"\d+",
			@".+#\d{4}"
		};

		static HandlerModLogAssistant() {
			string[] minor = new string[6];
			string[] major = new string[6];
			int i = 0;
			foreach (string str in Phrases[LogType.Warning]) {
				if (i < 3) {
					minor[i] = "minorly " + str;
					major[i] = "majorly " + str;
				} else {
					minor[i] = "minor " + str;
					major[i] = "major " + str;
				}
				i++;
			}
			Phrases[LogType.MinorWarning] = minor;
			Phrases[LogType.MajorWarning] = major;
		}

		public override async Task<bool> ExecuteHandlerAsync(Member executor, BotContext executionContext, Message message) {
			if (executor.GetPermissionLevel() < PermissionData.PermissionLevel.Operator) return false;
			if (message.Channel.ID != 629740939992104970) return false;
			
			string text = message.Content;
			string lowerText = text.ToLower();
			string identity = null;
			int startIndex = 0;
			LogType type = LogType.Invalid;
			foreach (string phrase in Phrases[LogType.MinorWarning]) {
				Match match = Regex.Match(lowerText, phrase);
				if (match.Success) {
					type = LogType.MinorWarning;
					startIndex = match.Index + match.Length;
					identity = match.Groups[3].Value;
					break;
				}
			}
			if (type == LogType.Invalid) {
				foreach (string phrase in Phrases[LogType.MajorWarning]) {
					Match match = Regex.Match(lowerText, phrase);
					if (match.Success) {
						type = LogType.MajorWarning;
						startIndex = match.Index + match.Length;
						identity = match.Groups[1].Value;
						break;
					}
				}
			}
			if (type == LogType.Invalid) {
				foreach (string phrase in Phrases[LogType.Warning]) {
					Match match = Regex.Match(lowerText, phrase);
					if (match.Success) {
						type = LogType.Warning;
						startIndex = match.Index + match.Length;
						identity = match.Groups[1].Value;
						break;
					}
				}
			}

			if (type != LogType.Invalid) {
				text = text[startIndex..];
				while (text.StartsWith(" ")) text = text[1..];
				lowerText = text.ToLower();

				foreach (string idm in IDMatches) {
					Match match = Regex.Match(lowerText, idm);
					if (match.Success && match.Index == 0) {
						text = text[match.Length..];
						break;
					}
				}
				while (text.StartsWith(" ")) {
					text = text[1..];
				}
				lowerText = text.ToLower();

				if (lowerText.StartsWith("for")) {
					text = text[3..];
					lowerText = text.ToLower();
				}

				if (lowerText.StartsWith("because")) {
					text = text[7..];
					lowerText = text.ToLower();
				}

				if (lowerText.StartsWith("when")) text = text[4..];
				// lowerText = text.ToLower();

				// Technically this isn't meant to be used here but I'll do it anyway since it fits the exact purpose.
				Person target = new Person().From(identity, executionContext);
				//HandlerLogger.WriteLine(identity, EtiLogger.Logging.LogLevel.Debug);
				//HandlerLogger.WriteLine($"Debug: New log entry of type {type} would registered to {target.Member?.ToString() ?? "null"}", EtiLogger.Logging.LogLevel.Debug);
				if (target.Member == null) {
					await ResponseUtil.RespondToAsync(message, HandlerLogger, "I was unable to determine who you are talking about. Please manually log this with the `>> log` command.\n\nThis message will delete itself shortly.", mentions: AllowedMentions.Reply, deleteAfterMS: 7500);
					return false;
				}

				await ResponseUtil.RespondToAsync(message, HandlerLogger, $"Debug: New log entry of type {type} has been registered to <@!{target.Member.ID}> ({target.Member.FullName}): {text}", mentions: AllowedMentions.Reply, deleteAfterMS: 7500);
				InfractionLogProvider forServer = InfractionLogProvider.GetProvider(executionContext);

				if (type == LogType.MinorWarning) {
					forServer.AppendMinorWarn(executor, target.Member.ID, text);
				} else if (type == LogType.Warning) {
					forServer.AppendWarn(executor, target.Member.ID, text);
				} else if (type == LogType.MajorWarning) {
					forServer.AppendMajorWarn(executor, target.Member.ID, text);
				} else if (type == LogType.AuthorizeAlt) {
					// Not ready
				} else if (type == LogType.RevokeAlt) {
					// Not ready
				}
			} else {
				await ResponseUtil.RespondToAsync(message, HandlerLogger, "I was unable to determine what type of log entry this should have been. Please manually log this with the `>> log` command.\n\nThis message will delete itself shortly.", mentions: AllowedMentions.Reply, deleteAfterMS: 7500);
			}

			return false;
		}
	}
}
