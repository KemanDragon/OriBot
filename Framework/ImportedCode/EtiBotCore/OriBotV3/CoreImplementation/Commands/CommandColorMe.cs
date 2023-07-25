using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.Data;
using OldOriBot.Data.Flair;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Extensions;
using OldOriBot.Utility.Responding;

namespace OldOriBot.CoreImplementation.Commands {
	public class CommandColorMe : Command {

		private Task ColorCreationTask;

		public CommandColorMe(BotContext ctx) : base(ctx) {
			string presets = "**Possible Colors:** ";
			foreach (string clr in UserColor.ColorKeywords.Keys) {
				presets += clr.SurroundIn('`') + ", ";
			}
			presets += "\n**Brightness Keywords:** ";
			foreach (string key in UserColor.ModifierKeywords.Keys) {
				presets += key.SurroundIn('`') + ", ";
			}
			Description = "Sets your role color based on a list of presets. Use the command without any arguments, or input \"none\" or \"null\", to remove your color.\n\n" + presets;

		}

		public void InstantiateAllColors(BotContext inContext) {
			ColorCreationTask = UserColor.InstantiateAllColors(inContext);
		}

		public override string Name { get; } = "color";
		public override string Description { get; }
		public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string, string>("modifier", "color").SetRequiredState(false, false);
		public override string[] Aliases { get; } = {
			"colour",
			"colorme",
			"colourme"
		};

		public override bool NoConsole { get; } = true;

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			if (!ColorCreationTask.IsCompleted) {
				await originalMessage.ReplyAsync("Just a second! I'm still creating all of the color roles (or finding them). I'll run your command when I'm done. **This could take up to a minute at worst.**", null, AllowedMentions.Reply);
				await ColorCreationTask;
			}
			//if (argArray.Length == 0) {
				//throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
		//	} else
			if (argArray.Length > 2) {
				throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
			}

			string alpha, bravo;
			ArgumentMap<string, string> args = Syntax.SetContext(executionContext).Parse<string, string>(argArray.ElementAtOrDefault(0), argArray.ElementAtOrDefault(1));
			alpha = args.Arg1;
			bravo = args.Arg2;
			bool nothing = alpha == null && bravo == null;

			bool hasBright = alpha == "light" || bravo == "light";
			if (hasBright) {
				throw new CommandException(this, "`light` is not a valid keyword, as bright colors are limited to staff members only. Try `mid` instead!");
			}

			if (bravo == null) bravo = "mid";

			Message waitMsg = await originalMessage.ReplyAsync(Personality.Get("generic.working"), null, AllowedMentions.Reply);

			if (nothing || alpha.ToLower() == "none" || alpha.ToLower() == "null") {
				List<Role> rolesToRemove = new List<Role>();
				foreach (Role r in executor.Roles) {
					if (r.Name.StartsWith("UserColor")) {
						rolesToRemove.Add(r);
					}
				}
				executor.BeginChanges();
				foreach (Role r in rolesToRemove) executor.Roles.Remove(r);
				await executor.ApplyChanges("Removed user color.");
				waitMsg.BeginChanges();
				waitMsg.Content = Personality.Get("generic.workDone");
				await waitMsg.ApplyChanges("Telling command user that the work is done.");
				return;
			} else {
				// await ResponseUtil.StartTypingAsync(originalMessage);
				Role role = await UserColor.GetRoleFromColor(executionContext, alpha, bravo);
				if (role != null) {
					List<Role> rolesToRemove = new List<Role>();
					foreach (Role r in executor.Roles) {
						if (r.Name.StartsWith("UserColor")) {
							rolesToRemove.Add(r);
						}
					}
					executor.BeginChanges();
					foreach (Role r in rolesToRemove) executor.Roles.Remove(r);
					executor.Roles.Add(role);
					await executor.ApplyChanges("Changed user color.");
					// await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.colorme.success.add"), null, AllowedMentions.Reply);
				} else {
					throw new CommandException(this, "I can't figure out what color role you want.");
				}
				waitMsg.BeginChanges();
				waitMsg.Content = Personality.Get("generic.workDone");
				await waitMsg.ApplyChanges("Telling command user that the work is done.");
				return;
			}
		}
	}
}
