using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.Data.Persistence;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.Interaction.CommandData;
using OldOriBot.PermissionData;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Responding;

namespace OldOriBot.Data.Commands.Default {
	public class CommandConfig : Command {

		/// <summary>
		/// A soft hyphen that appears invisible.
		/// </summary>
		public const char INVISIBLE = '\u00AD';

		public override string Name { get; } = "config";
		public override string Description { get; } = "Alters configurations that dictate the bot's behavior.";
		public override ArgumentMapProvider Syntax { get; }
		public override Command[] Subcommands { get; }
		public override bool IsExclusiveBase { get; } = true;
		public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
		public override CommandVisibilityType Visibility { get; } = CommandVisibilityType.OnlyIfUsable;
		public override bool RequiresContext { get; } = true;
		public CommandConfig() : base(null) {
			Subcommands = new Command[] {
				new CommandConfigList(null, this),
				new CommandConfigGet(null, this),
				new CommandConfigSet(null, this),
				new CommandConfigRemove(null, this)
			};
		}

		public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) => throw new NotImplementedException();


		public class CommandConfigList : Command {
			public override string Name { get; } = "list";
			public override string Description { get; } = "Lists all configuration values. A domain must be provided to list configuration values. To list domains, run this command without any args. Using \"global\" will list all global domains.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string, int>("domain", "page").SetRequiredState(false, false);
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override CommandVisibilityType Visibility { get; } = CommandVisibilityType.OnlyIfUsable;
			public override bool RequiresContext { get; } = true;
			public CommandConfigList(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				ArgumentMap<string, int> args = Syntax.SetContext(executionContext).Parse<string, int>(argArray.ElementAtOrDefault(0), argArray.ElementAtOrDefault(1));

				if (args.Arg1 != null) {
					if (args.Arg1 == "global") {
						EmbedBuilder builder = new EmbedBuilder {
							Title = "Global Configuration Domains",
							Description = ""
						};
						foreach (string domain in DataPersistence.Domains["global"]) {
							builder.Description += $"• `global-{domain}`\n";
						}
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, builder.Build(), AllowedMentions.Reply);
					} else {
						DataPersistence target;
						if (args.Arg1.StartsWith("global-") && args.Arg1.Length > 7) {
							target = DataPersistence.GetDataPersistenceNoCreate(null, args.Arg1[7..] + ".cfg");
						} else {
							target = DataPersistence.GetDataPersistenceNoCreate(executionContext, args.Arg1 + ".cfg");
						}
						if (target == null) {
							throw new CommandException(this, Personality.Get("cmd.config.noDomain"));
						}
						int keysPerPage = DataPersistence.Global.TryGetType("EntriesPerConfigPage", 20);
						string[] keys = target.OrderedKeys;
						int numPages = (int)Math.Floor(keys.Length / (double)keysPerPage);
						if (args.Arg2 > numPages) {
							throw new CommandException(this, Personality.Get("cmd.config.invalidPage"));
						}

						keys = keys.Skip(args.Arg2 * keysPerPage).Take(keysPerPage).ToArray();
						EmbedBuilder builder = new EmbedBuilder {
							Title = $"Configuration Values: {target.Domain}, page {args.Arg2 + 1}/{numPages + 1}",
							Description = "```css\n"
						};

						foreach (string key in keys) {
							builder.Description += $"{key}=[{target.GetValue(key)}]\n";
						}
						builder.Description += "```";
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, builder.Build(), AllowedMentions.Reply);
					}
				} else {
					EmbedBuilder builder = new EmbedBuilder {
						Title = "Configuration Domains",
						Description = ""
					};
					foreach (string domain in DataPersistence.Domains[executionContext.DataPersistenceName]) {
						builder.Description += $"• `{domain}`\n";
					}
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, builder.Build(), AllowedMentions.Reply);
				}
			}
		}

		public class CommandConfigGet : Command {
			public override string Name { get; } = "get";
			public override string Description { get; } = "Get a specific configuration value. A domain is a specific configuration file. For a list of domains, run the list subcommand without any args.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string, string>("domain", "key").SetRequiredState(true, true);
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override CommandVisibilityType Visibility { get; } = CommandVisibilityType.OnlyIfUsable;
			public override bool RequiresContext { get; } = true;
			public CommandConfigGet(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length < 2) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(1)} and/or {Syntax.GetArgName(0)}"));
				} else if (argArray.Length > 2) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}
				ArgumentMap<string, string> args = Syntax.SetContext(executionContext).Parse<string, string>(argArray[0], argArray[1]);

				DataPersistence target;
				if (args.Arg1.StartsWith("global-") && args.Arg1.Length > 7) {
					target = DataPersistence.GetDataPersistenceNoCreate(null, args.Arg1[7..] + ".cfg");
				} else {
					target = DataPersistence.GetDataPersistenceNoCreate(executionContext, args.Arg1 + ".cfg");
				}
				
				if (target == null) {
					throw new CommandException(this, Personality.Get("cmd.config.noDomain"));
				}
				if (!target.ContainsKey(args.Arg2)) {
					throw new CommandException(this, Personality.Get("cmd.config.invalidKey"));
				}
				EmbedBuilder builder = new EmbedBuilder {
					Title = $"Configuration Entry: {target.Domain} :: {args.Arg2}",
					Description = $"The value of this configuration entry is:\n{target.GetValue(args.Arg2)}"
				};
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, builder.Build(), AllowedMentions.Reply);
			}
		}

		public class CommandConfigSet : Command {
			public override string Name { get; } = "set";
			public override string Description { get; } = "Set a specific configuration value in the given domain. A domain is a specific configuration file. For a list of domains, run the list subcommand without any args.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string, string, string>("domain", "key", "value").SetRequiredState(true, true, true);
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override CommandVisibilityType Visibility { get; } = CommandVisibilityType.OnlyIfUsable;
			public override bool RequiresContext { get; } = true;
			public CommandConfigSet(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length < 3) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(2)}, {Syntax.GetArgName(1)}, and/or {Syntax.GetArgName(0)}"));
				} else if (argArray.Length > 3) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}
				ArgumentMap<string, string, string> args = Syntax.SetContext(executionContext).Parse<string, string, string>(argArray[0], argArray[1], argArray[2]);
				DataPersistence target;
				if (args.Arg1.StartsWith("global-") && args.Arg1.Length > 7) {
					target = DataPersistence.GetDataPersistenceNoCreate(null, args.Arg1[7..] + ".cfg");
				} else {
					target = DataPersistence.GetDataPersistenceNoCreate(executionContext, args.Arg1 + ".cfg");
				}
				if (target == null) {
					throw new CommandException(this, Personality.Get("cmd.config.noDomain"));
				}
				target.SetValue(args.Arg2, args.Arg3);
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.config.set", args.Arg2, args.Arg1, args.Arg3), mentions: AllowedMentions.Reply);
			}
		}

		public class CommandConfigRemove : Command {
			public override string Name { get; } = "remove";
			public override string Description { get; } = "Remove a specific configuration key/value pair from the given domain. A domain is a specific configuration file. For a list of domains, run list without any args.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string, string>("domain", "key").SetRequiredState(true, true);
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override CommandVisibilityType Visibility { get; } = CommandVisibilityType.OnlyIfUsable;
			public override bool RequiresContext { get; } = true;
			public CommandConfigRemove(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length < 2) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(1)}, and/or {Syntax.GetArgName(0)}"));
				} else if (argArray.Length > 2) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}
				ArgumentMap<string, string> args = Syntax.SetContext(executionContext).Parse<string, string>(argArray[0], argArray[1]);
				DataPersistence target;
				if (args.Arg1.StartsWith("global-") && args.Arg1.Length > 7) {
					target = DataPersistence.GetDataPersistenceNoCreate(null, args.Arg1[7..] + ".cfg");
				} else {
					target = DataPersistence.GetDataPersistenceNoCreate(executionContext, args.Arg1 + ".cfg");
				}
				if (target == null) {
					throw new CommandException(this, Personality.Get("cmd.config.noDomain"));
				}
				if (!target.ContainsKey(args.Arg2)) {
					throw new CommandException(this, Personality.Get("cmd.config.invalidKey"));
				}
				target.RemoveValue(args.Arg2);
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.config.removed", args.Arg2), mentions: AllowedMentions.Reply);
			}
		}

		public class CommandConfigReload : Command {
			public override string Name { get; } = "reload";
			public override string Description { get; } = "Reloads a specific configuration domain from file. A domain is a specific configuration file. For a list of domains, run list without any args.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string>("domain").SetRequiredState(true);
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override CommandVisibilityType Visibility { get; } = CommandVisibilityType.OnlyIfUsable;
			public override bool RequiresContext { get; } = true;
			public CommandConfigReload(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length < 1) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
				} else if (argArray.Length > 1) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}
				ArgumentMap<string> args = Syntax.SetContext(executionContext).Parse<string>(argArray[0]);
				DataPersistence target;
				if (args.Arg1.StartsWith("global-") && args.Arg1.Length > 7) {
					target = DataPersistence.GetDataPersistenceNoCreate(null, args.Arg1[7..] + ".cfg");
				} else {
					target = DataPersistence.GetDataPersistenceNoCreate(executionContext, args.Arg1 + ".cfg");
				}
				if (target == null) {
					throw new CommandException(this, Personality.Get("cmd.config.noDomain"));
				}
				target.Reload();
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.config.reloaded"));
			}
		}

	}
}
