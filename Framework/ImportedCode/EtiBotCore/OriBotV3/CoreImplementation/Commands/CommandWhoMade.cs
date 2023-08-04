using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Utility.Marshalling;
using OldOriBot.Data;
using OldOriBot.Data.Commands.ArgData;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Responding;

namespace OldOriBot.CoreImplementation.Commands {
	public class CommandWhoMade : Command {

		public const string EMOJI_MATCH = @"<(a?):(.+):(\d+)>";

		public override string Name { get; } = "whomade";
		public override string Description { get; } = "When given a user-made Emoji for this server, this will display the name of the user that made it.";
		public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string>("emoji").SetRequiredState(true);
		public override Command[] Subcommands { get; }

		#region Data

		/// <summary>
		/// A map from creator name (or ID as string) to the IDs of the Emojis they have created.
		/// </summary>
		public static readonly Dictionary<string, List<ulong>> CreatorToEmojisMap = new Dictionary<string, List<ulong>>() {
			["616136907860213760"] = new List<ulong>() { 
				// ori bot ID
				// Moon
				710894064714645534,
				710894064723296326,
				710894064395878482,
				710894064559587440,
				710894064697999380,
				671886904903335937,
				671886905671024640,
				671886904702140469,
				671886904974901250,
				671886904773443584,
				671886905440206849,
				671886983194214406,
				671886982972178432,
				671886982997344285,
				671886982976372738,
				671886983726891033,
				671886983139688448,
				671886983139950592,
				671886982967722002,
				671886982976241705,
				671886983026704395,
				671886982754074655,

				// Microsoft
				685306067932151841,
				685306067957055549
			},
			/*
			["Moon Studios"] = new List<ulong>() {
				// Moon
				710894064714645534,
				710894064723296326,
				710894064395878482,
				710894064559587440,
				710894064697999380,
				671886904903335937,
				671886905671024640,
				671886904702140469,
				671886904974901250,
				671886904773443584,
				671886905440206849,
				671886983194214406,
				671886982972178432,
				671886982997344285,
				671886982976372738,
				671886983726891033,
				671886983139688448,
				671886983139950592,
				671886982967722002,
				671886982976241705,
				671886983026704395,
				671886982754074655
			},
			["Microsoft"] = new List<ulong>() {
				685306067932151841,
				685306067957055549
			},
			*/
			["63506518695215104"] = new List<ulong>() {
				// Rin 63506518695215104
				628302357922316329,
				628302358182363167,
				628302358098739220,
				628302358182625331
			},
			["216451709063397376"] = new List<ulong>() {
				// Arvemis 216451709063397376
				716054854048415865,
				716054854493274223,
				716054855197786113,
				716054855608959006
			},
			["128764014896480256"] = new List<ulong>() {
				// Eggy 128764014896480256
				716054854706921523
			},
			["472472848003235850"] = new List<ulong>() {
				// KurroKiri 472472848003235850
				697883662057734726
			},
			["191624173380960256"] = new List<ulong>() {
				// Borazilla 191624173380960256
				697541032417558638,
				697880416820133951
			},
			["357810501901877259"] = new List<ulong>() {
				// Oli 357810501901877259
				695741277970628658,
			},
			["911897708963450880"] = new List<ulong>() {
				// F-L-R-N 161256119107321857, now 911897708963450880 due to hack.
				693635578222084156,
			},
			["173216716107677697"] = new List<ulong>() {
				// BoomKatz 173216716107677697
				693636122097614899,
				693636122290683914
			},
			["167995760544186368"] = new List<ulong>() {
				// Uni99 167995760544186368
				693635928211849256,
			},
			["190293569624473600"] = new List<ulong>() {
				// SilverStarStrike 190293569624473600
				693635899312963605,
			},
			["506495908473733158"] = new List<ulong>() {
				// Rhombidandy 506495908473733158
				694772246585147452,
				699415887404597259,
				723217835051974747,
				734707157890760745
			},
			["191675819121180672"] = new List<ulong>() {
				// KirbyPie 191675819121180672
				723217612687015996,
			},
			["334743203603283969"] = new List<ulong>() {
				// Kazooie 334743203603283969
				723217744748871780,
				875457469348982854,
				875457438424379412,
				875457425325555742,
				875457485182476379,
				875457451779063810,
				875457410792304650,
				875515340929523732,
				875522825660797029,
				875530156691824690,
				875539545263849482, // a:
				875546892338081822,
				875561709241266216,
				875566393632895026,
				881549710941388882,
				881549743623372920,
				881549901018837072,
				881550018618748999,
				881555911112548383,
			},
			["225205975425089537"] = new List<ulong>() {
				// don 225205975425089537
				723217960759722035,
			},
			["701644285585391676"] = new List<ulong>() {
				// amateurlurker 701644285585391676
				723401323051089940,
				723401322061234199,
				734707010565832764,
				757665681255956517,
				790427850372677654,
				790427850040934430,
				790427853334380554
			},
			/*
			 * rip in piece: these emojis
			["709776147109380098"] = new List<ulong>() {
				// floof the moth 709776147109380098 
				725794349551714395,
				728377282095218750,
				757665788583739512,
				757665751787110461,
				790427849902522398,
				790427850892640306,
				854276463335047201,
			},
			*/
			["657755721534013440"] = new List<ulong>() {
				// finx the false arbiter 657755721534013440
				790427851085971456,
				790427849416245258
			},
			["714137378419114016"] = new List<ulong>() {
				// willow the nature spirit 714137378419114016
				790427851336843264,
			},
			["354240014844035079"] = new List<ulong>() {
				// sonninja 354240014844035079
				854276463657615370,
			},
			["562270213668732931"] = new List<ulong>() {
				// tikialia 562270213668732931
				854276463456288768,
			},
			["234815173100175361"] = new List<ulong>() {
				// jetaru 234815173100175361
				875561389081632828
			},
			["537399918025768982"] = new List<ulong>() {
				// blade 537399918025768982
				875562762028343368
			},
			["621872009315483659"] = new List<ulong>() {
				// prate 621872009315483659
				881552046325628948,
				881549764569739354,
				881549920635605053,
				881549938310402048,
				881549979381039104,

			},
			["479316888837554176"] = new List<ulong>() {
				// rekku (rafa) 479316888837554176
				881549959277711430
			},
			["598988124026175514"] = new List<ulong>() {
				// lilac 598988124026175514
				881550041137954877
			}
		};

		[Obsolete("Not implemented yet.")]
		public static readonly Dictionary<string, List<ulong>> CreatorToStickersMap = new Dictionary<string, List<ulong>>() {
			["701644285585391676"] = new List<ulong>() {
				// Amateurlurker

			}
		};

		/// <summary>
		/// A list of backups in case the user ID is valid for an emoji creator, but the user has left the server which renders it impossible to get their username.
		/// </summary>
		public static readonly Dictionary<ulong, string> ReserveNames = new Dictionary<ulong, string>() {
			[63506518695215104] = "RinTheYordle",
			[216451709063397376] = "Arvemis",
			[128764014896480256] = "Eggy",
			[472472848003235850] = "KurroKiri",
			[191624173380960256] = "Borazilla",
			[357810501901877259] = "Oli",
			// [161256119107321857] = "F-L-R-N",
			[911897708963450880] = "F-L-R-N",
			[173216716107677697] = "BoomKatz",
			[167995760544186368] = "Uni99",
			[190293569624473600] = "SilverStarStrike",
			[506495908473733158] = "Rhombidandy",
			[191675819121180672] = "KirbyPie",
			[334743203603283969] = "FoxyMangleBreegull / Kazooie",
			[225205975425089537] = "Don / Kira",
			[701644285585391676] = "amatuerlurker",
			// [709776147109380098] = "Floof the Moth",
			[657755721534013440] = "Finx the False Arbiter",
			[714137378419114016] = "Willow the nature spirit",
			[354240014844035079] = "SonNinja",
			[562270213668732931] = "Tikialia",
			[234815173100175361] = "Jetaru",
			[537399918025768982] = "Blade / Ansar"
		};

		public static readonly List<Snowflake> NullUsers = new List<Snowflake>();

		#endregion

		public CommandWhoMade(BotContext ctx) : base(ctx) {
			Subcommands = new Command[] {
				new CommandWhoMadeList(ctx, this)
			};
		}

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			if (argArray.Length == 0) throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
			Match match = Regex.Match(argArray[0], EMOJI_MATCH);
			if (match.Success) {

				// 0 is a potential letter "a"
				/*
				string emojiName = match.Groups[1].Value;
				ulong emojiId = ulong.Parse(match.Groups[2].Value);
				*/
				string emoji = match.Value;

				string emojiName = GetEmojiName(emoji);
				string emojiIdStr = GetEmojiId(emoji);
				ulong emojiId = ulong.Parse(emojiIdStr);

				Snowflake creatorIdSF = default;
				string creatorMention = null;
				foreach (string creator in CreatorToEmojisMap.Keys) {
					List<ulong> emojis = CreatorToEmojisMap[creator];
					if (emojis.Contains(emojiId)) {
						if (ulong.TryParse(creator, out ulong creatorId)) {
							// if (XanBotCoreSystem.IsDebugMode) await ResponseUtil.RespondToAsync(originalMessage, "Scanning creator " + creatorId);
							creatorMention = await GetName(creatorId, executionContext);
							creatorIdSF = creatorId;
						} else {
							creatorMention = creator;
						}
						break;
					}
				}

				if (creatorMention != null) {
					if (creatorIdSF.IsValid) {
						User usr = await User.GetOrDownloadUserAsync(creatorIdSF);
						Member mbr = await usr.InServerAsync(executionContext.Server);
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"<:{emojiName}:{emojiId}> was made by: {creatorMention} ({mbr.FullNickname})", mentions: AllowedMentions.Reply);
					} else {
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"<:{emojiName}:{emojiId}> was made by: {creatorMention}", mentions: AllowedMentions.Reply);
					}
				} else {
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "I don't know who made this emoji. Sorry!", mentions: AllowedMentions.Reply);
				}
			} else {
				// Catch case: What if it's an animated emoji and the user doesn't have nitro?
				string emojiName = null;
				ulong emojiId = 0;
				bool found = false;
				foreach (Emoji emoji in executionContext.Server.Emojis) {
					if (emoji.Name.Replace(":", "") == argArray[0].Replace(":", "")) {
						emojiName = emoji.Name;
						emojiId = emoji.ID;
						found = true;
						break;
					}
				}

				if (!found) {
					throw new CommandException(this, "This emoji is invalid! Is it a user-created emoji? Is it from this server?");
				}

				Snowflake creatorIdSF = default;
				string creatorMention = null;
				foreach (string creator in CreatorToEmojisMap.Keys) {
					List<ulong> emojis = CreatorToEmojisMap[creator];
					if (emojis.Contains(emojiId)) {
						if (ulong.TryParse(creator, out ulong creatorId)) {
							// if (XanBotCoreSystem.IsDebugMode) await ResponseUtil.RespondToAsync(originalMessage, "Scanning creator " + creatorId);
							creatorMention = await GetName(creatorId, executionContext);
							creatorIdSF = creatorId;
						} else {
							creatorMention = creator;
						}
						break;
					}
				}

				if (creatorMention != null) {
					if (creatorIdSF.IsValid) {
						User usr = await User.GetOrDownloadUserAsync(creatorIdSF);
						Member mbr = await usr.InServerAsync(executionContext.Server);
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"<:{emojiName}:{emojiId}> was made by: {creatorMention} ({mbr.FullNickname})", mentions: AllowedMentions.Reply);
					} else {
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"<:{emojiName}:{emojiId}> was made by: {creatorMention}", mentions: AllowedMentions.Reply);
					}
				} else {
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "I don't know who made this emoji. Sorry!", mentions: AllowedMentions.Reply);
				}
			}
		}

		public class CommandWhoMadeList : Command {
			public override string Name { get; } = "list";
			public override string Description { get; } = "List all emoji creators.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Person>("creatorNameOrID").SetRequiredState(false);
			public CommandWhoMadeList(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length > 1) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}
				ArgumentMap<Person> args = Syntax.SetContext(executionContext).Parse<Person>(argArray.ElementAtOrDefault(0));
				if (args.Arg1 != null) {
					if (args.Arg1.Member == null) {
						throw new CommandException(this, Personality.Get("cmd.err.noMemberFound"));
					}
					if (CreatorToEmojisMap.TryGetValue(args.Arg1.Member.ID.ToString(), out List<ulong> emojiIDs)) {
						EmbedBuilder builder = new EmbedBuilder {
							Title = "Emojis: " + args.Arg1.Member.FullNickname,
						};
						string desc = "";
						foreach (ulong id in emojiIDs) {
							Emoji emoji = executionContext.Server.Emojis.FirstOrDefault(emoji => emoji.ID == id);
							//desc += $"<:_:{id}>";
							desc += emoji.ToString();
						}
						builder.Description = desc;
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, builder.Build(), AllowedMentions.Reply);
					} else {
						throw new CommandException(this, "This user has not created any emojis for the server.");
					}
				} else {
					EmbedBuilder builder = new EmbedBuilder {
						Title = "Valid Emoji Creators",
					};
					string desc = "";
					foreach (string creator in CreatorToEmojisMap.Keys) {
						string creatorDisplay = creator;
						if (ulong.TryParse(creator, out ulong id)) {
							Member creatorUser = await (await User.GetOrDownloadUserAsync(id)).InServerAsync(executionContext.Server);
							creatorDisplay = $"<@!{creatorUser.ID}> ({creatorUser.FullNickname})";
						}
						desc += "• " + creatorDisplay + "\n";
					}
					builder.Description = desc;
					builder.SetFooter("You can use >> " + FullName + " <PERSON> to see the specific emojis from one of these people.");
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, builder.Build(), AllowedMentions.Reply);
				}
			}
		}

		/// <summary>
		/// Looks for the member with the given user in the given context, or if they could not be found, returns a cached version of their name.
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="inContext"></param>
		/// <returns></returns>
		public static async Task<string> GetName(Snowflake userId, BotContext inContext) {
			if (NullUsers.Contains(userId)) {
				if (ReserveNames.ContainsKey(userId)) return ReserveNames[userId] + " (User is no longer in the server, so their name was loaded from a cache instead.)"; ;
				return null;
			}

			Member mbr = await inContext.Server.GetMemberAsync(userId);
			if (mbr == null) {
				NullUsers.Add(userId);
				return await GetName(userId, inContext); // Will recycle and return from the top.
			}

			return mbr.Mention;
		}

		private static string GetEmojiName(string str) {
			int openBracket = str.IndexOf(':');
			if (openBracket == -1) return null;
			int closeBracket = str.IndexOf(':', openBracket + 1);
			if (closeBracket == -1) return null;

			return str.Substring(openBracket + 1, closeBracket - openBracket - 1);
		}

		private static string GetEmojiId(string str) {
			int openBracket = str.LastIndexOf(':');
			if (openBracket == -1) return null;
			int closeBracket = str.IndexOf('>', openBracket + 1);
			if (closeBracket == -1) return null;

			return str.Substring(openBracket + 1, closeBracket - openBracket - 1);
		}
	}
}
