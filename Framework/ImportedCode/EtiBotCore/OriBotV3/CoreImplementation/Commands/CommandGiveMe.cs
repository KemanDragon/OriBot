using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Guilds.Specialized;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.Data;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Responding;

namespace OldOriBot.CoreImplementation.Commands {
	public class CommandGiveMe : Command {
		public override string Name { get; } = "role";
		public override string Description { get; } = "Give yourself a vanity role, or take it away. Roles for completing games are **exclusive**, meaning you can only have one at a time. They will automatically swap out as needed. To get rid of it, give yourself the one you already have.";
		public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string>("roleName").SetRequiredState(true);
		public override string[] Aliases { get; } = {
			"giveme",
			"give",
			"roles"
		};
		public override bool NoConsole { get; } = true;
		public IReadOnlyDictionary<string, ManagedRole> NameToRoleBindings { get; internal set; }

		public override Command[] Subcommands { get; }

		private bool HasInitialized = false;

		public CommandGiveMe(BotContext ctx) : base(ctx) {
			//CompletedBothRole = new ManagedRole(ctx.Server, "Completed Both Ori Games");
			//_ = CompletedBothRole.Initialize();
			Subcommands = new Command[] {
				new CommandListRoles(ctx, this)
			};
		}

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			if (argArray.Length == 0) {
				// NEW BEHAVIOR: Post role list.
				await Subcommands[0].ExecuteCommandAsync(executor, executionContext, originalMessage, argArray, rawArgs, isConsole);
				return;

				//throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
			} else if (argArray.Length > 1) {
				throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
			}
			if (!HasInitialized) {
				HasInitialized = true;
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.giveme.init"));
				//await originalMessage.ServerChannel.StartTypingAsync();
				await ResponseUtil.StartTypingAsync(originalMessage);
				foreach (ManagedRole r in NameToRoleBindings.Values) {
					await r.Initialize();
					await Task.Delay(500);
				}
			}

			ArgumentMap<string> args = Syntax.SetContext(executionContext).Parse<string>(argArray[0]);
			string roleName = args.Arg1.ToLower();

			List<ManagedRole> candidates = new List<ManagedRole>();
			foreach (KeyValuePair<string, ManagedRole> data in NameToRoleBindings) {
				// keys should be lower
				if (data.Key.StartsWith(roleName)) {
					candidates.Add(data.Value);
				}
			}
			if (candidates.Count == 0) {
				throw new CommandException(this, Personality.Get("cmd.ori.giveme.err.noRoleFound"));
			} else if (candidates.Count > 1) {
				throw new CommandException(this, Personality.Get("cmd.ori.giveme.err.multipleRolesFound"));
			} else {
				Role target = candidates[0].Role;
				executor.BeginChanges();
				bool wantsBF = target == NameToRoleBindings["completedbf"]; // u want a bf? thats kinda cringe bro,,,,,
				bool wantsWotW = target == NameToRoleBindings["completedwotw"];
				bool wantsBoth = target == NameToRoleBindings["completedboth"];

				if (wantsBF || wantsWotW || wantsBoth) {
					bool hasCompletedBF = executor.Roles.Contains(NameToRoleBindings["completedbf"].Role);
					bool hasCompletedWotW = executor.Roles.Contains(NameToRoleBindings["completedwotw"].Role);
					bool hasCompletedBoth = executor.Roles.Contains(NameToRoleBindings["completedboth"].Role);
					if (hasCompletedBF) executor.Roles.Remove(NameToRoleBindings["completedbf"].Role);
					if (hasCompletedWotW) executor.Roles.Remove(NameToRoleBindings["completedwotw"].Role);
					if (hasCompletedBoth) executor.Roles.Remove(NameToRoleBindings["completedboth"].Role);

					bool actionWasRemoval = false;
					if (wantsBF) {
						if (hasCompletedBF) {
							actionWasRemoval = true;
						} else {
							executor.Roles.Add(NameToRoleBindings["completedbf"].Role);
						}
					} else if (wantsWotW) {
						if (hasCompletedWotW) {
							actionWasRemoval = true;
						} else {
							executor.Roles.Add(NameToRoleBindings["completedwotw"].Role);
						}
					} else if (wantsBoth) {
						if (hasCompletedBoth) {
							actionWasRemoval = true;
						} else {
							executor.Roles.Add(NameToRoleBindings["completedboth"].Role);
						}
					}

					if (actionWasRemoval) {
						await executor.ApplyChanges($"Member used the {Name} command and needed this role removed.");
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.giveme.success.remove", target.Name), mentions: AllowedMentions.Reply);
					} else {
						await executor.ApplyChanges($"Member used the {Name} command and needed this role added, and other game roles removed.");
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.giveme.success.addAndRemove", target.Name), mentions: AllowedMentions.Reply);
					}
				} else {
					if (executor.Roles.Contains(target)) {
						executor.Roles.Remove(target);
						await executor.ApplyChanges($"Member used the {Name} command and needed this role removed.");
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.giveme.success.remove", target.Name), mentions: AllowedMentions.Reply);
					} else {
						executor.Roles.Add(target);
						await executor.ApplyChanges($"Member used the {Name} command and needed this role added.");
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.giveme.success.add", target.Name), mentions: AllowedMentions.Reply);
					}
				}
			}
		}

		public class CommandListRoles : Command {
			public override string Name { get; } = "list";
			public override string Description { get; } = "Lists all of the available vanity roles.";
			public override ArgumentMapProvider Syntax { get; }

			public CommandListRoles(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				CommandGiveMe cmd = (CommandGiveMe)Parent;
				EmbedBuilder resultBuilder = new EmbedBuilder {
					Title = "All Vanity Roles",
					Description = "Try one of these!\n"
				};
				foreach (string key in cmd.NameToRoleBindings.Keys) {
					resultBuilder.Description += "• `>> " + Parent.Name + " " + key + "`\n";
				}
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, resultBuilder.Build(), AllowedMentions.Reply);
			}
		}
	}
}
