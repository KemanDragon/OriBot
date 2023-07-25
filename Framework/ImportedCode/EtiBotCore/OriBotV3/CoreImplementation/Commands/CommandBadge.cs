using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.Data;
using OldOriBot.Data.Commands.ArgData;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.UserProfiles;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Responding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OldOriBot.CoreImplementation.Commands {
	public class CommandBadge : Command {
		public override string Name { get; } = "badge";
		public override string Description { get; } = "Provides options to view predefined badges. Defining a person for the `onProfile` parameter can be used to view custom badge information.";
		public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string, Person>("badgeName", "onProfile").SetRequiredState(false, false);
		public override Command[] Subcommands { get; }

		public CommandBadge(BotContext ctx) : base(ctx) {
			Subcommands = new Command[] {
				new CommandBadgeList(ctx, this)
			};
		}

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			if (argArray.Length == 0) {
				throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
			} else if (argArray.Length > 2) {
				throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
			}

			ArgumentMap<string, Person> args = Syntax.SetContext(executionContext).Parse<string, Person>(argArray[0], argArray.ElementAtOrDefault(1));
			string badgeName = args.Arg1;
			Person target = args.Arg2;

			Badge info = BadgeRegistry.GetBadgeFromPredefinedRegistry(badgeName);
			if (info != null) {
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, info.ToEmbed(), AllowedMentions.Reply);
			} else {
				// Now wait - new behavior
				if (target?.Member != null) {
					Member mbr = target.Member;
					UserProfile profile = UserProfile.GetOrCreateProfileOf(mbr);
					info = profile.Badges.FirstOrDefault(badge => badge.Name.ToLower() == badgeName.ToLower());
					if (info != null) {
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, info.ToEmbed(), AllowedMentions.Reply);
						return;
					}
				}
				throw new CommandException(this, Personality.Get("cmd.ori.profile.err.noBadgeFound", badgeName));
				// ^ has a good enough message
			}
		}

		public class CommandBadgeList : Command {
			public override string Name { get; } = "list";
			public override string Description { get; } = "Lists all badges that are predefined in the system. This does not include any custom badges. To view custom badges, you must define the ID of the user whose profile has said badge (see >> help badge)";
			public override ArgumentMapProvider Syntax { get; }
			public CommandBadgeList(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				EmbedBuilder builder = new EmbedBuilder {
					Title = "All Badges"
				};
				StringBuilder badgeBuilder = new StringBuilder();
				foreach (Badge badge in BadgeRegistry.AllBadges) {
					badgeBuilder.AppendLine($"{badge.Icon} __**{badge.Name}**__");
				}
				builder.Description = badgeBuilder.ToString();
				builder.SetFooter("To get information on a specific badge, input its name into the badge command (do not include the icon).");
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, builder.Build(), AllowedMentions.Reply);
			}
		}
	}
}
