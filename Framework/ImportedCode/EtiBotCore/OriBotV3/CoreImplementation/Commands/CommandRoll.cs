using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using OldOriBot.Interaction;
using OldOriBot.Utility.Arguments;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Utility.Marshalling;
using OldOriBot.PermissionData;
using OldOriBot.Exceptions;
using OldOriBot.Data;
using System.Linq;
using System.Text.RegularExpressions;
using EtiBotCore.DiscordObjects.Factory;
using OldOriBot.Utility.Responding;
using OldOriBot.Interaction.CommandData;
using EtiBotCore.Data.Structs;

namespace OldOriBot.CoreImplementation.Commands {
	public class CommandRoll : Command {

		private static readonly Random RNG = new Random();

		public override string Name { get; } = "roll";
		public override string Description { get; } = "Roll some RNG.";
		public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Variant<int, string>, int>("rollOrNumRolls", "sides").SetRequiredState(true, false);
		public override string[] Aliases { get; } = {
			"rng"
		};
		public override CommandVisibilityType Visibility { get; } = CommandVisibilityType.Never;

		public override Snowflake? GetUseInChannel(BotContext executionContext, Member member, Snowflake? channelUsedIn) {
			if (channelUsedIn == 617797540615815178) return 617797540615815178;
			return base.GetUseInChannel(executionContext, member, channelUsedIn);
		}

		public CommandRoll(BotContext ctx) : base(ctx) { }

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			if (argArray.Length < 1) {
				throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
			} else if (argArray.Length > 2) {
				throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
			}

			ArgumentMap<Variant<int, string>, int> args = Syntax.SetContext(executionContext).Parse<Variant<int, string>, int>(argArray[0], argArray.ElementAtOrDefault(1));
			double rollCount;
			double sides;
			char op = default;
			double mod = default;
			if (argArray.Length == 1) {
				if (args.Arg1.ArgIndex != 2) {
					throw new CommandException(this, Personality.Get("cmd.rng.invalidFormatSingle"));
				}

				Match match = Regex.Match(args.Arg1.Value2, @"(\d+)(d)(\d+)((\+|-|\*|\/|^)(\d+))?");
				// 1 2(d) 3 5 6
				if (!match.Success) {
					throw new CommandException(this, Personality.Get("cmd.rng.invalidFormatSingle"));
				}

				if (!double.TryParse(match.Groups[1].Value, out rollCount) || !double.TryParse(match.Groups[3].Value, out sides) || match.Groups[2].Value.ToLower() != "d") {
					throw new CommandException(this, Personality.Get("cmd.rng.invalidFormatSingle"));
				}
				if (match.Groups.Count == 7 && match.Groups[5].Success && match.Groups[6].Success) {
					string opStr = match.Groups[5].Value;
					if (opStr == "+") {
						op = '+';
					} else if (opStr == "-") {
						op = '-';
					} else if (opStr == "*") {
						op = '*';
					} else if (opStr == "/") {
						op = '/';
					} else if (opStr == "^") {
						op = '^';
					}
					if (!double.TryParse(match.Groups[6].Value, out mod)) {
						throw new CommandException(this, "Invalid modifier number.");
					}
				}
			} else {
				if (args.Arg1.ArgIndex != 1) {
					throw new CommandException(this, Personality.Get("generic.typeCastException", "Number", "Text"));
				}
				rollCount = args.Arg1.Value1;
				sides = args.Arg2;
			}

			rollCount = Math.Floor(rollCount);
			sides = Math.Floor(sides);

			if (rollCount < 1 || rollCount > 10) {
				throw new CommandException(this, "Argument out of range, expected value within [1-10].");
			}

			EmbedBuilder result = new EmbedBuilder();
			result.Title = "Roll Result";
			for (int rollIndex = 1; rollIndex <= rollCount; rollIndex++) {
				double v = RNG.NextDouble();
				v *= sides - 1;
				v += 1;
				double value = Math.Round(v);

				if (op == '+') {
					value += mod;
				} else if (op == '-') {
					value -= mod;
				} else if (op == '*') {
					value *= mod;
				} else if (op == '/') {
					value /= mod;
				} else if (op == '^') {
					value = Math.Pow(value, mod);
				}

				result.Description += "Result #" + rollIndex + ": " + value + "\n";
			}

			await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, result.Build());
		}
	}
}
