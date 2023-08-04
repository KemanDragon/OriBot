using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using OldOriBot.Data;
using OldOriBot.Data.Commands.ArgData;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Responding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OldOriBot.CoreImplementation.Commands {
	public class CommandMakeTimestamp : Command {

		private const ulong MAX_DATE = 4_398_046_511_104; // The highest date supported by Discord.

		public override string Name { get; } = "makets";
		public override string[] Aliases { get; } = new string[] {
			"maketimestamp",
			"gettimestamp",
			"getts",
			"time"
		};
		public override string Description { get; } = @"Using Discord's timestamp format system, this can be used to create a timestamp that displays in local time for everyone viewing it.
The `mode` argument accepts Discord's [Timestamp Styles](https://discord.com/developers/docs/reference#message-formatting-timestamp-styles), but for ease of access, they are:
• `t` for Short Time, such as `16:20`
• `T` for Long Time, such as `16:20:30`
• `d` for Short Date, such as `20/04/2021`
• `D` for Long Date, such as `20 April 2021`
• `f` for Short Date/Time, such as `20 April 2021 16:20`
• `F` for Long Date/Time, such as `Tuesday, 20 April 2021 16:20`
• `R` for Relative Time, such as `2 months ago`

Similarly to Discord, the default value is `f`.

**Reminder:** The input time is relative to GMT+0. If your time zone is GMT-6, then you need to add 6 hours to the time.";
		public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<DateAndTime, string>("dateAndTime", "mode").SetRequiredState(true, false);
		public CommandMakeTimestamp(BotContext ctx) : base(ctx) { }

		private static readonly string[] VALID_TAGS = new string[] { "t", "T", "d", "D", "f", "F", "R" };

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			if (argArray.Length < 1) {
				throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
			} else if (argArray.Length > 2) {
				throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
			}

			ArgumentMap<DateAndTime, string> args = Syntax.SetContext(executionContext).Parse<DateAndTime, string>(argArray[0], argArray.ElementAtOrDefault(1));
			DateAndTime input = args.Arg1;
			string mode = args.Arg2 ?? "f";

			if (!VALID_TAGS.Contains(mode)) {
				throw new CommandException(this, $"Invalid parameter `mode`! Use `>> help {Name}` to see a list of valid modes.");
				// TODO: Use personality system so this message can go into the text doc.
			}

			ulong unix = (ulong)input.Inner.ToUnixTimeSeconds();
			if (unix > MAX_DATE) {
				throw new CommandException(this, "Timestamps with values above around 2^42 aren't functional.");
			}

			string result = $"<t:{unix}:{mode}>";
			await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Your timestamp code is `{result}` which shows as {result} (this time is localized to the reader's time zone).");
		}
	}
}
