using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Utility.Extension;
using EtiLogger.Logging;
using OldOriBot.Data;
using OldOriBot.Data.MemberInformation;
using OldOriBot.Data.Persistence;
using OldOriBot.Interaction;
using OldOriBot.PermissionData;
using OldOriBot.Utility.Responding;

namespace OldOriBot.CoreImplementation.Handlers {
	public class HandlerPassiveResponseSystem : PassiveHandler {

		/*
		static HandlerPassiveResponseSystem() {
			foreach (OptionSet set in ResponseTypes.Values) {
				set.BakeOptions();
			}
		}
		*/

		#region Configs

		private static readonly Random RNG = new Random();

		private DataPersistence Config => DataPersistence.GetPersistence(Context, "chatinteraction.cfg");

		/// <summary>
		/// Whether or not this <see cref="PassiveHandler"/> is active.
		/// </summary>
		private bool IsSystemEnabled => Config.TryGetType("IsEnabled", true);

		/// <summary>
		/// Whether or not this handler can trigger in all channels or just #bot-commands
		/// </summary>
		private bool AllowInAnyChannel => Config.TryGetType("AllowInAnyChannel", false);

		/// <summary>
		/// The time that a user must wait before they can get another response from the bot.
		/// </summary>
		private int CooldownTimeMS => Config.TryGetType("CooldownTimeMS", 7500);

		/// <summary>
		/// Whether or not the cooldown system is enabled.
		/// </summary>
		private bool IsCooldownEnabled => Config.TryGetType("CooldownEnabled", true);

		/// <summary>
		/// The chance of Ku chiming in.
		/// </summary>
		private double KuChance => Config.TryGetType("ChanceOfKuResponse", 1D);

		/// <summary>
		/// Force the system to believe it's march 11.
		/// </summary>
		private bool ForceBirthday => Config.TryGetType("ForceBirthday", false);

		/// <summary>
		/// A dictionary of user ID to epoch that represents when the user last used this handler.
		/// </summary>
		private static readonly Dictionary<ulong, long> MemberLastUsedEpoch = new Dictionary<ulong, long>();

		/// <summary>
		/// Returns a random boolean based on <see cref="KuChance"/>
		/// </summary>
		public bool RandomlyUseKuResponse => KuChance == 1 || RNG.NextDouble() >= 1.0 - KuChance;

		#endregion

		#region Queries

		/// <summary>
		/// A binding from (user input) to (response).
		/// </summary>
		private static readonly Dictionary<OptionSet, OptionSet> ResponseOptions = new Dictionary<OptionSet, OptionSet> {
			#region Greetings
			[new OptionSet() {
				Options = new string[] {
					"{GREETING} {ORIBOT}"
				}
			}] = new OptionSet() {
				Options = new string[] {
					"Hi, {USERPING}!",
					"Hi!",
					"Oh! How are you, {USERPING}? " + OriServerEmojis.OriHeart,
					// ":wave:",
					// As proposed by Stretch#0588 714588316845998121...
					OriServerEmojis.OriWave,
					"Hey!",
					"Good to see you, {USERPING}!" + OriServerEmojis.OriHeart,
				}
			},
			#endregion
			#region Good (time of day here)
			[new OptionSet() {
				Options = new string[] {
					"{GOODMORNING} {ORIBOT}"
				}
			}] = new OptionSet() {
				Options = new string[] {
					"Good morning!",
					"Did you have a good night's rest? " + OriServerEmojis.OriHeart,
					"Did you sleep well? " + OriServerEmojis.OriHeart,
					"Are you well-rested?",
					"Morning!",
					"Hey! Did you remember to eat your breakfast?",
					"Ready to start the day? " + OriServerEmojis.OriHype
				}
			},
			[new OptionSet() {
				Options = new string[] {
					"{GOODNIGHT} {ORIBOT}"
				}
			}] = new OptionSet() {
				Options = new string[] {
					"Goodnight!",
					"Have a good rest! " + OriServerEmojis.OriHeart,
					"I'll see you tomorrow, {USERPING}! " + OriServerEmojis.OriHeart,
					"Oh! Have a good night. " + OriServerEmojis.OriHeart,
					"Night!",
					"Sleep tight! " + OriServerEmojis.OriHeart,
				}
			},
			[new OptionSet() {
				Options = new string[] {
					"{GOODAFTERNOON} {ORIBOT}"
				}
			}] = new OptionSet() {
				Options = new string[] {
					"To you too!",
					"Good afternoon!",
					"Thanks, {USERPING} " + OriServerEmojis.OriHeart,
					"It sure is!"
				}
			},
			[new OptionSet() {
				Options = new string[] {
					"{GOODEVENING} {ORIBOT}"
				}
			}] = new OptionSet() {
				Options = new string[] {
					"To you too!",
					"Good evening!",
					"Thanks, {USERPING} " + OriServerEmojis.OriHeart,
					"It sure is!"
				}
			},
			#endregion
			#region Goodbyes
			[new OptionSet() {
				Options = new string[] {
					"{GOODBYE} {ORIBOT}",
				}
			}] = new OptionSet() {
				Options = new string[] {
					"Bye! " + OriServerEmojis.OriHeart,
					"See you later!",
					"See you soon! " + OriServerEmojis.OriHeart,
					"Will I see you soon? " + OriServerEmojis.OriCry,
					":wave: Goodbye!"
				}
			},
			#endregion
			#region Asking About the Bot
			[new OptionSet() {
				Options = new string[] {
					"{ASKTIMEPAST} {ORIBOT}"
				}
			}] = new OptionSet() {
				Options = new string[] {
					"It was good! " + OriServerEmojis.OriHype,
					"It was great!",
					"Pretty good.",
					"Not bad at all!"
				}
			},
			[new OptionSet() {
				Options = new string[] {
					"{ASKTIMENOW} {ORIBOT}"
				}
			}] = new OptionSet() {
				Options = new string[] {
					"It's going great! " + OriServerEmojis.OriHype,
					"Really good.",
					"Relaxing.",
					"It's pretty enjoyable. " + OriServerEmojis.OriHeart
				}
			},
			[new OptionSet() {
				Options = new string[] {
					"{ASKSTATUS} {ORIBOT}"
				}
			}] = new OptionSet() {
				Options = new string[] {
					"I'm doing good! Thanks for asking " + OriServerEmojis.OriHeart,
					"I'm doing well.",
					"Not bad at all!",
					"I'm pretty happy."
				}
			},
			[new OptionSet() {
				Options = new string[] {
					"{ASKSTATUSYN} {ORIBOT}"
				}
			}] = new OptionSet() {
				Options = new string[] {
					"Yep! Thanks for asking " + OriServerEmojis.OriHeart,
					"Uh-huh!",
					"Yeah! " + OriServerEmojis.OriHype
				}
			},
			[new OptionSet() {
				Options = new string[] {
					"{ASKACTIVITY} {ORIBOT}"
				}
			}] = new OptionSet() {
				Options = new string[] {
					"Oh, not much. Just relaxing!",
					"Thinking, thinking, thinking. Imagination is great!",
					"Making sure everyone here's being nice to eachother. " + OriServerEmojis.OriHeart,
					"Nothing but talking to you, I guess!"
				}
			},
			#endregion
			#region Asking about the bot (as comments)
			[new OptionSet() {
				Options = new string[] {
					"{TELLACTIVITYGOOD} {ORIBOT}",
				}
			}] = new OptionSet() {
				Options = new string[] {
					"Thanks! " + OriServerEmojis.OriHeart,
					"To you too! " + OriServerEmojis.OriHeart,
					OriServerEmojis.OriHeart,
				}
			},
			[new OptionSet() {
				Options = new string[] {
					"{FAVORITECOLOR} {ORIBOT}",
				}
			}] = new OptionSet() {
				Options = new string[] {
					"Oh... I don't know! I like greens and blues, oranges and reds, all of them really! I like all of the colors you can find in Nibel. " + OriServerEmojis.OriHeart
				}
			},
			[new OptionSet() {
				Options = new string[] {
					"{LOVE} {ORIBOT}",
				}
			}] = new OptionSet() {
				Options = new string[] {
					OriServerEmojis.OriHeart,
					"Aw, thanks {USERPING}! " + OriServerEmojis.OriHype,
					"Oh! " + OriServerEmojis.OriHeart
				}
			},
			#endregion
			#region Asking About Developer
			[new OptionSet() {
				Options = new string[] {
					"{ASKHOWETI} {ORIBOT}"
				}
			}] = new OptionSet() {
				Options = new string[] {
					"Uh..... I don't know, to be honest! Hopefully doing good.",
					"I dunno. But I do know that if he's offline, he's asleep!",
					"Pretty good! Or, so I hope " + OriServerEmojis.OriHeart,
					"You could just ask him, I guess. Oh! - But make sure no one else has pinged him about it in the past couple hours, or you might bother him " + OriServerEmojis.OriCry,
				}
			},
			[new OptionSet() {
				Options = new string[] {
					"{ASKWHATETI} {ORIBOT}"
				}
			}] = new OptionSet() {
				Options = new string[] {
					$"Knowing him? Probably making something! {OriServerEmojis.OriHype} ...Or breaking something. Or sleeping!",
					"You could just ask him, I guess. Oh! - But make sure no one else has pinged him about it in the past couple hours, or you might bother him " + OriServerEmojis.OriCry,
					"Does his status say anything? I'd look there, he sets that a lot " + OriServerEmojis.OriHeart,
					"No idea! I'm too busy sitting here with you all."
				}
			},
			#endregion
			#region Thanks
			[new OptionSet() {
				Options = new string[] {
					"{THANKS} {ORIBOT}"
				}
			}] = new OptionSet() {
				Options = new string[] {
					"Oh! You're welcome " + OriServerEmojis.OriHype,
					"Of course!",
					"It was the least I could do " + OriServerEmojis.OriHeart,
					"Sure thing!",
					"Anything for a friend " + OriServerEmojis.OriHeart
				}
			},
			#endregion
			#region Birthday
			[new OptionSet {
				Options = new string[] {
					"{BIRTHDAY} {ORIBOT}"
				}
			}] = new OptionSet {
				Options = new string[] {
					OriServerEmojis.OriHype + " :tada:",
					"Thank you!",
					"Hooray! :tada:"
				}
			}
			#endregion
		};
		private static readonly OptionSet SpecialNotBirthdayResponses = new OptionSet {
			Options = new string[] {
				"Today's not my birthday! It's on the 11th of March.",
				"I think you might have mixed up the date, it's on the 11th of March!"
			}
		};

		// When writing these, queries must be all lowercase with no punctuation.
		/// <summary>
		/// Represents a list of interchangable words e.g. hi vs hello.<para/>
		/// These aliases allow an entire set of words to be referenced with one key.<para/>
		/// It's a bit of a mess but I need to update these as people try + fail different queries. The result is some funny stuff. :P<para/>
		/// TODO: Sub-aliases to reduce the amount of stuff in these lists? e.g. a list element called "{YOU}" which collectively refers to "you", "u", "ya", etc.
		/// A cascading list of things would definitely clean this up.
		/// </summary>
		/// 

		private static readonly Dictionary<string, OptionSet> ResponseTypes = new Dictionary<string, OptionSet> {
			#region Hellos / Goodbyes
			["{GREETING}"] = new OptionSet() {
				Options = new string[] {
					"hi", "hello", "hey", "whats up", "heya", "hiya", "yo", "greetings", "sup", "whats poppin", "whats crackin",
					"howdy there", "howdy", "well howdy there", "well howdy",
					"gday", "gday 2u", "gday 2 you", "gday 2 u",
					"good day", "goodday",
					"good day to you", "gday to you", "gday to u", "good day to u", "good day 2 you", "good day 2 u", "good day 2u",
					"yello", "yellow", "yelo", "elo", "ey",
					"salutations", "henlo", "ello", "ayo", "eyo", "hio"
				}
			},
			["{GOODBYE}"] = new OptionSet() {
				Options = new string[] {
					"bye", "peace", "see ya", "later", "see you", "see ya later", "see you later", "goodbye", "cya", "im out", "im headed off", "im headed out",
					"im heading off", "im heading out", "im leaving", "ill be back later", "brb", "gtg", "g2g", "gotta go", "got to go", "im gonna go", "im going",
					"bbl", "ima dip", "gonna dip", "im gonna dip", "see u", "see u later",
					"have a good day", "have a nice day"
				}
			},
			#endregion
			#region Good (time of day)s & Asking About Bot
			["{GOODNIGHT}"] = new OptionSet() {
				Options = new string[] {
					"nightnight", "night", "night-night", "goodnight", "gnight", "bedtime", "time for bed",
					"going to bed", "going to sleep", "sleepytime", "time to sleep", "time for me to sleep",
					"off to sleep", "off to bed", "nightynight", "gonna go to bed", "gonna go to sleep"
				}
			},
			["{GOODMORNING}"] = new OptionSet() {
				Options = new string[] {
					"morning", "good morning", "gmorning", "mornin", "good mornin"
				}
			},
			["{GOODAFTERNOON}"] = new OptionSet() {
				Options = new string[] {
					"good afternoon to you", "good afternoon to ya", "good afternoon to ye", "good afternoon to u",
					"gafternoon to you", "gafternoon to ya", "gafternoon to ye", "gafternoon to u",
					"afternoon to you", "afternoon to ya", "afternoon to ye", "afternoon to u",
					"afternoon", "good afternoon", "after noon", "good after noon", "gafternoon",
				}
			},
			["{GOODEVENING}"] = new OptionSet() {
				Options = new string[] {
					"good evening to you", "good evening to ya", "good evening to ye", "good evening to u",
					"gevening to you", "gevening to ya", "gevening to ye", "gevening to u",
					"evening to you", "evening to ya", "evening to ye", "evening to u",
					"evening", "good evening", "gevening",
				}
			},
			["{ASKTIMEPAST}"] = new OptionSet() {
				Options = new string[] {
					"how was your day", "how was ur day", "how did ur day go", "how did your day go", "howd your day go", "howd ur day go", "you have a good day", "u have a good day",
					"how was your night", "how was ur night", "how did ur night go", "how did your night go", "howd your night go", "howd ur night go", "you have a good night", "u have a good night",
				}
			},
			["{ASKTIMENOW}"] = new OptionSet() {
				Options = new string[] {
					"how is your day", "how is ur day", "hows your day", "hows ur day",
					"how is your night", "how is ur night", "hows your night", "hows ur night"
				}
			},
			["{FAVORITECOLOR}"] = new OptionSet() {
				Options = new string[] {
					"what is ur favorite color", "whats ur favorite color", "what is your favorite color", "whats your favorite color",
					"tell me your favorite color", "tell me ur favorite color", "which color do you like most", "which color do u like most",
					"which color do you like the most", "which color do u like the most", "what color do you like most", "what color do u like most",
					"what color do you like the most", "what color do u like the most",
				}
			},
			#endregion
			#region Statuses / Activities
			["{ASKSTATUS}"] = new OptionSet() {
				Options = new string[] {
					"how are you", "how r u", "how are you feeling", "how r u feeling", "how are you doing", "how r u doing",
					"how are u", "how r you", "how r ya", "how are ya", "how are ye", "how r ye"
				}
			},
			["{ASKSTATUSYN}"] = new OptionSet() {
				Options = new string[] {
					"are you doing well", "are you doing good", "you doing well", "you doing good",
				}
			},
			["{ASKACTIVITY}"] = new OptionSet() {
				Options = new string[] {
					"what are you up to", "what r u up to", "whatre you up to", "whatre u up to",
					"what r u doing", "what are you doing"
				}
			},
			["{LOVE}"] = new OptionSet() {
				Options = new string[] {
					"i love", "i love you",
					"love you", "i love u", "love u",
					"i luv", "i luv you", "luv you", "luv u",
					"ily", "ly"
				}
			},
			#endregion
			#region Direct References to Individuals / Bot
			["{ASKHOWETI}"] = new OptionSet() {
				Options = new string[] {
					"how is eti", "how is xan", "hows eti", "hows xan"
				}
			},
			["{ASKWHATETI}"] = new OptionSet() {
				Options = new string[] {
					"what is eti doing", "whats eti doing", "what is xan doing", "whats xan doing",
					"what is eti up to", "whats eti up to", "what is xan up to", "whats xan up to"
				}
			},
			["{ORIBOT}"] = new OptionSet() {
				Options = new string[] {
					// ((<:)([a-z]|[A-Z]|[0-9]|_)+:)+
					"ori", "ori-o", "orio", "ori#8480", "616136907860213760", "<@616136907860213760>", "<@!616136907860213760>"
				}
			},
			#endregion
			#region Comments to the bot
			["{TELLACTIVITYGOOD}"] = new OptionSet() {
				Options = new string[] {
					"i hope you are doing well", "i hope you are doing good", "i hope u r doing well", "i hope u r doing good",
					"i hope youre doing well", "i hope youre doing good", "i hope ur doing well", "i hope ur doing good",
					"i hope your day is going well", "i hope your day is going good", "i hope ur day is going well", "i hope ur day is going good"
				}
			},
			["{THANKS}"] = new OptionSet() {
				Options = new string[] {
					"thank you", "thank u", "thx", "thanks", "thnx", "thank ya", "thank ye", "thanx", "thankies", "tanks", "tnx",
					"big mcthankies from mcspankies",
					"thx you", "thx u", "thnx you", "thnx u",
					"tnx you", "tnx u"
				}
			},
			#endregion
			#region Birthday
			["{BIRTHDAY}"] = new OptionSet {
				Options = new string[] {
					"happy birthday", "happy bday", "happy b-day"
				}
			}
			#endregion
		};

		/*
		private static readonly Dictionary<string, OptionSet> ResponseTypes = new Dictionary<string, OptionSet> {
			#region Hellos / Goodbyes
			["{GREETING}"] = new OptionSet() {
				Options = new string[] {
					"hello", "hi", "hey", "howdy", "yo", "sup", "{WHATIS} up", "{WHATIS} {GOING}", "{WHATIS} {GOING} on",
					"heya", "hiya", "greetings", "howdy",
					"yello", "yellow", "yelo", "elo", "ey",
					"salutations", "henlo", "ello", "ayo", "eyo", "hio", "hii", "hiii", // KEK
					"{WHATIS} {GOOD}"
				}
			},
			["{GOODBYE}"] = new OptionSet() {
				Options = new string[] {
					"bye", "peace", "later", "see {YOU}", "goodbye", "cya", "im out", "im headed off", "im headed out",
					"im heading off", "im heading out", "im leaving", "ill be back later", "brb", "gtg", "g2g", "gotta go", "got {TO} go", "im gonna go", "im going",
					"bbl", "ima dip", "gonna dip", "im gonna dip", "see u", "see u later",
					"have a good day", "have a nice day"
				}
			},
			#endregion
			#region Good (time of day)s & Asking About Bot
			["{GOODNIGHT}"] = new OptionSet() {
				Options = new string[] {
					"nightnight", "night", "night-night", "goodnight", "gnight", "bedtime", "time for bed",
					"going to bed", "going to sleep", "sleepytime", "time to sleep", "time for me to sleep",
					"off to sleep", "off to bed", "nightynight", "gonna go to bed", "gonna go to sleep"
				}
			},
			["{GOODMORNING}"] = new OptionSet() {
				Options = new string[] {
					"morning", "good morning", "gmorning", "mornin", "good mornin"
				}
			},
			["{GOODAFTERNOON}"] = new OptionSet() {
				Options = new string[] {
					"good afternoon to you", "good afternoon to ya", "good afternoon to ye", "good afternoon to u",
					"gafternoon to you", "gafternoon to ya", "gafternoon to ye", "gafternoon to u",
					"afternoon to you", "afternoon to ya", "afternoon to ye", "afternoon to u",
					"afternoon", "good afternoon", "after noon", "good after noon", "gafternoon",
				}
			},
			["{GOODEVENING}"] = new OptionSet() {
				Options = new string[] {
					"good evening to you", "good evening to ya", "good evening to ye", "good evening to u",
					"gevening to you", "gevening to ya", "gevening to ye", "gevening to u",
					"evening to you", "evening to ya", "evening to ye", "evening to u",
					"evening", "good evening", "gevening",
				}
			},
			["{ASKTIMEPAST}"] = new OptionSet() {
				Options = new string[] {
					"how was your day", "how was ur day", "how did ur day go", "how did your day go", "howd your day go", "howd ur day go", "you have a good day", "u have a good day",
					"how was your night", "how was ur night", "how did ur night go", "how did your night go", "howd your night go", "howd ur night go", "you have a good night", "u have a good night",
				}
			},
			["{ASKTIMENOW}"] = new OptionSet() {
				Options = new string[] {
					"how is your day", "how is ur day", "hows your day", "hows ur day",
					"how is your night", "how is ur night", "hows your night", "hows ur night"
				}
			},
			["{FAVORITECOLOR}"] = new OptionSet() {
				Options = new string[] {
					"what is ur favorite color", "whats ur favorite color", "what is your favorite color", "whats your favorite color",
					"tell me your favorite color", "tell me ur favorite color", "which color do you like most", "which color do u like most",
					"which color do you like the most", "which color do u like the most", "what color do you like most", "what color do u like most",
					"what color do you like the most", "what color do u like the most",
				}
			},
			#endregion
			#region Statuses / Activities
			["{ASKSTATUS}"] = new OptionSet() {
				Options = new string[] {
					"how are you", "how r u", "how are you feeling", "how r u feeling", "how are you doing", "how r u doing",
					"how are u", "how r you", "how r ya", "how are ya", "how are ye", "how r ye"
				}
			},
			["{ASKSTATUSYN}"] = new OptionSet() {
				Options = new string[] {
					"are you doing well", "are you doing good", "you doing well", "you doing good",
				}
			},
			["{ASKACTIVITY}"] = new OptionSet() {
				Options = new string[] {
					"what are you up to", "what r u up to", "whatre you up to", "whatre u up to",
					"what r u doing", "what are you doing"
				}
			},
			["{LOVE}"] = new OptionSet() {
				Options = new string[] {
					"i love", "i love you",
					"love you", "i love u", "love u",
					"i luv", "i luv you", "luv you", "luv u",
					"ily", "ly"
				}
			},
			#endregion
			#region Direct References to Individuals / Bot
			["{ASKHOWETI}"] = new OptionSet() {
				Options = new string[] {
					"how is eti", "how is xan", "hows eti", "hows xan"
				}
			},
			["{ASKWHATETI}"] = new OptionSet() {
				Options = new string[] {
					"what is eti doing", "whats eti doing", "what is xan doing", "whats xan doing",
					"what is eti up to", "whats eti up to", "what is xan up to", "whats xan up to"
				}
			},
			["{ORIBOT}"] = new OptionSet() {
				Options = new string[] {
					// ((<:)([a-z]|[A-Z]|[0-9]|_)+:)+
					"ori", "ori-o", "orio", "ori#8480", "616136907860213760", "<@616136907860213760>", "<@!616136907860213760>"
				}
			},
			#endregion
			#region Comments to the bot
			["{TELLACTIVITYGOOD}"] = new OptionSet() {
				Options = new string[] {
					"i hope you are doing well", "i hope you are doing good", "i hope u r doing well", "i hope u r doing good",
					"i hope youre doing well", "i hope youre doing good", "i hope ur doing well", "i hope ur doing good",
					"i hope your day is going well", "i hope your day is going good", "i hope ur day is going well", "i hope ur day is going good"
				}
			},
			["{THANKS}"] = new OptionSet() {
				Options = new string[] {
					"thank you", "thank u", "thx", "thanks", "thnx", "thank ya", "thank ye", "thanx", "thankies", "tanks", "tnx",
					"big mcthankies from mcspankies",
					"thx you", "thx u", "thnx you", "thnx u",
					"tnx you", "tnx u"
				}
			},
			#endregion
			#region Birthday
			["{BIRTHDAY}"] = new OptionSet {
				Options = new string[] {
					"happy birthday", "happy bday", "happy b-day"
				}
			},
			#endregion

			#region Substitutions
			// A space that is optional.
			["{_}"] = new OptionSet {
				Options = new string[] {
					" ", ""
				}
			},
			["{YOU}"] = new OptionSet {
				Options = new string[] {
					"you", "u", "ya", "yu", "ye"
				}
			},
			["{ARE}"] = new OptionSet {
				Options = new string[] {
					"are", "r"
				}
			},
			["{WHAT}"] = new OptionSet {
				Options = new string[] {
					"what", "wat", "whut", "wut", "wot", "whot"
				}
			},
			["{YOUR}"] = new OptionSet {
				Options = new string[] {
					"your", "yer", "yur", "ur"
				}
			},
			["{I}"] = new OptionSet {
				Options = new string[] {
					"i", "me"
				}
			},
			["{GOOD}"] = new OptionSet {
				Options = new string[] {
					"good", "gud", "guud"
				}
			},
			["{LOVE}"] = new OptionSet {
				Options = new string[] {
					"love", "luv", "wuv", "wuve"
				}
			},
			["{GOINGTO}"] = new OptionSet {
				Options = new string[] {
					"{?IAM?_}gonna", "{?IAM?_}boutta", "{?IAM?_}about to", "{I} will", "{?IAM?_}going to", "{?IAM?_}bout to", "{?IAM?_}bouta",
					"{?IAM?_}abouta", "{?IAM?_}finna", "{?IAM?_}headed"
				}
			},
			["{IAM}"] = new OptionSet {
				Options = new string[] {
					"ima", "i am", "imma", "im"
				}
			},
			["{WHATIS}"] = new OptionSet {
				Options = new string[] {
					"whats", "what is", "wats", "whuts", "wuts"
				}
			},
			["{WHATARE}"] = new OptionSet {
				Options = new string[] {
					"{WHAT}re"
				}
			},
			["{NIGHT}"] = new OptionSet {
				Options = new string[] {
					"night", "nighty"
				}
			},
			["{TO}"] = new OptionSet {
				Options = new string[] {
					"to", "ta", "2"
				}
			},
			["{SEE}"] = new OptionSet {
				Options = new string[] {
					"see", "c"
				}
			},
			["{DAY}"] = new OptionSet {
				Options = new string[] {
					"day"
				}
			},
			["{GOING}"] = new OptionSet {
				Options = new string[] {
					"goin", "going", "happenin", "happening", "poppin", "popping", "crackin", "cracking"
				}
			}
			#endregion
		};
		*/

		private static OptionSet TEST_SET = new OptionSet {
			Options = new string[] {
				"{WHATIS} {GOOD}, {IAM} {GOINGTO}"
			}
		};
		public static string[] GetAllTestResults() => TEST_SET.Options;

		/// <summary>
		/// Common queries that people use to ask about Ori's gender. Again a bit of a mess using a hardcoded list. Find a better solution, yada yada.
		/// </summary>
		private static readonly string[] AskingAboutOriGender = new string[] {
			"ori a boy or a girl",
			"ori boy or girl",

			"ori male or female",
			"ori a male or a female",

			"ori a boy or girl",
			"ori a male or female",

			"ori a girl or a boy",
			"ori girl or boy",

			"ori female or male",
			"ori a female or a male",

			"ori a girl or boy",
			"ori a female or male",

			"what is oris gender",
			"whats oris gender",
			"what is the gender of ori",
			"whats the gender of ori",
			"what gender is ori",

			"whats ori gender",
			"what is ori gender",
		};

		#endregion

		public override string Name { get; } = "Passive Response System";
		public override string Description { get; } = "Responds to a number of queries, allowing people to \"talk\" to the bot.";
		public HandlerPassiveResponseSystem(BotContext ctx) : base(ctx) { }

		public override async Task<bool> ExecuteHandlerAsync(Member executor, BotContext executionContext, Message message) {
			if (!IsSystemEnabled && executor.GetPermissionLevel() < PermissionLevel.BotDeveloper) return false;
			// Abort if disabled
			// temp edit: it only allows me rather than mods
			// Asking for gender works everywhere.
			double ogLength = message.Content.Length;
			string userText = StripPunctuation(message.Content.ToLower());
			double newLength = userText.Length;

			

			foreach (string genderQ in AskingAboutOriGender) {
				if (userText.Contains(genderQ)) {
					await ResponseUtil.RespondToAsync(message, HandlerLogger, Personality.Get("ori.gender"), mentions: AllowedMentions.Reply);
					return false;
				}
			}

			if (!AllowInAnyChannel && message.Channel.ID != Context.BotChannelID && executor.GetPermissionLevel() < PermissionLevel.Operator) return false; 
			// Abort if they need to be in bot channel and they aren't.
			// This handler should never intercept messages, so don't return true.

			// If they are under cooldown just quietly ignore them
			if (MemberLastUsedEpoch.ContainsKey(executor.ID)) {
				DateTime lastUsed = DateTime.FromBinary(MemberLastUsedEpoch[executor.ID]);
				TimeSpan latency = DateTime.UtcNow - lastUsed;
				if (latency.TotalMilliseconds < CooldownTimeMS) return false;
			}

			foreach (OptionSet options in ResponseOptions.Keys) {
				if (options.ContainsThisWithIdentifierSubs(userText)) {
					// Special catch case
					if (options.Contains("{BIRTHDAY} {ORIBOT}")) {
						// They said it's their birthday, but is it really?
						DateTimeOffset now = DateTimeOffset.UtcNow;
						if (!(now.Month == 3 && now.Day == 11) && !ForceBirthday) {
							HandlerLogger.WriteLine("§eToday is not Ori's birthday.", LogLevel.Debug);
							// "Today's not my birthday! It's on the 11th of March."
							if (IsCooldownEnabled) {
								MemberLastUsedEpoch[executor.ID] = DateTime.UtcNow.ToBinary();
							}

							string resp = SpecialNotBirthdayResponses.RandomEntry; // Grab a random entry from the responses about birthday being wrong.
							if (RandomlyUseKuResponse) {
								await ResponseUtil.RespondToAsync(message, HandlerLogger, OriServerEmojis.OriKu + ": " + Personality.GetRandomKuResponse() + $"\n\n{OriServerEmojis.OriFace}: " + resp, mentions: AllowedMentions.Reply);
								return false; // Exit here.
							} else {
								await ResponseUtil.RespondToAsync(message, HandlerLogger, resp, mentions: AllowedMentions.Reply);
								return false; // Exit here.
							}
						}
						HandlerLogger.WriteLine("§eToday IS Ori's birthday.", LogLevel.Debug);
					}

					// This optionset applies. Respond!
					// Also, move them into the cooldown list.
					if (IsCooldownEnabled) {
						MemberLastUsedEpoch[executor.ID] = DateTime.UtcNow.ToBinary();
					}

					if (ogLength / newLength > 3.25) {
						await ResponseUtil.RespondToAsync(message, HandlerLogger, "I mean, yeah, you *can* technically query the system with that, ...", attachments: new System.IO.FileInfo(@"C:\butwhy.webm"));
						return false;
					}

					string response = ResponseOptions[options].RandomEntry.Replace("{USERPING}", $"{executor.Mention}");
					if (RandomlyUseKuResponse) {
						response = OriServerEmojis.OriKu + ": " + Personality.GetRandomKuResponse() + $"\n\n{OriServerEmojis.OriFace}: " + response;
					}
					await ResponseUtil.RespondToAsync(message, HandlerLogger, response, mentions: AllowedMentions.Reply);
					return false; // Exit the function
				}
			}

			return false;
		}

		#region Core

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string StripPunctuation(string input) {
			return input.Replace(",", "").Replace(".", "").Replace("!", "").Replace("?", "").Replace("~", "").Replace("'", "");
		}

		/// <summary>
		/// Gets the identifier for the specified word, e.g "hi" turns into "{GREETING}". If no identifier is found, it returns the input text.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string GetIdentifierFor(string text) {
			foreach (OptionSet set in ResponseTypes.Values) {
				if (set.Contains(text)) {
					return ResponseTypes.KeyOf(set);
				}
			}
			return text;
		}

		/// <summary>
		/// Some queries are multi-word, so this looks at the words in their entirety and sees if this word is the start of an identifier.<para/>
		/// Returns 1 if the current option's word (determined by index) is equal to the input text.<para/>
		/// Returns 2 if index > the split option's word count, which means that all previous queries matched because we call this with indices > 0 if there was a match already.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public static byte CouldBeIdentifierFor(string text, int index = 0) {
			foreach (OptionSet set in ResponseTypes.Values) {
				foreach (string opt in set.Options) {
					string[] splitOpt = opt.Split(' ');
					if (index >= splitOpt.Length) return 2;
					if (splitOpt[index] == text) return 1;
				}
			}
			return 0;
		}

		public static string ReplaceWithIdentifiers(string text) {
			foreach (OptionSet set in ResponseTypes.Values) {
				string key = ResponseTypes.KeyOf(set); // An identifier word e.g. {GREETING}
				foreach (string opt in set.Options) {
					// opt is one of the queries that can trigger it.
					int idx = text.IndexOf(opt);
					if (idx != -1) {
						// Found it! Now one thing to immediately test is if the start and end of this are spaces, OR if the start and end are the boundaries of the text itself.
						// This will cause the system to avoid replacing the wrong things (e.g. "your" picking up "yo")

						int beforeIdx = idx - 1; // What's the character index of the character before this text?
						int afterIdx = idx + opt.Length; // And the character after?
						if (beforeIdx < 0 || afterIdx >= text.Length) {


							// If they are both at the bounds then the entire entered text qualifies for this.
							// That is, the input text is something like "Hello", which in itself is a query for "{GREETING}".
							if (beforeIdx < 0 && afterIdx >= text.Length) {
								// ...And in this case, just replace it directly.
								return text.Replace(opt, key);
							}

							// In this case, the word is either at the start or end. Special handling time.
							// Namely, can we isolate it with spaces?
							if (beforeIdx >= 0 && afterIdx < text.Length) {
								// TODO: Not sure what I'm doing here.
								// If I'm not mistaken, it's in case the start of the query has a space (I don't remember why that would happen)
								if (text.ElementAt(beforeIdx) == ' ') {
									// Space! We're good. Since the start of the text is the start of the query, this means we can replace it.
									return text.Replace(opt, key);
								}

							} else if (beforeIdx < 0 && afterIdx < text.Length) {
								// In this case, the query is at the start of the string itself, so the only space would be after it.
								if (text.ElementAt(afterIdx) == ' ') {
									return text.Replace(opt, key);
								}
							}
						} else {
							// In this case there's spaces surrounding both sides.
							if (text.ElementAt(beforeIdx) == ' ' && text.ElementAt(afterIdx) == ' ') {
								// There's spaces surrounding the text.
								return text.Replace(opt, key);
							}
						}
					}
				}
			}
			return text;
		}

		/// <summary>
		/// For construction of the scalable system, this looks at certain queries such as {IAM} when used in other lines, and expands it into all possible variants of that line.<para/>
		/// Optional entries can be surrounded by question marks: <c>{?IAM?}</c><para/>
		/// To include a space when the optional entry is included, append a <c>_</c> between the question mark and table bracket: <c>{?IAM?_}</c>
		/// </summary>
		/// <returns></returns>
		[Obsolete]
		protected static string[] CreateAllSubstitutesIn(string entry) {
			Match match = Regex.Match(entry, @"(\{{1}_?\??)(.+)(\??_?\}{1})");
			if (!match.Success) {
				return new string[] { entry };
			}

			return CreateAllSubstitutesIn(new List<string>() { entry }).ToArray();
		}

		[Obsolete]
		private static List<string> CreateAllSubstitutesIn(List<string> entries) {
			List<string> variants = new List<string>();

			foreach (string entry in entries) {
				Match match = Regex.Match(entry, @"(\{{1}_?\??)([A-Za-z0-9]+)(\??_?\}{1})");
				if (!match.Success) break;

				// friendly self reminder that groups[0] is the whole match
				string prefix = match.Groups[1].Value;
				string content = match.Groups[2].Value;
				string suffix = match.Groups[3].Value;

				// {?IAM?_}
				// prefix: {?
				// content: IAM
				// suffix: ?_}
				bool isOptional = prefix.EndsWith('?') || suffix.StartsWith('?');
				if (isOptional) {
					if (!(prefix.EndsWith('?') && suffix.StartsWith('?'))) {
						throw new FormatException($"The given format tag ({match.Groups[0].Value}) is invalid: ? symbols are not present on both sides.");
					}
				}
				bool startsWithSpace = prefix.StartsWith("{_");
				bool endsWithSpace = prefix.StartsWith("_}");
				if ((startsWithSpace || endsWithSpace) && !isOptional) {
					throw new FormatException($"The given format tag ({match.Groups[0].Value}) is invalid: Cannot use space indicators (_) on non-optional tags.");
				}

				if (!isOptional) {
					// Easy. Replace verbatim.
					string tag = "{" + content + "}";
					if (!ResponseTypes.TryGetValue(tag, out OptionSet responseSet)) {
						throw new FormatException($"The given format tag ({tag}) was unable to be resolved! Did you forget to create it? Did you make a typo?");
					}
					foreach (string option in responseSet.Options) {
						variants.Add(entry.Replace(tag, option));
					}
				} else {
					// Not so easy. Replace verbatim, but also omit.
					string tag = "{" + content + "}";
					if (!ResponseTypes.TryGetValue(tag, out OptionSet responseSet)) {
						throw new FormatException($"The given format tag ({tag}) was unable to be resolved! Did you forget to create it? Did you make a typo?");
					}
					foreach (string option in responseSet.Options) {
						string newOption = option;
						if (startsWithSpace) newOption = " " + newOption;
						if (endsWithSpace) newOption += " ";
						variants.Add(entry.Replace(tag, newOption));
						variants.Add(entry.Replace(tag, ""));
					}
				}

				variants = CreateAllSubstitutesIn(variants);
			}

			return variants;
		}

		#endregion

		/// <summary>
		/// Represents possible queries to search for bot interaction.
		/// </summary>
		private class OptionSet {

			/// <summary>
			/// The possible strings that match up to this query.
			/// </summary>
			public string[] Options { get; set; }
			/*{
				get {
					if (!_hasAllOptions) {
						_options = CreateAllSubstitutesIn(_options.ToList()).ToArray();
						_hasAllOptions = true;
					}
					return _options;
				}
				set => _options = value;
			}

			private string[] _options = null;
			private bool _hasAllOptions = false;
			*/
			/// <summary>
			/// Intended for use with in-line constructors.
			/// </summary>
			public OptionSet() { }

			/// <summary>
			/// Create a new <see cref="OptionSet"/> from the specified array of strings.
			/// </summary>
			/// <param name="queries"></param>
			public OptionSet(string[] queries) {
				Options = queries;
			}

			[Obsolete]
			public void BakeOptions() {
				Options = CreateAllSubstitutesIn(Options.ToList()).ToArray();
			}

			/// <summary>
			/// Returns whether or not this <see cref="OptionSet"/> contains the specified string, with the check optionally being case sensitive.
			/// </summary>
			/// <param name="query"></param>
			/// <param name="caseSensitive"></param>
			/// <returns></returns>
			public bool IsThisWithIdentifierSubs(string query, bool caseSensitive = false) {
				string[] querySplit = query.Split(' ');
				string newQuery = "";
				foreach (string word in querySplit) {
					if (newQuery != "") newQuery += ' ';
					newQuery += GetIdentifierFor(word);
				}
				foreach (string opt in Options) {
					if (caseSensitive) {
						if (opt == newQuery) return true;
					} else {
						if (opt.ToLower() == newQuery.ToLower()) return true;
					}
				}
				return false;
			}

			/// <summary>
			/// The opposite counterpart to <see cref="IsThisWithIdentifierSubs(string, bool)"/> which tests if the input text contains this option.
			/// </summary>
			/// <param name="query"></param>
			/// <param name="caseSensitive"></param>
			/// <returns></returns>
			public bool ContainsThisWithIdentifierSubs(string query, bool caseSensitive = false) {
				//string replaced = ReplaceWithIdentifiers(query);
				// XanBotLogger.WriteDebugLine("STOCK ENTRY: §8" + query);
				// XanBotLogger.WriteDebugLine(replaced);
				string[] querySplit = query.Split(' ');
				string newQuery = "";
				//int i = 0;
				//string accumulatedWord = "";
				foreach (string word in querySplit) {
					if (newQuery != "") newQuery += ' ';
					string newThing = GetIdentifierFor(word);
					newQuery += newThing;
				}

				// Now some queries have multiple words. Get those.
				newQuery = ReplaceWithIdentifiers(newQuery);

				foreach (string opt in Options) {
					if (caseSensitive) {
						if (newQuery.Contains(opt)) return true;
					} else {
						if (newQuery.ToLower().Contains(opt.ToLower())) return true;
					}
				}
				return false;
			}

			public bool Contains(string query, bool caseSensitive = false) {
				if (caseSensitive) return Options.Contains(query);

				if (!caseSensitive) query = query.ToLower();
				for (int idx = 0; idx < Options.Length; idx++) {
					string contained = Options[idx];
					if (!caseSensitive) contained = contained.ToLower();
					if (query == contained) return true;
				}
				return false;
			}

			public string RandomEntry => Options.Random();

			public static bool operator ==(OptionSet alpha, OptionSet bravo) => alpha.Equals(bravo);

			public static bool operator !=(OptionSet alpha, OptionSet bravo) => !alpha.Equals(bravo);

			public override bool Equals(object obj) => obj is OptionSet optionSet && Equals(optionSet);

			public override int GetHashCode() => HashCode.Combine(Options);

			public bool Equals(OptionSet other) {
				if (ReferenceEquals(this, other)) return true;
				if (other is OptionSet && other.Options.SequenceEqual(Options)) {
					return true;
				}
				return false;
			}

		}
	}
}
