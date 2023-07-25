using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Utility.Marshalling;
using EtiLogger.Data.Structs;
using OldOriBot.Data.MemberInformation;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.Interaction.CommandData;
using OldOriBot.PermissionData;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Extensions;
using OldOriBot.Utility.Formatting;
using OldOriBot.Utility.Responding;

namespace OldOriBot.Data.Commands.Default {
	public class CommandHelp : Command {

		public const string CHECK = "✅";

		public const string DENY = "❌";

		public override string Name { get; } = "help";
		public override string Description { get; } = "Acquire a list of commands, or get help with the syntax of a given command.";
		public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Variant<int, string>, string[]>("pageOrCommand", "subCommands").SetRequiredState(false, false);
		public override string[] Aliases { get; } = {
			"commands",
			"cmds",
			"?"
		};
		public override Snowflake? GetUseInChannel(BotContext executionContext, Member member, Snowflake? channelUsedIn) {
			Snowflake? ch = base.GetUseInChannel(executionContext, member, channelUsedIn);
			if (ch != channelUsedIn) {
				if (channelUsedIn == 625489171095748629) return channelUsedIn;
			}
			return ch;
		}

		public CommandHelp() : base(null) { }

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			string[] arg = null;
			if (argArray.Length > 1) arg = argArray.Skip(1).ToArray();
			ArgumentMap<Variant<int, string>, string[]> args = Syntax.Parse<Variant<int, string>, string[]>(argArray.ElementAtOrDefault(0), arg);

			Variant<int, string> cmdOrPage = args.Arg1;
			string[] subChain = args.Arg2 ?? new string[0];
			if (cmdOrPage != null && cmdOrPage.ArgIndex == 2) {
				Command cmd = CommandMarshaller.GetCommand(executionContext, cmdOrPage.Value2);
				if (cmd == null) {
					throw new CommandException(this, Personality.Get("cmd.err.noCmd", cmdOrPage.Value2.Replace("`", "'")));
				}
				for (int idx = 0; idx < subChain.Length; idx++) {
					if (cmd.Subcommands != null) {
						Command sub = null;
						foreach (Command c in cmd.Subcommands) {
							if (c.Name == subChain[idx]) {
								sub = c;
								break;
							}
						}
						if (sub != null) {
							cmd = sub;
							continue;
						}
					}
					throw new CommandException(this, Personality.Get("cmd.err.noSubCmd", cmdOrPage.Value2.Replace("`", "'"), subChain[idx]));
				}
				Embed embed = PostSpecificHelpMenu(executor, cmd, isConsole);
				//await originalMessage.ReplyAsync(null, embed, AllowedMentions.Reply);
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, embed, AllowedMentions.Reply);
			} else {
				Embed embed = PostGenericHelpMenu(executor, executionContext, cmdOrPage?.Value1 ?? 0, isConsole);
				//await originalMessage.ReplyAsync(null, embed, AllowedMentions.Reply);
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, embed, AllowedMentions.Reply);
			}
		}

		private void ConstructCommandList(Member executor, DiffBuilder builder, IEnumerable<Command> commands, bool isConsole, out bool showedCommand) {
			showedCommand = false;
			foreach (Command cmd in commands) {
				if (cmd is DeprecatedCommand) continue;
				if (isConsole) {
					showedCommand = true;
					builder.Added(cmd.Name);
					continue;
				}
				if (cmd.Visibility == CommandVisibilityType.Never) continue;

				bool canUse = cmd.CanRunCommand(executor).CanUse;
				if (!canUse && cmd.Visibility == CommandVisibilityType.OnlyIfUsable) continue;

				if (canUse) {
					builder.Added(cmd.Name);
				} else {
					builder.Removed(cmd.Name);
				}
				showedCommand = true;
			}
		}

		/// <summary>
		/// Posts the list of commands.
		/// </summary>
		/// <returns></returns>
		private Embed PostGenericHelpMenu(Member executor, BotContext executionContext, int _, bool isConsole) {
			DiffBuilder builder = new DiffBuilder();

			EmbedBuilder embed = new EmbedBuilder {
				Title = Personality.Get("cmd.help.title"),
				Link = new Uri("https://etithespir.it/discord/commands.xhtml")
				//Description = new MDLink("Click here", "https://etithespir.it/discord/commands.xhtml").ToString() + " to learn how commands work."
			};

			//result.AppendLine("__Global Commands:__");
			ConstructCommandList(executor, builder, CommandMarshaller.DefaultCommands, isConsole, out bool _2);
			//result.AppendLine(builder.ToString());
			embed.AddField(Personality.Get("cmd.help.globalCommand"), builder.ToString());

			builder = new DiffBuilder();
			Command[] cmds = executionContext?.Commands;
			if (cmds == null || cmds.Length == 0) {
				//result.Append("None");
				embed.AddField(Personality.Get("cmd.help.serverCmds"), "None");
			} else {
				ConstructCommandList(executor, builder, cmds, isConsole, out bool showed);
				if (showed) {
					embed.AddField(Personality.Get("cmd.help.serverCmds"), builder.ToString());
				} else {
					embed.AddField(Personality.Get("cmd.help.serverCmds"), "None");
				}
			}

			
			if (!isConsole) {
				embed.AddField("About You", $"**Permission Level:** {executor.GetPermissionLevel().GetFullName()}");
			} else {
				embed.AddField("About You", $"**Permission Level:** {PermissionLevel.Bot.GetFullName()}");
			}
			//embed.SetAuthor($"Click here to learn how to use commands.", new Uri("https://etithespir.it/discord/commands.xhtml"), new Uri(Images.INFORMATION));
			return embed.Build();
		}

		/// <summary>
		/// Post help for a specific command. This is public and static because it is used by external commands.
		/// </summary>
		/// <param name="executor"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public static Embed PostSpecificHelpMenu(Member executor, Command target, bool isConsole) {
			if (target is DeprecatedCommand depr) return PostSpecificHelpMenu(executor, depr.Target, isConsole);
			
			EmbedBuilder embed = new EmbedBuilder {
				Title = Personality.Get("cmd.help.helpMenuCommand", target.FullName),
				Description = target.Description
			};
			string footer = null;
			if (target.Syntax?.HasStringArg() ?? false) footer = "This command wants some text values! Remember to surround your text in quotes \"\" if you need to put spaces in it, and if you need to use an actual quote mark in your text, put a \\ before it, like this: \\\".";
			CommandUsagePacket usageInfo;
			if (!isConsole) {
				usageInfo = target.CanRunCommand(executor);
			} else {
				usageInfo = CommandUsagePacket.Success;
			}
			if (target.HasCustomUsageBehavior) {
				if (usageInfo.CanUse || isConsole) {
					string usage = "";
					if (target.Syntax != null && target.Syntax.ArgCount > 0) {
						usage = Personality.Get("cmd.help.syntax", target.Syntax);//$"**Syntax:** {target.Syntax}\n\n";
					}
					if (!string.IsNullOrWhiteSpace(usage)) embed.AddField(Personality.Get("cmd.help.cmdInfo"), usage);
					embed.Color = Color.GREEN;
				} else {
					if (target.CanSeeHelpForAnyway) {
						string usage = "";
						if (target.Syntax != null && target.Syntax.ArgCount > 0) {
							usage = Personality.Get("cmd.help.syntax", target.Syntax);//$"**Syntax:** {target.Syntax}\n\n";
						}
						if (!string.IsNullOrWhiteSpace(usage)) embed.AddField(Personality.Get("cmd.help.cmdInfo"), usage);
						embed.AddField(Personality.Get("cmd.help.additional"), Personality.Get("cmd.notAllowed.prefix", usageInfo.Reason));
						embed.Color = Color.YELLOW;
					} else {
						embed.AddField(Personality.Get("cmd.help.cmdInfo"), Personality.Get("cmd.notAllowed.prefix", usageInfo.Reason));
						embed.Color = Color.RED;
					}
				}
			} else {
				string usage = (usageInfo.CanUse || isConsole) ? "" : (Personality.Get("cmd.notAllowed.prefix", usageInfo.Reason) + "\n");
				// usage += $"**Requires:** {target.RequiredPermissionLevel.GetFullName()}\n**You are:** {executor.GetPermissionLevel().GetFullName()}";
				if (target.Syntax != null && target.Syntax.ArgCount > 0) {
					usage += Personality.Get("cmd.help.syntax", target.Syntax);
				}
				if (!string.IsNullOrWhiteSpace(usage)) embed.AddField(Personality.Get("cmd.help.cmdInfo"), usage);
				embed.Color = usageInfo.CanUse ? Color.GREEN : Color.RED;
			}

			if (target.Subcommands != null && target.Subcommands.Length > 0) {
				string subCommands = "";
				Command firstSub = null;
				for (int idx = 0; idx < target.Subcommands.Length; idx++) {
					Command sub = target.Subcommands[idx];
					bool usable = isConsole || sub.CanRunCommand(executor).CanUse;
					if (!usable && sub.CanSeeHelpForAnyway) {
						// Special handling.
						subCommands += $"`{target.Subcommands[idx].Name}`";
						if (firstSub == null) firstSub = sub;
						if (idx != target.Subcommands.Length - 1) {
							subCommands += ", ";
						}
					} else {
						bool visible = usable ||
										(!usable && sub.Visibility == CommandVisibilityType.Visible) &&
										sub.Visibility != CommandVisibilityType.Never;
						if (visible || isConsole) {
							if (firstSub == null) firstSub = sub;
							subCommands += $"`{target.Subcommands[idx].Name}`";
							if (idx != target.Subcommands.Length - 1) {
								subCommands += ", ";
							}
						}
					}
				}
				if (subCommands.EndsWith(", ")) {
					subCommands = subCommands[0..^2];
				}
				if (!string.IsNullOrWhiteSpace(subCommands)) {
					embed.AddField(Personality.Get("cmd.help.subcommands"), subCommands);
					footer += $"\n\nYou can use '>> help {target.FullName} [subcommand]' to get more information on these commands individually.\n\nTo use a subcommand, put its name after the main command, for example, `>> {firstSub.FullName}`";
				}
			}
			if (footer != null) embed.SetFooter(footer);
			embed.SetAuthor($"Click here to learn how to use commands.", new Uri("https://etithespir.it/discord/commands.xhtml"), new Uri(Images.VALUE_EDITABLE_GIF));
			return embed.Build();
		}
	}
}
