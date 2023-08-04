using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Exceptions;
using EtiBotCore.Exceptions.Marshalling;
using EtiBotCore.Utility.Extension;
using EtiLogger.Data.Structs;
using EtiLogger.Logging;
using OldOriBot.Data.Commands.Default;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.Interaction.CommandData;
using OldOriBot.PermissionData;
using OldOriBot.Utility.Extensions;
using OldOriBot.Utility.Formatting;
using OldOriBot.Utility.Responding;

namespace OldOriBot.Data.Commands {
	public static class CommandMarshaller {

		/// <summary>
		/// The amount of people listed when a <see cref="NonSingularPersonException"/> is thrown.
		/// </summary>
		public const int NUMBER_OF_PEOPLE_IN_NONSINGULAR_LIST = 20;

		/// <summary>
		/// The delay time after the user's command executes that they must wait before their next command is allowed to run.
		/// </summary>
		public const int EXTRA_COMMAND_LATENCY_MS = 1250;

		/// <summary>
		/// If a command is longer than this, it will error out.
		/// </summary>
		public const int MAX_COMMAND_LENGTH = 32;

		/// <summary>
		/// Whether or not the permission requirements of commands and their subcommands have been verified, which prevents subcommands from having lower requirements than their parent.
		/// </summary>
		private static bool HasVerifiedCommands = false;

		public static readonly Logger CommandLogger = new Logger(
			new LogMessage.MessageComponent("[Command Marshaller] ", Color.DARK_MAGENTA)
		);

		public static bool Ready { get; set; } = false;

		/// <summary>
		/// The <see cref="BotContext"/> commands will run in.
		/// </summary>
		public static BotContext TargetContext { get; internal set; } = null;

		/// <summary>
		/// The commands that are common across all servers.
		/// </summary>
		public static readonly IReadOnlyList<Command> DefaultCommands;

		/// <summary>
		/// Keeps track of the last time that someone was warned to stop using commands because their previous command was still running.
		/// </summary>
		private static Dictionary<Snowflake, long> LastWarnedForRateLimitAt = new Dictionary<Snowflake, long>();

		static CommandMarshaller() {
			CommandPerms permsCmd = new CommandPerms();
			List<Command> cmds = new List<Command> {
				new CommandHelp(),
				new CommandConfig(),
				new CommandTypeInfo(),
				new CommandDumpSnowflake(),
				new CommandWhoIs(),
				permsCmd,
				new CommandHandler(),
				new CommandShutdown(),
				new CommandSetContext(),
				new CommandInvokeCore(),
				new CommandTicket()
			};
			cmds.Add(new CommandSetPermsObsolete((CommandPerms.CommandSetPerms)permsCmd.Subcommands.First(cmd => cmd is CommandPerms.CommandSetPerms)));
			DefaultCommands = cmds;
		}

		internal static readonly Dictionary<Snowflake, ProgressiveCommand.Tracker> MembersInProgressiveCommands = new Dictionary<Snowflake, ProgressiveCommand.Tracker>();
		internal static readonly Dictionary<Snowflake, bool> MembersExecutingCommands = new Dictionary<Snowflake, bool>();

		private static bool IsCommand(string msg) {
			if (msg.StartsWith(">>>")) return false;
			if (msg.StartsWith(">>")) {
				if (msg.StartsWith(">> ")) return msg.Length > 3;
				return msg.Length > 2;
			}
			return false;
		}

		private static string StripPrefix(string msg) {
			if (msg.StartsWith(">>")) {
				return msg[2..];
			}
			return msg;
		}

		/// <summary>
		/// Finds the command that matches the given string (this string is assumed to be like a chat command), or null if one could not be found.
		/// </summary>
		/// <param name="rawMsg"></param>
		/// <returns></returns>
		public static Command GetCommandFromAnywhere(string rawMsg) {
			string cmdStr = StripPrefix(rawMsg).Trim();
			string[] args = cmdStr.SplitArgs();
			string command = args[0];

			// First search contexts
			foreach (BotContext ctx in BotContextRegistry.GetContexts()) {
				if (ctx.Commands != null) {
					foreach (Command cmd in ctx.Commands) {
						if (cmd.NameRunsCommand(command)) {
							return cmd;
						}
					}
				}
			}

			// Then defaults
			foreach (Command cmd in DefaultCommands) {
				if (cmd.NameRunsCommand(command)) {
					return cmd;
				}
			}
			return null;
		}

		private static void VerifyCommands() {
			if (HasVerifiedCommands) {
				return;
			}
			HasVerifiedCommands = true;

			foreach (Command cmd in DefaultCommands) {
				VerifySubs(cmd, cmd.RequiredPermissionLevel);
			}

			foreach (BotContext ctx in BotContextRegistry.GetContexts()) {
				foreach (Command cmd in ctx.Commands) {
					VerifySubs(cmd, cmd.RequiredPermissionLevel);
				}
			}

			CommandLogger.WriteLine("Finished verifying command permission level hierarchy.", LogLevel.Debug);
		}

		private static void VerifySubs(Command parent, PermissionLevel effectiveHighestPerm) {
			if (parent.Subcommands != null && parent.Subcommands.Length > 0) {
				foreach (Command sub in parent.Subcommands) {
					if (parent.RequiredPermissionLevel > effectiveHighestPerm) {
						effectiveHighestPerm = parent.RequiredPermissionLevel;
					}
					if (sub.RequiredPermissionLevel < effectiveHighestPerm) {
						CommandLogger.WriteCritical($"Command {sub.GetType().FullName} had a lower permission level requirement than its parent, {parent.GetType().FullName}! This must be resolved immediately.");
						VerifySubs(sub, effectiveHighestPerm);
					} else {
						VerifySubs(sub, sub.RequiredPermissionLevel);
					}
				}
			}
		}

		public static Command GetCommand(BotContext ctx, string command, bool alsoGlobals = true) {
			// First search contexts
			if (ctx?.Commands != null) {
				foreach (Command cmd in ctx.Commands) {
					if (cmd.NameRunsCommand(command)) {
						return cmd;
					}
				}
			}

			// Then defaults
			if (alsoGlobals)
				foreach (Command cmd in DefaultCommands) {
					if (cmd.NameRunsCommand(command)) {
						return cmd;
					}
				}
			return null;
		}

		/// <summary>
		/// Given a DM message, this will attempt to parse it as a command.
		/// </summary>
		/// <returns></returns>
		public static async Task ParseDMCommand(Message message) {
			if (!Ready) return;

			User author = message.Author;
			if (author.IsSelf) return;
			if (author.IsABot) return;
			if (author.IsDiscordSystem) return;
			if (!HasVerifiedCommands) VerifyCommands();

			string text = message.Content;
			if (text == null) {
				// await message.ReplyAsync(Personality.Get("cmd.err.noContent"), mentionLimits: AllowedMentions.Reply);
				// MembersExecutingCommands[author.ID] = false;
				return;
			}
			bool isCmd = IsCommand(text);

			BotContext target = BotContextRegistry.GetContext(577548441878790146); // A bit of a stupid trick but this will work for now.

			if (isCmd) {
				if (MembersExecutingCommands.ContainsKey(author.ID) && MembersExecutingCommands[author.ID] == true) {
					if (MembersExecutingCommands[author.ID] == true) {
						if (LastWarnedForRateLimitAt.TryGetValue(author.ID, out long epoch)) {
							// Epoch will be in the past.
							// say now is 1000 and epoch is 999
							// 1000-999 is 1, return
							if ((DateTimeOffset.UtcNow.ToUnixTimeSeconds() - epoch) < 2) {
								CommandLogger.WriteLine($"§8User §6{author.FullName}§8 (§3{author.ID}§8) was rate limited from using a command because their previous command was still executing.", LogLevel.Debug);
								return;
							}
						}
						LastWarnedForRateLimitAt[author.ID] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
						await message.ReplyAsync(Personality.Get("cmd.err.stillExec"), mentionLimits: AllowedMentions.Reply);
						return;
					}
				}
				MembersExecutingCommands[author.ID] = true;

				string cmdStr = StripPrefix(text).Trim();

				string[] args = cmdStr.SplitArgs();
				string command = args[0];
				args = args.Skip(1).ToArray();
				cmdStr = cmdStr[command.Length..];

				if (command.Length > MAX_COMMAND_LENGTH) {
					CommandLogger.WriteLine($"§8User §6{author.FullName}§8 (§3{author.ID}§8) attempted to use a command that was too long.", LogLevel.Debug);
					await message.ReplyAsync(Personality.Get("cmd.err.tooLong"), mentionLimits: AllowedMentions.Reply);
					await Task.Delay(EXTRA_COMMAND_LATENCY_MS);
					MembersExecutingCommands[author.ID] = false;
					return;
				}

				foreach (Command cmd in DefaultCommands) {
					if (cmd.NameRunsCommand(command)) {
						CommandLogger.WriteLine($"§8User §6{author.FullName}§8 (§3{author.ID}§8) is trying to issue command: §2{command} {cmdStr}", LogLevel.Debug);
						await RunOrRunSubcommand(target, message, cmd, args, cmdStr);
						await Task.Delay(EXTRA_COMMAND_LATENCY_MS);
						MembersExecutingCommands[author.ID] = false;
						return;
					}
				}

				CommandLogger.WriteLine($"§8User §6{author.FullName}§8 (§3{author.ID}§8) attempted to issue a non-existent command: §2{command} {cmdStr}", LogLevel.Debug);
				await message.ReplyAsync(Personality.Get("cmd.err.noCmd", command), mentionLimits: AllowedMentions.Reply);
				await Task.Delay(EXTRA_COMMAND_LATENCY_MS);
				MembersExecutingCommands[author.ID] = false;
			}
		}

		/// <summary>
		/// Given a server message, this will attempt to parse it.
		/// </summary>
		/// <param name="chatMessage"></param>
		/// <param name="ctx"></param>
		/// <returns></returns>
		public static async Task ParseCommand(Message chatMessage, BotContext ctx) {
			if (!Ready) return;

			Member executor = chatMessage.AuthorMember;
			if (executor == null) return;
			if (executor.IsSelf) return;
			if (executor.IsABot) return;
			if (executor.IsDiscordSystem) return;

			string text = chatMessage.Content;
			if (text == null) return;
			bool isCmd = IsCommand(text);

			if (!HasVerifiedCommands) VerifyCommands();

			if (ctx.Handlers != null) {
				foreach (PassiveHandler handler in ctx.Handlers) {
					if (!isCmd || handler.RunOnCommands) {
						try {
							if (await handler.ExecuteHandlerAsync(executor, ctx, chatMessage)) return;
						} catch (Exception exc) {
							await PostExceptionEmbedForHandler(handler, exc);
						}
					}
				}
			}

			if (isCmd) {
				if (MembersInProgressiveCommands.ContainsKey(executor.ID)) {
					if (!MembersInProgressiveCommands[executor.ID].Terminated) {
						await chatMessage.ReplyAsync(Personality.Get("cmd.err.inProgressive"), mentionLimits: AllowedMentions.Reply);
						return;
					} else {
						MembersInProgressiveCommands.Remove(executor.ID);
					}
				}
				if (MembersExecutingCommands.ContainsKey(executor.ID) && MembersExecutingCommands[executor.ID] == true) {
					if (LastWarnedForRateLimitAt.TryGetValue(executor.ID, out long epoch)) {
						// Epoch will be in the past.
						// say now is 1000 and epoch is 999
						// 1000-999 is 1, return
						if ((DateTimeOffset.UtcNow.ToUnixTimeSeconds() - epoch) < 2) {
							CommandLogger.WriteLine($"§8User §6{executor.FullName}§8 (§3{executor.ID}§8) was rate limited from using a command because their previous command was still executing.", LogLevel.Debug);
							return;
						}
					}
					LastWarnedForRateLimitAt[executor.ID] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
					await chatMessage.ReplyAsync(Personality.Get("cmd.err.stillExec"), mentionLimits: AllowedMentions.Reply);
					return;
				}
				MembersExecutingCommands[executor.ID] = true;

				string cmdStr = StripPrefix(text).Trim();

				string[] args = cmdStr.SplitArgs();
				string command = args[0];
				args = args.Skip(1).ToArray();
				cmdStr = cmdStr[command.Length..];

				if (command.Length > MAX_COMMAND_LENGTH) {
					CommandLogger.WriteLine($"§8User §6{executor.FullName}§8 (§3{executor.ID}§8) attempted to use a command that was too long.", LogLevel.Debug);
					await chatMessage.ReplyAsync(Personality.Get("cmd.err.tooLong"), mentionLimits: AllowedMentions.Reply);
					await Task.Delay(EXTRA_COMMAND_LATENCY_MS);
					MembersExecutingCommands[executor.ID] = false;
					return;
				}

				// First search contexts
				if (ctx != null && ctx.Commands != null) {
					foreach (Command cmd in ctx.Commands) {
						if (cmd.IsDMOnly) continue;
						if (cmd.NameRunsCommand(command)) {
							CommandLogger.WriteLine($"§8User §6{executor.FullName}§8 (§3{executor.ID}§8) is trying to issue command: §2{command} {cmdStr}", LogLevel.Debug);
							await RunOrRunSubcommand(ctx, chatMessage, cmd, args, cmdStr);
							await Task.Delay(EXTRA_COMMAND_LATENCY_MS);
							MembersExecutingCommands[executor.ID] = false;
							return;
						}
					}
				}


				// Then defaults
				foreach (Command cmd in DefaultCommands) {
					if (cmd.IsDMOnly) continue;
					if (cmd.NameRunsCommand(command)) {
						CommandLogger.WriteLine($"§8User §6{executor.FullName}§8 (§3{executor.ID}§8) is trying to issue command: §2{command} {cmdStr}", LogLevel.Debug);
						await RunOrRunSubcommand(ctx, chatMessage, cmd, args, cmdStr);
						await Task.Delay(EXTRA_COMMAND_LATENCY_MS);
						MembersExecutingCommands[executor.ID] = false;
						return;
					}
				}

				CommandLogger.WriteLine($"§8User §6{executor.FullName}§8 (§3{executor.ID}§8) attempted to issue a non-existent command: §2{command} {cmdStr}", LogLevel.Debug);
				await chatMessage.ReplyAsync(Personality.Get("cmd.err.noCmd", command), mentionLimits: AllowedMentions.Reply);
				await Task.Delay(EXTRA_COMMAND_LATENCY_MS);
				MembersExecutingCommands[executor.ID] = false;
			}

			if (MembersInProgressiveCommands.ContainsKey(executor.ID) && chatMessage.Channel == MembersInProgressiveCommands[executor.ID].Channel) {
				await MembersInProgressiveCommands[executor.ID].ExecuteCurrentTask(chatMessage);
			}
		}

		private static async Task RunOrRunSubcommand(BotContext ctx, Message chatMessage, Command cmd, string[] args, string cmdStr) {
			Snowflake? targetChannel = chatMessage?.Channel?.ID;
			Member authorMember = await chatMessage?.Author.InServerAsync(ctx.Server);
			if (cmd.GetUseInChannel(ctx, authorMember, targetChannel) != targetChannel && targetChannel != null) {
				CommandLogger.WriteLine($"§8User §6{chatMessage.Author.FullName}§8 (§3{chatMessage.Author.ID}§8) was redirected to the bot channel, so command execution did not complete.", LogLevel.Debug);
				await ResponseUtil.RespondToAsync(chatMessage, CommandLogger, "We should go to <#" + ctx.BotChannelID.Value + "> to use this.", null, AllowedMentions.Reply, deleteAfterMS: 5000);
				return;
			}
			CommandUsagePacket usage = cmd.CanRunCommand(authorMember);
			if (usage.CanUse) {
				if (cmd.Subcommands != null && args.Length > 0) {
					Command sub = cmd.Subcommands.Where(cmd => cmd.NameRunsCommand(args[0])).FirstOrDefault();
					if (sub != null) {
						await RunOrRunSubcommand(ctx, chatMessage, sub, args.Skip(1).ToArray(), cmdStr);
						return;
					}
				}
				if (cmd.Syntax?.ArgCount == 0 && args.Length > 0) {
					// No args for the command, but arg length is greater than 0.
					CommandLogger.WriteLine($"§8User §6{chatMessage.Author.FullName}§8 (§3{chatMessage.Author.ID}§8) issued a subcommand that does not exist.", LogLevel.Debug);
					await chatMessage.ReplyAsync(Personality.Get("cmd.err.noSubCmd", cmd.FullName, args[0]), mentionLimits: AllowedMentions.Reply);
					return;
				}
				try {
					if (cmd.IsExclusiveBase) {
						CommandLogger.WriteLine($"§8User §6{chatMessage.Author.FullName}§8 (§3{chatMessage.Author.ID}§8) successfully issued the command, but it is an exclusive base, so they got the help menu for it.", LogLevel.Debug);
						Embed helpMenu = CommandHelp.PostSpecificHelpMenu(authorMember, cmd, false);
						await chatMessage.ReplyAsync(null, helpMenu, AllowedMentions.Reply);
					} else {
						//if (cmd is ProgressiveCommand prog) {
						//	ProgressiveCommand.Tracker tracker = await prog.BeginExecutionAsync(chatMessage.AuthorMember, ctx, chatMessage);
						//	MembersInProgressiveCommands[chatMessage.Author.ID] = tracker;
						//} else {
							await cmd.ExecuteCommandAsync(authorMember, ctx, chatMessage, args, cmdStr, false);
						//}
					}
				} catch (Exception exc) {
					CommandLogger.WriteLine($"§8User §6{chatMessage.Author.FullName}§8 (§3{chatMessage.Author.ID}§8) failed to execute a command due to an exception being thrown, which will be logged unless told not to by the command.", LogLevel.Debug);
					await PostEmbedForFailure(chatMessage, exc);
					if (!(exc.InnerException is NoThrowDummyException) && !(exc is CommandException) && !(exc is NonSingularPersonException)) {
						CommandLogger.WriteException(exc);
					}
				}
			} else {
				CommandLogger.WriteLine($"§8User §6{chatMessage.Author.FullName}§8 (§3{chatMessage.Author.ID}§8) is not authorized to use this command.", LogLevel.Debug);
				EmbedBuilder builder = new EmbedBuilder {
					Title = Personality.Get("cmd.notAllowed.base"),
					Description = usage.Reason,
					Color = Color.DARK_RED
				};
				await chatMessage.ReplyAsync(null, builder.Build(), AllowedMentions.Reply);
				//await chatMessage.ReplyAsync(Personality.Get("cmd.notAllowed.prefix", usage.Reason), mentionLimits: AllowedMentions.Reply);
			}
		}

		private static async Task RunOrRunSubcommandConsole(Command cmd, BotContext executionContext, Member botMember, string[] args, string cmdStr) {
			if (cmd.Subcommands != null && args.Length > 0) {
				Command sub = cmd.Subcommands.Where(cmd => cmd.NameRunsCommand(args[0])).FirstOrDefault();
				if (sub != null) {
					await RunOrRunSubcommandConsole(sub, executionContext, botMember, args.Skip(1).ToArray(), cmdStr);
					return;
				}
			}
			if (cmd.Syntax?.ArgCount == 0 && args.Length > 0) {
				// No args for the command, but arg length is greater than 0.
				CommandLogger.WriteLine(Personality.Get("cmd.err.noSubCmd", cmd.FullName, args[0]));
				return;
			}
			try {
				if (cmd.IsExclusiveBase) {
					Embed helpMenu = CommandHelp.PostSpecificHelpMenu(botMember, cmd, true);
					await ResponseUtil.RespondToAsync(null, CommandLogger, null, helpMenu);
				} else {
					if (!cmd.NoConsole) {
						if ((cmd.RequiresContext && executionContext != null) || !cmd.RequiresContext) {
							await cmd.ExecuteCommandAsync(botMember, executionContext, null, args, cmdStr, true);
						} else {
							throw new CommandException(cmd, Personality.Get("cmd.err.noContext"));
						}
					} else {
						throw new CommandException(cmd, Personality.Get("cmd.err.noConsole"));
					}
				}
			} catch (CommandException cmdErr) {
				CommandLogger.WriteLine(Personality.Get("cmd.err.prefix", cmd.FullName, cmdErr.Message));
			} catch (NonSingularPersonException multiTarget) {
				StringBuilder list = new StringBuilder(Personality.Get("err.multiUser.listStart", NUMBER_OF_PEOPLE_IN_NONSINGULAR_LIST, multiTarget.Candidates.Count));
				for (int idx = 0; idx < NUMBER_OF_PEOPLE_IN_NONSINGULAR_LIST; idx++) {
					Member mbr = multiTarget.Candidates[idx];
					list.AppendLine($"`{mbr.ID}` {mbr.FullNickname.EscapeAllDiscordMarkdown()}");
				}
				CommandLogger.WriteLine(Personality.Get("cmd.err.prefix", cmd.FullName, multiTarget.Message + list.ToString()));
			} catch (Exception exc) {
				CommandLogger.WriteException(exc);
			}
		}

		public static async Task ParseConsole(string text) {
			string cmdStr = StripPrefix(text).Trim();
			string[] args = cmdStr.SplitArgs();
			string command = args[0];
			args = args.Skip(1).ToArray();
			cmdStr = cmdStr[command.Length..];

			Member botMember = TargetContext?.Server?.BotMember;

			// Then defaults
			foreach (Command cmd in DefaultCommands) {
				if (cmd.Name == command) {
					await RunOrRunSubcommandConsole(cmd, TargetContext, botMember, args, cmdStr);
					return;
				}
			}

			if (TargetContext != null) {
				foreach (Command cmd in TargetContext!.Commands) {
					if (cmd.Name == command) {
						await RunOrRunSubcommandConsole(cmd, TargetContext, botMember, args, cmdStr);
						return;
					}
				}
			}

			CommandLogger.WriteLine("§cUnknown command.");
		}

		private static async Task PostEmbedForFailure(Message original, Exception exc) {
			if (exc is CommandException cmdExc) {
				EmbedBuilder builder = new EmbedBuilder {
					Title = Personality.Get("cmd.err.prefixNoReason", cmdExc.Cause.FullName),
					Description = cmdExc.Message,
					Color = Color.DARK_RED
				};
				builder.SetFooter($"You can use `>> help {cmdExc.Cause.FullName}` to get more information on how to use this command.", new Uri(Images.INFORMATION));
				await original.ReplyAsync(null, builder.Build(), AllowedMentions.Reply, null, true);

			} else if (exc is NonSingularPersonException multiTarget) {
				int min = Math.Min(NUMBER_OF_PEOPLE_IN_NONSINGULAR_LIST, multiTarget.Candidates.Count);
				StringBuilder list = new StringBuilder(Personality.Get("err.multiUser.listStart", min, multiTarget.Candidates.Count));
				for (int idx = 0; idx < min; idx++) {
					Member mbr = multiTarget.Candidates[idx];
					list.AppendLine($"`{mbr.ID}` {mbr.FullNickname.EscapeAllDiscordMarkdown()}");
				}
				EmbedBuilder builder = new EmbedBuilder {
					Title = multiTarget.Message,
					Description = list.ToString(),
					Color = Color.DARK_RED
				};
				await original.ReplyAsync(null, builder.Build(), AllowedMentions.Reply, null, true);
			} else if (exc is InsufficientPermissionException perms) {
				EmbedBuilder builder = new EmbedBuilder {
					Title = Personality.Get("cmd.err.exception", exc.GetType().Name),
					Description = exc.Message,
					Color = Color.DARK_RED
				};
				if (perms.PermissionsAsString != null) {
					builder.AddField("Required Permissions", perms.PermissionsAsString);
				}
				await original.ReplyAsync(null, builder.Build(), AllowedMentions.Reply, null, true);
			} else {
				EmbedBuilder builder = new EmbedBuilder {
					Title = Personality.Get("cmd.err.exception", exc.GetType().Name),
					Description = exc.Message,
					Color = Color.DARK_RED
				};
				await original.ReplyAsync(null, builder.Build(), AllowedMentions.Reply, null, true);
			}
		}

		public static async Task PostExceptionEmbedInChannel(TextChannel channel, Exception exc) {
			if (exc is CommandException cmdExc) {
				EmbedBuilder builder = new EmbedBuilder {
					Title = Personality.Get("cmd.err.prefixNoReason", cmdExc.Cause.FullName.EscapeAllDiscordMarkdown()),
					Description = cmdExc.Message.EscapeAllDiscordMarkdown(),
					Color = Color.DARK_RED
				};
				builder.SetFooter($"You can use `>> help {cmdExc.Cause.FullName}` to get more information on how to use this command.", new Uri(Images.INFORMATION));
				await channel.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);

			} else if (exc is NonSingularPersonException multiTarget) {
				int min = Math.Min(NUMBER_OF_PEOPLE_IN_NONSINGULAR_LIST, multiTarget.Candidates.Count);
				StringBuilder list = new StringBuilder(Personality.Get("err.multiUser.listStart", min, multiTarget.Candidates.Count));
				for (int idx = 0; idx < min; idx++) {
					Member mbr = multiTarget.Candidates[idx];
					list.AppendLine($"`{mbr.ID}` {mbr.FullNickname.EscapeAllDiscordMarkdown()}");
				}
				EmbedBuilder builder = new EmbedBuilder {
					Title = multiTarget.Message.EscapeAllDiscordMarkdown(),
					Description = list.ToString(),
					Color = Color.DARK_RED
				};
				await channel.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);

			} else if (exc is InsufficientPermissionException perms) {
				EmbedBuilder builder = new EmbedBuilder {
					Title = Personality.Get("cmd.err.exception", exc.GetType().Name),
					Description = exc.Message.EscapeAllDiscordMarkdown(),
					Color = Color.DARK_RED
				};
				if (perms.PermissionsAsString != null) {
					builder.AddField("Required Permissions", perms.PermissionsAsString);
				}
				await channel.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);
			} else if (exc is WebSocketException webErr) {
				WebSocketErroredException newErr = WebSocketErroredException.Wrap(webErr);
				EmbedBuilder builder = new EmbedBuilder {
					Title = Personality.Get("cmd.err.exception", exc.GetType().Name),
					Description = newErr.Message.EscapeAllDiscordMarkdown(),
					Color = Color.DARK_RED
				};
				await channel.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);
			} else {
				EmbedBuilder builder = new EmbedBuilder {
					Title = Personality.Get("cmd.err.exception", exc.GetType().Name),
					Description = exc.Message.EscapeAllDiscordMarkdown(),
					Color = Color.DARK_RED
				};
				await channel.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);

			}
		}

		public static async Task PostExceptionEmbedForHandler(PassiveHandler handler, Exception exc) {
			TextChannel channel = handler.Context.EventLog;
			if (exc is CommandException cmdExc) {
				EmbedBuilder builder = new EmbedBuilder {
					Title = Personality.Get("cmd.err.prefixNoReason", cmdExc.Cause.FullName.EscapeAllDiscordMarkdown()),
					Description = cmdExc.Message.EscapeAllDiscordMarkdown(),
					Color = Color.DARK_RED
				};
				builder.SetFooter($"This exception was thrown by {handler.GetType().FullName} ({handler.Name})\n\nYou can use `>> help {cmdExc.Cause.FullName}` to get more information on how to use this command.", new Uri(Images.INFORMATION));
				await channel.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);

			} else if (exc is NonSingularPersonException multiTarget) {
				int min = Math.Min(NUMBER_OF_PEOPLE_IN_NONSINGULAR_LIST, multiTarget.Candidates.Count);
				StringBuilder list = new StringBuilder(Personality.Get("err.multiUser.listStart", min, multiTarget.Candidates.Count));
				for (int idx = 0; idx < min; idx++) {
					Member mbr = multiTarget.Candidates[idx];
					list.AppendLine($"`{mbr.ID}` {mbr.FullNickname.EscapeAllDiscordMarkdown()}");
				}
				EmbedBuilder builder = new EmbedBuilder {
					Title = multiTarget.Message.EscapeAllDiscordMarkdown(),
					Description = list.ToString(),
					Color = Color.DARK_RED
				};
				builder.SetFooter($"This exception was thrown by {handler.GetType().FullName} ({handler.Name})");
				await channel.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);

			} else if (exc is InsufficientPermissionException perms) {
				EmbedBuilder builder = new EmbedBuilder {
					Title = Personality.Get("cmd.err.exception", exc.GetType().Name),
					Description = exc.Message.EscapeAllDiscordMarkdown(),
					Color = Color.DARK_RED
				};
				if (perms.PermissionsAsString != null) {
					builder.AddField("Required Permissions", perms.PermissionsAsString);
				}
				builder.SetFooter($"This exception was thrown by {handler.GetType().FullName} ({handler.Name})");
				await channel.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);

			} else {
				EmbedBuilder builder = new EmbedBuilder {
					Title = Personality.Get("cmd.err.exception", exc.GetType().Name),
					Description = exc.Message.EscapeAllDiscordMarkdown(),
					Color = Color.DARK_RED
				};
				builder.SetFooter($"This exception was thrown by {handler.GetType().FullName} ({handler.Name})");
				await channel.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);

			}
		}

		public static void Initialize() {
			DiscordClient.Current.Events.MessageEvents.OnMessageCreated += async (message, pinned) => {
				try {
					if (message.Channel is TextChannel txt) {
						BotContext ctx = BotContextRegistry.GetContext(txt.Server.ID);
						await ParseCommand(message, ctx);
					} else if (message.Channel is DMChannel) {
						await ParseDMCommand(message);
					}
				} catch (Exception exc) {
					CommandLogger.WriteException(exc);
				}
			};
		}

	}
}
