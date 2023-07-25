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
using OldOriBot.Data.Commands.ArgData;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Formatting;
using OldOriBot.Utility.Responding;

namespace OldOriBot.Data.Commands.Default {
	public class CommandWhoIs : Command {

		public override string Name { get; } = "whois";
		public override string Description { get; } = "Returns information about a given user.";
		public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Variant<Person, Snowflake>>("user").SetRequiredState(true);
		public CommandWhoIs() : base(null) { }

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			if (argArray.Length == 0) throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
			if (argArray.Length > 1) throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));

			ArgumentMap<Variant<Person, Snowflake>> argMap = Syntax.SetContext(executionContext).Parse<Variant<Person, Snowflake>>(argArray[0]);
			Variant<Person, Snowflake> arg0 = argMap.Arg1;

			User user;
			Member inServer; // May be null legitimately.
			if (arg0.ArgIndex == 1) {
				Person p = arg0.Value1;
				if (p.Member == null) throw new CommandException(this, Personality.Get("cmd.err.noMemberFound"));
				user = p.Member;
				inServer = p.Member;
			} else {
				user = await User.GetOrDownloadUserAsync(arg0.Value2);
				inServer = await user?.InServerAsync(executionContext.Server);
			}

			if (user == null) {
				throw new CommandException(this, Personality.Get("cmd.err.noMemberFound"));
			}

			EmbedBuilder builder = new EmbedBuilder {
				Title = "User Correlation",
				Description = $"**User ID:** {user.ID}\n**User:** {inServer?.FullNickname ?? user.FullName}"
			};
			builder.SetFooter("You can use the `>> about` command to get information such as when the account was created.", new Uri(Images.INFORMATION));

			await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, builder.Build(), AllowedMentions.Reply);
		}
	}
}
