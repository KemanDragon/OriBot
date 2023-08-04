using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.Data.Commands.ArgData;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Formatting;
using OldOriBot.Utility.Responding;

namespace OldOriBot.Data.Commands.Default {
	public class CommandTypeInfo : Command { 

		public override string Name { get; } = "typeinfo";
		public override string Description { get; } = "Returns information about what a specific command parameter type is. These are the words like `Text` or `Number` in the Syntax of the command";
		public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string>("typeName").SetRequiredState(false);

		public static readonly IReadOnlyDictionary<string, string> Information;
		private static readonly Dictionary<string, string> InformationInternal;
		public static readonly IReadOnlyList<string> TypeList;

		static CommandTypeInfo() {
			InformationInternal = new Dictionary<string, string> {
				// System Types & Custom Type Bases
				[typeof(bool).Name] = "A value that is either true or false. The words \"yes\" and \"no\" are also valid inputs.",
				[typeof(sbyte).Name] = $"A whole number from {sbyte.MinValue} to {sbyte.MaxValue}",
				[typeof(byte).Name] = $"A whole number from {byte.MinValue} to {byte.MaxValue}",
				[typeof(short).Name] = $"A whole number from {short.MinValue} to {short.MaxValue}",
				[typeof(ushort).Name] = $"A whole number from {ushort.MinValue} to {ushort.MaxValue}",
				[typeof(int).Name] = $"A whole number from {int.MinValue} to {int.MaxValue}",
				[typeof(uint).Name] = $"A whole number from {uint.MinValue} to {uint.MaxValue}",
				[typeof(long).Name] = $"A whole number from {long.MinValue} to {long.MaxValue}",
				[typeof(ulong).Name] = $"A whole number from {ulong.MinValue} to {ulong.MaxValue}",
				[typeof(float).Name] = $"A decimal value. This has a range from -(1e38) to +(1e38).",
				[typeof(double).Name] = $"A decimal value. This has a range from -(1e308) to +(1e308).",
				[typeof(object).Name] = "literally whatever you want it to be chief lol",
				[typeof(Type).Name] = "Stop being so meta!",

				["bool"] = "A value that is either true or false. The words \"yes\" and \"no\" are also valid inputs.",
				["short"] = $"A whole number from {short.MinValue} to {short.MaxValue}",
				["ushort"] = $"A whole number from {ushort.MinValue} to {ushort.MaxValue}",
				["int"] = $"A whole number from {int.MinValue} to {int.MaxValue}",
				["uint"] = $"A whole number from {uint.MinValue} to {uint.MaxValue}",
				["long"] = $"A whole number from {long.MinValue} to {long.MaxValue}",
				["ulong"] = $"A whole number from {ulong.MinValue} to {ulong.MaxValue}",
				["float"] = $"A decimal value. This has a range from -(1e38) to +(1e38).",

				[typeof(string).Name] = $"Any sort of text. Generally speaking, text arguments should be put inside of quotation marks, but if it's just a single word without spaces, then quotes are not required.\n\nIf the text itself happens to have a quotation mark inside of it, such as `Sally said \"Hello\" to me`, you need to put a backslash \\ before each quote mark. That is, what you type in should look like: `\"Sally said \\\"Hello\\\" to me\"`. Basically, the \\ tells the computer \"ignore this next thing\".",
				[typeof(char).Name] = $"A single character. A character is something like a letter, number, or other symbol.",

				[typeof(Snowflake).Name] = $"An ID or Snowflake is a unique ID for anything on Discord, be it a server, a channel, a user, an emoji, a role, or much more. This can be acquired via developer mode. See *[Where can I find my User/Server/Message ID?](https://support.discord.com/hc/en-us/articles/206346498-Where-can-I-find-my-User-Server-Message-ID-)* for more information.",
				[typeof(Person).Name] = $"A reference to a specific person in this server. This can either be their unique ID (acquired via developer mode) `114163433980559366`, pinging them `@Eti`, their username or nickname `Eti`, or their full username#discriminator `Eti#1760`.",
				[typeof(Duration).Name] = $"A duration of time. This is usually denoted by a number and a suffix, examples being `5s` or `5` for 5 seconds, `1m` for 1 minute, `10h` for 10 hours, `3d` for 3 days, and `2w` for 2 weeks.",
				[typeof(Channel).Name] = $"A channel, either referenced by its ID, or by linking the channel e.g. #general",
				[typeof(DateAndTime).Name] = $"A representation of date and time (obviously). This is input as an EU-style date and time, and is always relative to UTC+0. An example might be `01/04/1969 6:12:15 PM` for 01 April 1969 at 6:12:15 PM",
			};
			InformationInternal["Decimal"] = "A number value that supports decimals, like `43.195`";
			InformationInternal["Integer"] = "A whole number value like `22`. Using decimals will cause an error.";
			InformationInternal["Text"] = InformationInternal[typeof(string).Name];
			InformationInternal["Character"] = InformationInternal[typeof(char).Name];
			InformationInternal["ID"] = InformationInternal[typeof(Snowflake).Name];
			InformationInternal["Either"] = "A variation between one of the types after the word. For example, `Either<Boolean or Number>` means you can input true or false (a boolean), or a number like 12345, but any other type of value will not work (for instance, putting in a color will cause an error).";
			InformationInternal["Color"] = "An RGB color code, either given via hex `#RRGGBB` or `RRGGBB`, or a formatted list such as `R, G, B`. Examples: `#ffffff`, `b2ffe9`, `255, 127, 0`";
			Information = InformationInternal;
			TypeList = new List<string> {
				"Decimal", "Integer",
				"Text", "Character",
				"ID", "Person",
				"Color", "Duration",
				"Boolean",
				"Channel", "DateAndTime",
				"Either"
			};
		}

		public CommandTypeInfo() : base(null) { }

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			if (argArray.Length > 1) {
				throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
			} else if (argArray.Length == 0) {
				EmbedBuilder builder = new EmbedBuilder {
					Title = "Type List"
				};
				string tList = "**Try one of these:**\n";
				foreach (string t in TypeList) {
					tList += $"• `>> {Name} {t}`\n";
				}
				builder.Description = tList;
				builder.SetFooter("Note: All C# primitive types are registered in this command too. If you don't know what that means, you can ignore this.", new Uri(Images.INFORMATION));
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, builder.Build(), AllowedMentions.Reply);
				return;
			}

			ArgumentMap<string> args = Syntax.Parse<string>(argArray[0]);
			string tName = args.Arg1;
			foreach (KeyValuePair<string, string> data in Information) {
				if (data.Key.ToLower() == tName.ToLower()) {
					EmbedBuilder builder = new EmbedBuilder {
						Title = "Type Information: " + data.Key,
						Description = data.Value
					};
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, builder.Build(), AllowedMentions.Reply);
					return;
				}
			}
			await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"I wasn't able to find any information on a type named {tName}", null, AllowedMentions.Reply);
		}
	}
}
