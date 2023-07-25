using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.Data.MemberInformation;
using OldOriBot.Data.Persistence;
using OldOriBot.Interaction;
using OldOriBot.PermissionData;
using OldOriBot.Utility;

namespace OldOriBot.CoreImplementation.Handlers {
	public class HandlerProfanityFilter : PassiveHandler {

		#region Yucky Words

		/// <summary>
		/// Words that mute if explicitly defined, but warn in mod channels if they are partial.
		/// </summary>
		private static readonly string[] ReallyBadWords = new string[] {
			// These are here because keyboard mashing has an oddly high occurrence rate of these.
			"fag",
			"fags",
		};

		/// <summary>
		/// Words that will trigger a mute even if partially included.
		/// </summary>
		private static readonly string[] ReallyBadWorksThatMuteForPartialToo = new string[] {
			"nigga",
			"nigger",
			"niggas",
			"niggers",

			"nlgger",
			"nlgga",
			"nlggas",
			"nlggers",

			//"fag",
			"faggot",
			"fagit",
			//"fags",
			"faggots",
			"fagits",

			"whore",
			"slut",
		};

		/// <summary>
		/// Words that only mute if they are explicitly defined and do nothing if they are partial.
		/// </summary>
		private static readonly string[] BadWordsThatDontWarnForPartial = new string[] {
			// lookatthenameofthecerealbox xdddddd
			"cum",
			"cummies",
			"jizz",
			"semen",
			"sperm",
			"spermies",

			// secks and forry stuff
			"yiff",
			"sex",
			"vore",
			"penis",
			"vagina",

			// shock sites or porn sites
			"2 girls 1 cup",
			"two girls one cup",
			"2 girls one cup",
			"two girls 1 cup",

			"2girls1cup",
			"twogirlsonecup",
			"2girlsonecup",
			"twogirls1cup",

			"goatse",
			"goatsecs",

			"e621",
			"pornhub",
			"redtube",
			"furaffinity",
			"fur affinity",
			"esix",
			"e-six",

			"r34",
			"rule34",
			"rule 34"
		};

		/// <summary>
		/// URLs that are straight up not allowed.
		/// </summary>
		private static readonly string[] PornOrShockSites = new string[] {
			"e621.net",
			"pornhub.com",
			"rule34.xxx",
			"furaffinity.net",
			"boob.bot",
			"goatse.cs",
			"rotten.com",
			"bestgore.com",
			"goregrish.com",
			"nowthatsfuckedup.com",
			"manbeef.com",
			".xxx",
		};

		/// <summary>
		/// Urls that lead to pirated content or exploits of some system that runs Discord (be it PC or mobile, whatever)
		/// </summary>
		private static readonly string[] PirateAndHackUrls = new string[] {
			@"\\.\globalroot\device\condrv\kernelconnect",
			"steamunlocked.net/ori-and-the-will-of-the-wisps-free-download",
			"igg-games.cc/20923-ori-and-the-will-of-the-wisps-free-download.html",

			/*
			"discocrdapp.com",
			"discocrd.com",
			"discordgift.app",
			"discord-app.co.uk",
			"dlscordgift.com",
			*/
		};

		/// <summary>
		/// File extensions that are not allowed when in lockdown.
		/// </summary>
		private static readonly string[] ProhibitedExtensions = new string[] {
			".exe",
			".zip",
			".rar",
			".7z",
			".app",
			".bat",
			".sh",
			".js"
		};

		/// <summary>
		/// Unwanted collector's edition content.
		/// </summary>
		private static readonly string[] CEStuff = new string[] {
			"youtube.com/watch?v=J0W5oxXVvL0",
			"youtu.be/J0W5oxXVvL0",
		};

		/// <summary>
		/// Obvious spoilers
		/// </summary>
		private static readonly string[] Spoilers = new string[] {
			"ori dies",
			"ori turns into a tree",
			"ori becomes a tree",
			"ori becomes tree",
			"ku dies",
			"shriek kills ku",
			"shriek dies",
			"kuro dies",
			"kuro sacrifices herself"
		};

		#endregion

		#region Personal Garbage

		// all lower
		// includes tests and actual garbage
		// and memes lol
		public static readonly string[] DeleteIfFound = new string[] {
			":etipineapplepizza:",
			":purospeen:",
			":puro_clubpenguin:",
			":ori_thorsty:",
		};

		#endregion

		/// <summary>
		/// Data persistence for the anti spam system
		/// </summary>
		protected DataPersistence SystemPersistence { get; }

		/// <summary>
		/// Whether or not to filter links that are suspiciously similar to (but not equal to) official Discord URLs.
		/// </summary>
		public bool DiscordLinkFiltering => SystemPersistence.TryGetType("FilterLinksSimilarToDiscord", true);

		/// <summary>
		/// Whether or not to send an alert if someone goes to aggie.io, because people have a knack for drawing rule-breaking art every time.
		/// </summary>
		public bool NotifyForAggieLinks => SystemPersistence.TryGetType("NotifyForAggieLinks", true);

		/// <summary>
		/// Whether or not to run on the mods.
		/// </summary>
		public bool RunOnMods => SystemPersistence.TryGetType("RunOnMods", false);

		/*static HandlerProfanityFilter() {
			List<string> pirateAndHackURLs = new List<string>() {
				@"\\.\globalroot\device\condrv\kernelconnect",
				"steamunlocked.net/ori-and-the-will-of-the-wisps-free-download",
				"igg-games.cc/20923-ori-and-the-will-of-the-wisps-free-download.html",
		*/
		/*
		"discocrdapp.com",
		"discocrd.com",
		"discordgift.app",
		"discord-app.co.uk",
		"dlscordgift.com",
		*/
		//	};

		/*
		for (char c = 'a'; c < 'z'; c++) {
			// TOP KEK
			pirateAndHackURLs.Add("discord" + c + ".gift");
			pirateAndHackURLs.Add("dlscord" + c + ".gift");
		}
		*/
		//}

		//language=regex
		private const string FIND_URL_REGEX = @"https?:\/\/(.+\.)?([-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,4})\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)";

		public override string Name { get; } = "Profanity Filter";
		public override string Description { get; } = "watch your profanity,,,,";
		public override bool RunOnCommands { get; } = true;
		public HandlerProfanityFilter(BotContext ctx) : base(ctx) {
			SystemPersistence = DataPersistence.GetPersistence(ctx, "filter.cfg");
		}


		/// <summary>
		/// Since the scambot dumps a link in every channel possible, it causes the bot to spam the emergency log.
		/// Prevent this by caching who was just muted.
		/// </summary>
		private readonly Dictionary<Snowflake, DateTimeOffset> RecentlyMutedScammerIDs = new Dictionary<Snowflake, DateTimeOffset>();

		/// <summary>
		/// Since the scambot dumps a link in every channel possible, it causes the bot to spam delete requests.
		/// This is used to limit them and prevent Discord from getting upset at the extreme flood of delete requests
		/// from the bot.
		/// </summary>
		private readonly Dictionary<Snowflake, int> ScammerMessagesToDelete = new Dictionary<Snowflake, int>();

		public override async Task<bool> ExecuteHandlerAsync(Member executor, BotContext executionContext, Message message) {
			if (executor.GetPermissionLevel() >= PermissionLevel.Operator && !RunOnMods) return false;
			MemberMuteUtility muter = MemberMuteUtility.GetOrCreate(executionContext);

			string content = message.Content.ToLower();
			

			string messageLink = message.JumpLink.ToString();
			bool hasWordIncluded = false;
			List<string> containedBadWords = new List<string>();

			foreach (string word in ReallyBadWorksThatMuteForPartialToo) {
				if (content.Contains(word)) {
					// If it has it discretely, mute the user and log the occurrence.
					await message.DeleteAsync($"Message contained content that was not suitable for the server. Content: {word}");
					await muter.MuteMemberAsync(executor, TimeSpan.FromDays(7), "Please use respectful language, and do not discuss topics that are not suitable for the server!");
					await executionContext.ModerationLog.SendMessageAsync($"User <@!{executor.ID}> {executor.ID} sent a message containing unwanted language that is not suitable for the server! Contained language: `[{word}]`", null, AllowedMentions.AllowNothing);
					return true;
				}
			}

			foreach (string word in ReallyBadWords) {
				if (content.Contains(word)) {
					hasWordIncluded = true;
					containedBadWords.Add(word);
				}
			}
			bool hasWordDiscretely = TextContainsDiscreteWord(content, containedBadWords);
			if (hasWordIncluded || hasWordDiscretely) {
				// It is there in one of both ways.
				if (hasWordDiscretely) {
					// If it has it discretely, mute the user and log the occurrence.
					await message.DeleteAsync($"Message contained content that was not suitable for the server. Content: {ElementsOf(containedBadWords)}");
					await muter.MuteMemberAsync(executor, TimeSpan.FromDays(7), "Please use respectful language, and do not discuss topics that are not suitable for the server!");
					await executionContext.ModerationLog.SendMessageAsync($"User <@!{executor.ID}> {executor.ID} sent a message containing unwanted language that is not suitable for the server! Contained language: `[{ElementsOf(containedBadWords)}]`", null, AllowedMentions.AllowNothing);
				} else if (hasWordIncluded) {
					// And if we make it here, it has it implicitly.
					await executionContext.ModerationLog.SendMessageAsync($"ALERT: User <@!{executor.ID}> {executor.ID} sent a message that might contain profanity or unwanted content! Here's the contained words: `[{ElementsOf(containedBadWords)}]`\n\nMessage: " + messageLink, null, AllowedMentions.AllowNothing);
				}
				return true;
			}

			foreach (string urlBase in PornOrShockSites) {
				if (content.Contains(urlBase)) {
					await message.DeleteAsync("Message contained content that was not suitable for the server.");
					await muter.MuteMemberAsync(executor, TimeSpan.FromDays(30), "Do not post links to content that is not suitable for the server!");
					await executionContext.ModerationLog.SendMessageAsync($"User <@!{executor.ID}> {executor.ID} sent a message containing unwanted content that is not suitable for the server! Classification: Known pornographic or shock content URL.", null, AllowedMentions.AllowNothing);
					return true;
				}
			}

			foreach (string urlBase in PirateAndHackUrls) {
				if (content.Contains(urlBase)) {
					await message.DeleteAsync("Message contained content that was not desired.");
					await muter.MuteMemberAsync(executor, TimeSpan.FromDays(1), "Do not post links to pirated or malicious content!");
					await executionContext.ModerationLog.SendMessageAsync($"User <@!{executor.ID}> {executor.ID} sent a message containing unwanted content that is not suitable for the server! Classification: Piracy / Malicious URL.", null, AllowedMentions.AllowNothing);
					return true;
				}
			}

			// NEW: Let's use the LARGE BRAIN 6000 IQ strat against the scam links
			// PercentageLevenshteinDistance.GetSimilarityPercentage

			if (DiscordLinkFiltering) {
				MatchCollection links = Regex.Matches(content, FIND_URL_REGEX);
				if (links.Count > 0) {
					string spaceless = content.Replace(" ", "");
					if (spaceless.Contains("freenitro") || spaceless.Contains("nitroforfree") || spaceless.Contains("freediscordnitro")) {
						// The bottom of the barrel, absurdly lazy checks. Trivial to get around.
						// I'm talking so easy to get around that if your small brain decided to hang itself from the tall ceiling that is your skull, it'd still
						// be able to get around these.
						// But hey, go for the easy ones.
						bool canAct = true;
						if (RecentlyMutedScammerIDs.ContainsKey(executor.ID)) {
							canAct = (DateTimeOffset.UtcNow - RecentlyMutedScammerIDs[executor.ID]).TotalSeconds > 10;
						}
						if (canAct) {
							RecentlyMutedScammerIDs[executor.ID] = DateTimeOffset.UtcNow;
							await muter.MuteMemberAsync(executor, TimeSpan.FromDays(1), $"User {executor.Mention} {executor.ID} sent a message that could contain potential unwanted activity (such as being a Nitro scam). Reason: Tripped the bottom-of-the-barrel 0.2 IQ text search (which, on that note, if this *wasn't* done for a joke and it's an actual attempt at a scam, then congratulations. you're a failure.)");
						}
						if (!ScammerMessagesToDelete.ContainsKey(executor.ID)) {
							ScammerMessagesToDelete[executor.ID] = 1;
						} else {
							ScammerMessagesToDelete[executor.ID]++;
						}
						await Task.Delay((ScammerMessagesToDelete[executor.ID] * 500) + 250);
						await message.DeleteAsync("Automated scam detection believes that this may be a fake discord domain.");
						ScammerMessagesToDelete[executor.ID]--;
						return true;
					}
				}
				foreach (Match link in links) {
					if (link.Groups.Count >= 3) {
						Group baseUrl = link.Groups[2];
						string raw = baseUrl.Value.Replace(".", "");
						if (raw == "discordcom" || raw == "discordappcom" || raw == "discordappnet" || raw == "discordgift" || raw == "discorddev" || raw == "discordgg") {
							continue; // This is valid: discord.com, discordapp.com, discord.gift, discord.dev are all official domains
									  // Skip these.
						}
						double similarity = PercentageLevenshteinDistance.GetSimilarityPercentage(raw, "discordcom");
						bool tooSimilar = similarity > 0.85D;
						string tripped = "discord.com";
						if (!tooSimilar) {
							similarity = PercentageLevenshteinDistance.GetSimilarityPercentage(raw, "discordappcom");
							tooSimilar = similarity > 0.85D;
							tripped = "discordapp.com";
						}
						if (!tooSimilar) {
							similarity = PercentageLevenshteinDistance.GetSimilarityPercentage(raw, "discordappnet");
							tooSimilar = similarity > 0.85D;
							tripped = "discordapp.net";
						}
						if (!tooSimilar) {
							similarity = PercentageLevenshteinDistance.GetSimilarityPercentage(raw, "discordgift");
							tooSimilar = similarity > 0.85D;
							tripped = "discord.gift";
						}
						if (!tooSimilar) {
							similarity = PercentageLevenshteinDistance.GetSimilarityPercentage(raw, "discorddev");
							tooSimilar = similarity > 0.85D;
							tripped = "discord.dev";
						}
						if (tooSimilar) {
							bool canAct = true;
							if (RecentlyMutedScammerIDs.ContainsKey(executor.ID)) {
								canAct = (DateTimeOffset.UtcNow - RecentlyMutedScammerIDs[executor.ID]).TotalSeconds > 10;
							}
							if (canAct) {
								RecentlyMutedScammerIDs[executor.ID] = DateTimeOffset.UtcNow;
								await muter.MuteMemberAsync(executor, TimeSpan.FromDays(1), $"User {executor.Mention} {executor.ID} sent a message that could contain potential unwanted activity (such as being a Nitro scam). Please check {executionContext.MessageBehaviorLog.Mention} for the content contained within this message. Message similarity: {raw} => {tripped} has a similarity rating of {similarity}. Seeing the recent behavior of scambots, assume this message was sent to every channel in the server.");
							}
							if (!ScammerMessagesToDelete.ContainsKey(executor.ID)) {
								ScammerMessagesToDelete[executor.ID] = 1;
							} else {
								ScammerMessagesToDelete[executor.ID]++;
							}
							await Task.Delay((ScammerMessagesToDelete[executor.ID] * 500) + 250);
							await message.DeleteAsync("Automated scam detection believes that this may be a fake discord domain.");
							ScammerMessagesToDelete[executor.ID]--;
							return true;
						}
					}
				}
			}

			foreach (string urlBase in CEStuff) {
				if (content.Contains(urlBase)) {
					await Task.Delay(100); // This may prevent desyncs where some people see the message.
					await message.DeleteAsync("Contains unwanted link to collector's edition exclusives.");
					await executor.TrySendDMAsync("Please do not link sources of exclusive content from the Collector's Edition here! It's not called a *Collector's Edition* so that it can be distributed freely - please respect those that paid for the exclusive content by not spreading pirated or otherwise illegitimate sources of content.");
					await executionContext.ModerationLog.SendMessageAsync($"User <@!{executor.ID}> {executor.ID} sent a message containing unwanted content that is not suitable for the server! Classification: Collectors Edition assets.", null, AllowedMentions.AllowNothing);
					return true;
				}
			}

			if (content.Contains("aggie.io")) {
				if (NotifyForAggieLinks) {
					/*
					User me = await User.GetOrDownloadUserAsync(114163433980559366);
					await me.TrySendDMAsync("Guy sent a message with an aggie link. Here's the jump: " + messageLink);
					// ^ Don't return anything because this doesn't do anything with the message, it's passive.
					*/
					await message.DeleteAsync("Aggie link, which requires mod authorization.");
					await message.AuthorMember?.TrySendDMAsync("Hey! I saw that you posted an aggie.io link. While this is OK, recent events have required us to be more strict on security. Please configure your room to **not** allow anonymous artists (in the settings, set all permissions to trusted instead of all), and keep track of who is who. After you're done, DM a moderator saying that you need the room link posted.\n\nBy hosting an aggie room, you agree that you will enforce the server's rules, and that if any inappropriate drawings are made, *you* will be held responsible and face the punishment (if the artist who drew it cannot be identified!)");
					return true;
				}
			}

			foreach (string garbage in DeleteIfFound) {
				// personal stuff, don't mute or anything
				if (content.Contains(garbage)) {
					await message.DeleteAsync("Unwanted message content (emoji). Not severe enough for punishment or warning.");
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Takes a list of strings and returns a text representation split by commas.
		/// </summary>
		/// <param name="list">The list of text.</param>
		/// <returns></returns>
		private static string ElementsOf(IEnumerable<string> list) {
			string output = "";
			foreach (string str in list) {
				output += str + ", ";
			}
			return output[0..^2]; // Remove the last ,
		}

		/// <summary>
		/// A regex sequence that targets: !"#$%&'()*+,-./ :;<=>?@ [\]^_` (newline)(carriage return)(tab)
		/// </summary>
		// language=regex
		private const string RegexKeyword = @"([!-/]|[:-@]|[\[-`]|\t|\n|\r)+";

		private static bool TextContainsDiscreteWord(string text, List<string> containerList) {
			text = " " + text + " "; // This is a very hacky method of ensuring every word is surrounded in a space.
			string splitText = Regex.Replace(text, RegexKeyword, " ");
			bool hasWord = false;
			foreach (string word in ReallyBadWords) {
				if (splitText.Contains(" " + word + " ")) {
					hasWord = true;
					if (!containerList.Contains(word)) containerList.Add(word);
				}
			}
			foreach (string word in BadWordsThatDontWarnForPartial) {
				if (splitText.Contains(" " + word + " ")) {
					hasWord = true;
					if (!containerList.Contains(word)) containerList.Add(word);
				}
			}
			return hasWord;
		}
	}
}
