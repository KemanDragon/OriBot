using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiLogger.Logging;
using OldOriBot.Data.MemberInformation;
using OldOriBot.Data.Persistence;
using OldOriBot.Interaction;
using OldOriBot.PermissionData;
using OldOriBot.Utility;
using OldOriBot.Utility.Enumerables;
using OldOriBot.Utility.Responding;

namespace OldOriBot.CoreImplementation.Handlers {
	public class HandlerAntiSpamSystem : PassiveHandler {

		public override string Name { get; } = "Spam Countermeasures";
		public override string Description { get; } = "Appends systems that work to reduce malicious spam from the server.";
		public override bool RunOnCommands { get; } = true;

		/// <summary>
		/// Data persistence for the anti spam system
		/// </summary>
		protected DataPersistence SystemPersistence { get; }

		#region Message Strings

		public const string WARNING_PREFIX = "{0} You've triggered the spam countermeasures system and have gained an infraction!\n\n";

		public const string REMINDER_INFRACTIONS_TEMP = "\n\n{1} Infraction ratings expire after a prolonged amount of time (the exact time will not be told), so this is not a permanent value. Don't worry if it was an accident.";

		public const string WARNING_SENT_INVITE_LINK = WARNING_PREFIX + "**Reason:** Do not post Discord invite links to any server but the offical Ori the Game server! This server is not a place to advertise your own communities." + REMINDER_INFRACTIONS_TEMP;

		public const string WARNING_MAX_PING_SATURATION = WARNING_PREFIX + "**Reason:** You've sent too many messages that contain pings in a short timespan. Please avoid sending messages that repeatedly ping users." + REMINDER_INFRACTIONS_TEMP;

		public const string WARNING_MAX_EMOJI_SATURATION = WARNING_PREFIX + "**Reason:** The latest messages you sent have too many emojis in total. Please avoid sending messages that contain excessive amounts of emojis." + REMINDER_INFRACTIONS_TEMP;

		public const string WARNING_MAX_TEXT_SATURATION = WARNING_PREFIX + "**Reason:** You've sent too many messages with long content!" + REMINDER_INFRACTIONS_TEMP;

		public const string WARNING_REPEATED_CONTENT = WARNING_PREFIX + "**Reason:** You've sent the same message repeatedly. Please avoid sending messages with the same content several times in a row." + REMINDER_INFRACTIONS_TEMP;

		public const string WARNING_REPEATED_PINGS = WARNING_PREFIX + "**Reason:** You've sent too many messages in a row that each contain a ping. Please avoid sending messages that repeatedly ping users." + REMINDER_INFRACTIONS_TEMP;

		public const string WARNING_TOO_FAST = WARNING_PREFIX + "**Reason:** You're sending messages way too fast. Slow down! __You have been put under a brief cooldown time -- You will be unable to send messages for a few seconds after receiving this.__" + REMINDER_INFRACTIONS_TEMP;

		public const string WARNING_TOO_MANY_EMOJIS = WARNING_PREFIX + "**Reason:** You've sent too many emojis in your latest message." + REMINDER_INFRACTIONS_TEMP;

		#endregion

		#region Configuration

		/// <summary>
		/// Whether or not the system is enabled.
		/// </summary>
		public bool Enabled => SystemPersistence.TryGetType("IsEnabled", true);

		/// <summary>
		/// The amount of messages the system keeps track of when judging for spam.
		/// </summary>
		//public int NumTrackedMessages => SystemPersistence.TryGetType("NumTrackedMessages", 10);
		public int NumTrackedMessages => 10;

		/// <summary>
		/// True if the anti-spam system uses a prototype frequency-based system rather than only testing consecutive messages.<para/>
		/// If this is true, it tests frequency *on top of* consecutive messages, so consecutive testing is not removed.
		/// </summary>
		public bool UseFrequencyBasedSystem => SystemPersistence.TryGetType("UseFrequencySystem", true);

		/// <summary>
		/// True if the emoji saturation system is enabled, which looks at what % of content is emojis to gauge spam.
		/// </summary>
		public bool UseEmojiSystems => SystemPersistence.TryGetType("UseEmojiSystems", true);

		/// <summary>
		/// True if the character saturation system is enabled, which detects recent message size.
		/// </summary>
		public bool UseCharacterSaturationSystems => SystemPersistence.TryGetType("UseCharacterSaturationSystems", true);

		/// <summary>
		/// In seconds with a decimal, this is the shortest amount of time between messages that a user can send.
		/// </summary>
		public double MessageSpeedThreshold => SystemPersistence.TryGetType("MessageSpeedThresholdSeconds", 0.55);

		/// <summary>
		/// The maximum amount of identical messages that can be sent before the system trips.
		/// </summary>
		public int MaxConsecutiveIdenticalMessages => SystemPersistence.TryGetType("MaxConsecutiveIdenticalMessages", 3);

		/// <summary>
		/// The maximum amount of messages sent in rapid succession before the system trips.
		/// </summary>
		public int MaxFastMessagesBeforeDeletion => SystemPersistence.TryGetType("MaxFastMessagesBeforeDeletion", 5);

		/// <summary>
		/// The amount of time a user must wait before they are able to send messages again if they hit the rate limit (in seconds)
		/// </summary>
		public double UserTooFastCooldownTime => SystemPersistence.TryGetType("UserTooFastCooldownTimeSeconds", 3.5);

		/// <summary>
		/// The maximum amount of infractions a user can tack up before they're just flat out muted.
		/// </summary>
		public int MaximumInfractionsBeforeMute => SystemPersistence.TryGetType("MaxInfractionsBeforeMute", 3);

		/// <summary>
		/// The amount of seconds before a logged infraction is removed.
		/// </summary>
		public int InfractionExpirationTime => SystemPersistence.TryGetType("InfractionExpirationTimeSeconds", 600);

		/// <summary>
		/// The maximum number of pings a single message can contain.
		/// </summary>
		public int MaximumNumberOfPingsInMessage => SystemPersistence.TryGetType("MaximumNumberOfPingsInMessage", 10);

		/// <summary>
		/// The amount of times that the person can send a ping in a row.
		/// </summary>
		public int MaximumNumberOfConsecutiveMessagesWithPings => SystemPersistence.TryGetType("MaxConsecutiveMessagesWithPings", 3);

		/// <summary>
		/// Whether or not posting instant invites to servers other than this one is allowed.
		/// </summary>
		public bool AllowPostingInvites => SystemPersistence.TryGetType("AllowPostingInvites", false);

		/// <summary>
		/// The maximum % of messages that can contain pings in the last sent messages by the user. See <see cref="NumTrackedMessages"/> for how many messages this is.<para/>
		/// Percentage is computed by going through the last messages from the user and testing if the message contains a ping. If the # of messages / <see cref="NumTrackedMessages"/> &gt;= this, they will be flagged for an infraction.<para/>
		/// It should only be tested when the latest message contains a ping, as if they exceed the limit early on, sending consecutive messages even without pings will still trip it.
		/// </summary>
		public double MaxPingSaturation => SystemPersistence.TryGetType("MaxSaturationPings", 0.55);

		/// <summary>
		/// The maximum % of emojis (compared to text) in the last <see cref="NumTrackedMessages"/> messages sent by the user. If, on average, their messages are made up of more than (this %) emojis for content, it will be treated as spam.
		/// </summary>
		public double MaxEmojiSaturation => SystemPersistence.TryGetType("MaxSaturationEmojis", 0.65);

		/// <summary>
		/// The maximum saturation of characters in a message. That is, if the last <see cref="NumTrackedMessages"/> messages contain more characters than (this %) of the total character limit of all those messages combined, it will be treated as spam.<para/>
		/// Under default values, this means that if there's more than a total of 5000 characters in the last 10 messages, it will be treated as spam.
		/// </summary>
		public double MaxCharacterSaturationRatio => SystemPersistence.TryGetType("MaxCharacterSaturationRatio", 0.25);

		/// <summary>
		/// The maximum amount of emojis in a single message.
		/// </summary>
		public int MaxEmojisInOneMessage => SystemPersistence.TryGetType("MaxEmojisInOneMessage", 16);

		/// <summary>
		/// The maximum amount of <c>0x200D</c> emoji joining characters that can be used in a single composite emoji.<para/>
		/// If an emoji uses these, it will be split into groups of (this + 1) components, so a family of a mom/dad with 13 kids under the default value of 2 would count as 5 emojis. (15 / (2+1) = 5)
		/// </summary>
		public int MaxEmojiChainLength => SystemPersistence.TryGetType("MaxEmojiChainLength", 2);

		/// <summary>
		/// The amount of time a user will be muted for in seconds should they sufficiently break antispam threshold.
		/// </summary>
		public int DefaultMuteTimeMinutes => SystemPersistence.TryGetType("DefaultMuteTimeMinutes", 1440);

		/// <summary>
		/// Whether or not to count replying to someone (with @mentions ON) as a ping.
		/// </summary>
		public bool CountRepliesAsPings => SystemPersistence.TryGetType("CountRepliesAsPings", true);

		/// <summary>
		/// Whether or not to mute people who attempt to use @everyone or @here
		/// </summary>
		public bool MuteForEveryoneAndHere => SystemPersistence.TryGetType("MuteForEveryoneAndHere", true);

		#endregion

		#region Data

		/// <summary>
		/// The total possible length in the past <see cref="NumTrackedMessages"/> messages combined
		/// </summary>
		public int TotalPossibleCharacters => NumTrackedMessages * 2000; // n.b. don't care about nitro's 4k limit

		/// <summary>
		/// The current epoch in seconds.
		/// </summary>
		public double CurrentTimeInSeconds {
			get {
				long millis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				return (millis / 1000D);
			}
		}

		#region Dictionaries 


		/// <summary>
		/// Stores a user's latest messages.
		/// </summary>
		public readonly Dictionary<Snowflake, LimitedSpaceArray<Message>> UserLatestMessages = new Dictionary<Snowflake, LimitedSpaceArray<Message>>();

		/// <summary>
		/// An array from user to whether or not a given stored message was a reply.
		/// </summary>
		public readonly Dictionary<Snowflake, LimitedSpaceArray<bool>> UserLatestReplies = new Dictionary<Snowflake, LimitedSpaceArray<bool>>();

		/// <summary>
		/// Stores how many messages the user has sent in a row that are identical.
		/// </summary>
		public readonly Dictionary<Snowflake, int> UserConsecutiveIdenticalMessageCount = new Dictionary<Snowflake, int>();

		/// <summary>
		/// Stores how many messages the user has sent in a row that have pings
		/// </summary>
		public readonly Dictionary<Snowflake, int> UserConsecutivePingMessageCount = new Dictionary<Snowflake, int>();

		/// <summary>
		/// Stores how many messages the user has sent that were flagged as "fast".
		/// </summary>
		public readonly Dictionary<Snowflake, ExpiringCounter> UserFastMessageCount = new Dictionary<Snowflake, ExpiringCounter>();

		/// <summary>
		/// Stores the last tick that the user sent a message.
		/// </summary>
		public readonly Dictionary<Snowflake, double> UserMessageTimes = new Dictionary<Snowflake, double>();

		/// <summary>
		/// If the user is under a message rate parachute (prevent messages for *x* seconds), this stores the time that it started.
		/// </summary>
		public readonly Dictionary<Snowflake, double> UserParachuteTimes = new Dictionary<Snowflake, double>();

		/// <summary>
		/// Stores the amount of infractions from a user flagged by the spam handler exclusively.
		/// </summary>
		public readonly Dictionary<Snowflake, ExpiringCounter> UserInfractionCount = new Dictionary<Snowflake, ExpiringCounter>();

		/// <summary>
		/// The number of emojis in the last messages sent by a user.
		/// </summary>
		public readonly Dictionary<Snowflake, LimitedSpaceArray<int>> NumberOfRecentEmojis = new Dictionary<Snowflake, LimitedSpaceArray<int>>();

		/// <summary>
		/// The number of invites in the last messages sent by a user.
		/// </summary>
		public readonly Dictionary<Snowflake, int> NumberOfRecentInvites = new Dictionary<Snowflake, int>();

		/// <summary>
		/// A list of user IDs who are able to ping any number of users without getting muted by the ping limiter.
		/// </summary>
		public readonly List<Snowflake> UsersWhoCanBypassPingLimits = new List<Snowflake>();


		#endregion

		#endregion

		public HandlerAntiSpamSystem(BotContext ctx) : base(ctx) {
			SystemPersistence = DataPersistence.GetPersistence(ctx, "antispam.cfg");
			SetupEvents();
		}

		#region Core Code

		/// <summary>
		/// Returns true if this member has data in this handler.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool HasData(Snowflake userId) {
			return UserLatestMessages.ContainsKey(userId);
		}

		/// <summary>
		/// Looks at the given userID and defines its data in the various dictionaries.
		/// </summary>
		/// <param name="userId"></param>
		public void PopulateUserDataIfNecessary(Snowflake userId) {
			//if (HasData(userId)) return;
			//HandlerLogger.WriteLine(string.Format("Instantiatiating at least some userdata in antispam for {0} (storing {1} messages)", userId, NumTrackedMessages), LogLevel.Debug);
			if (!UserLatestMessages.ContainsKey(userId)) UserLatestMessages[userId] = new LimitedSpaceArray<Message>(NumTrackedMessages);
			if (!UserLatestReplies.ContainsKey(userId)) UserLatestReplies[userId] = new LimitedSpaceArray<bool>(NumTrackedMessages);
			if (!UserConsecutiveIdenticalMessageCount.ContainsKey(userId)) UserConsecutiveIdenticalMessageCount[userId] = 0;
			if (!UserConsecutivePingMessageCount.ContainsKey(userId)) UserConsecutivePingMessageCount[userId] = 0;
			if (!UserFastMessageCount.ContainsKey(userId)) UserFastMessageCount[userId] = new ExpiringCounter(0, 15);
			if (!UserMessageTimes.ContainsKey(userId)) UserMessageTimes[userId] = 0;
			if (!UserParachuteTimes.ContainsKey(userId)) UserParachuteTimes[userId] = 0;
			if (!UserInfractionCount.ContainsKey(userId)) UserInfractionCount[userId] = new ExpiringCounter(0, InfractionExpirationTime);
			if (!NumberOfRecentEmojis.ContainsKey(userId)) NumberOfRecentEmojis[userId] = new LimitedSpaceArray<int>(NumTrackedMessages);
			if (!NumberOfRecentInvites.ContainsKey(userId)) NumberOfRecentInvites[userId] = 0;
		}

		#region Dynamic Quantifying Methods

		/// <summary>
		/// Gets the number of emojis in the message.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		private int GetNumberOfEmojisInMessage(string message) {
			MatchCollection matches = Regex.Matches(message, @"<:([a-z]|[A-Z]|[0-9]|_)+:\d+>");
			MatchCollection animMatches = Regex.Matches(message, @"<a:([a-z]|[A-Z]|[0-9]|_)+:\d+>");

			// This is Y U K K I.
			int total = matches.Count + animMatches.Count;

			// "Glue" refers to 0x200D which is used to join emoji components for compound emojis, such as the family emojis.
			bool wasLastGlue = false;
			int lastChainCount = 0;

			for (var i = 0; i < message.Length;) {
				// Does this char form a surrogate pair (two chars combined into one)?
				bool isSurrogate = char.IsSurrogatePair(message, i);

				// Increment i based on the necessary amount of chars.
				i += isSurrogate ? 2 : 1;
				if (isSurrogate) {
					bool canHaveNext = i < message.Length; // Is this half a surrogate pair (is this the last char in the text)
					if (canHaveNext) {
						int val = char.ConvertToUtf32(message, i); // this will get the next char
						if (val == 0x200D) {
							// This is emoji glue, so skip it and flag that we are joining several emojis (so that it doesn't count the actual components)
							if (lastChainCount < MaxEmojiChainLength) {
								wasLastGlue = true;
								lastChainCount++;
							} else {
								// Screw it. Chain too long! Break the chain.
								wasLastGlue = false;
								// This will cause the next emoji to be treated as if it wasn't in a chain.
							}
						} else {
							// Something else that was an Emoji
							if (!wasLastGlue) {
								total++; // If the last char was not there to join two emojis, then these are separate and need to be counted.
								lastChainCount = 0; // And reset the counter that determines how long the chain is.
							} else {
								wasLastGlue = false; // Just reset this.
							}
						}
					} else {
						total++; // Assume that the half-pair is an emoji.
					}
				}
			}

			return total;
		}

		/// <summary>
		/// Returns true if the text contains a discord invite, but the invite is NOT to the Ori server.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public bool MessageContainsInviteLink(string message) {
			bool hasAnInvite = Regex.IsMatch(message, @"(discord.gg\/)\w+", RegexOptions.IgnoreCase) || Regex.IsMatch(message, @"(discord.com\/invite\/)\w+", RegexOptions.IgnoreCase);
			bool isThisServer = Regex.IsMatch(message, @"(discord.gg\/orithegame)", RegexOptions.IgnoreCase) || Regex.IsMatch(message, @"(discord.com\/invite\/orithegame)", RegexOptions.IgnoreCase);
			return hasAnInvite && !isThisServer;
		}

		/// <summary>
		/// Returns whether or not the latency of the message is under the message speed threshold.
		/// </summary>
		/// <param name="userId">The user ID to check.</param>
		/// <returns></returns>
		private bool IsMessageFast(ulong userId) {
			double currentTimeMS = CurrentTimeInSeconds;
			double lastMessageTime = UserMessageTimes[userId];
			UserMessageTimes[userId] = currentTimeMS;
			if (currentTimeMS - lastMessageTime <= MessageSpeedThreshold) {
				return true;
			}
			return false;
		}

		#endregion

		#region Utility

		/// <summary>
		/// Tests if the user in question has too many infractions. If they do, this gives them the server muted role and alerts moderators.
		/// </summary>
		/// <param name="member">The member to check.</param>
		public async Task<bool> TestInfractionsAndMuteIfNecessary(BotContext executingContext, Member member, Message cause, string systemReason) {
			string jumpLink = cause.JumpLink;
			bool mute = UserInfractionCount[member.ID].Count >= MaximumInfractionsBeforeMute;

			MemberMuteUtility muteUtil = MemberMuteUtility.GetOrCreate(executingContext);

			if (mute) {
				var action = await muteUtil.MuteMemberAsync(member, DateTimeOffset.UtcNow.AddMinutes(DefaultMuteTimeMinutes), "Tripped the anti-spam system and gained too many infractions. Latest flag: " + systemReason);
				if (action == MemberMuteUtility.ActionType.Muted) {
					string msg = $"Member {member.Mention} (UserID {member.ID}) was muted by the anti-spam system. The latest cause was: {systemReason}";
					msg += $"\n• The jump link to the latest offending message is: {jumpLink}";
					if (cause.Deleted) {
						msg += " -- this message has been deleted, so the jump link will not work.";
					}
					AllowedMentions mentions = new AllowedMentions();
					mentions.Users.Add(member.ID);
					await ResponseUtil.RespondInAsync(Context.EventLog, HandlerLogger, msg, null, mentions, true);
				} else if (action == MemberMuteUtility.ActionType.OnlyRegistered) {
					string msg = $"Member {member.Mention} (UserID {member.ID}) would have been muted, but the operation failed (either they left the server, or have a higher rank than me). As such, they have only been registered in the mute list, and not actually muted by the role since they're gone. The latest cause was: {systemReason}";
					msg += $"\n• The jump link to the latest offending message is: {jumpLink}";
					if (cause.Deleted) {
						msg += " -- this message has been deleted, so the jump link will not work.";
					}
					AllowedMentions mentions = new AllowedMentions();
					mentions.Users.Add(member.ID);
					await ResponseUtil.RespondInAsync(Context.EventLog, HandlerLogger, msg, null, mentions, true);
				} else if (action == MemberMuteUtility.ActionType.MuteTimeChanged) {
					string msg = $"Member {member.Mention} (UserID {member.ID}) had their mute time changed. This should never happen under any circumstances (if it did, it means they were just chatting in the middle of being muted). The latest cause was: {systemReason}";
					msg += $"\n• The jump link to the latest offending message is: {jumpLink}";
					if (cause.Deleted) {
						msg += " -- this message has been deleted, so the jump link will not work.";
					}
					AllowedMentions mentions = new AllowedMentions();
					mentions.Users.Add(member.ID);
					await ResponseUtil.RespondInAsync(Context.EventLog, HandlerLogger, msg, null, mentions, true);
				}
			} else {
				string msg = $"Member {member.Mention} (UserID {member.ID}) has gained an infraction. The latest cause was: {systemReason}";
				msg += $"\n• The jump link to the latest offending message is: {jumpLink}";
				if (cause.Deleted) {
					msg += " -- this message has been deleted, so the jump link will not work.";
				}
				AllowedMentions mentions = new AllowedMentions();
				mentions.Users.Add(member.ID);
				await ResponseUtil.RespondInAsync(Context.EventLog, HandlerLogger, msg, null, mentions, true);
			}

			return mute;
		}

		private void SetupEvents() {
			/*
			XanBotCoreSystem.Client.MessageReactionAdded += async (client, evt) => {
				if (SpecialDebugSettings) {
					await Task.Delay(1000);
					IReadOnlyList<DiscordUser> reactants = await evt.Message.GetReactionsAsync(evt.Emoji, 15);
					if (!reactants.Contains(evt.User)) {
						XanBotMember xan = XanBotMember.GetMemberFromUserId(BotContextOriTheGame.Instance.Server, 114163433980559366);
						xan.TrySendDMAsync("<@" + evt.User.Id + "> added a reaction of " + evt.Emoji.ToString() + " then removed it within a second lol.");
					}
				}
			};
			*/

			// For edited messages
			DiscordClient.Current.Events.MessageEvents.OnMessageEdited += async (oldMessage, message, isPinned) => {
				if (message.ServerChannel?.Server != Context.Server) return;
				if (UseEmojiSystems) {
					// If there's too many emojis in this message alone
					if (GetNumberOfEmojisInMessage(message.Content) >= MaxEmojisInOneMessage) {
						UserInfractionCount[message.Author.ID].Count++;
						Member member = message.AuthorMember;
						await message.DeleteAsync("This message was edited, and ended up having too many emojis after it was edited");
						await TestInfractionsAndMuteIfNecessary(Context, member, message, "Exceeded limit of emojis in a single message.");
						await member.TrySendDMAsync(WARNING_TOO_MANY_EMOJIS);
					}
				}
			};
		}

		#endregion

		#endregion

		public override async Task<bool> ExecuteHandlerAsync(Member executor, BotContext executionContext, Message message) {
			if (!Enabled) return false;
			if (executor.GetPermissionLevel() >= PermissionLevel.TrustedUser) return false;

			#region Data Setup

			Snowflake userId = executor.ID;
			PopulateUserDataIfNecessary(userId);
			LimitedSpaceArray<Message> userMessageArray = UserLatestMessages[userId];
			LimitedSpaceArray<bool> userReplyArray = UserLatestReplies[userId];
			LimitedSpaceArray<int> userEmojiCount = NumberOfRecentEmojis[userId];
			userMessageArray.Add(message);
			userReplyArray.Add(message.IsMentionedReply);

			#endregion

			#region Pings

			// FIRST TEST:
			// If there's too many pings...
			// I want to deal with this before I do anything else at all.
			// This is a zero tolerance thing. It does not use infractions.
			if (message.Mentions.Length >= MaximumNumberOfPingsInMessage) {
				if (!UsersWhoCanBypassPingLimits.Contains(executor.ID)) {
					UserInfractionCount[userId].Count += MaximumInfractionsBeforeMute;
					await TestInfractionsAndMuteIfNecessary(executionContext, executor, message, "Exceeded maximum amount of pings in one message.");
					return true;
				} else {
					UsersWhoCanBypassPingLimits.Remove(executor.ID);
					await ResponseUtil.RespondInAsync(Context.EventLog, HandlerLogger, $"User {executor.FullName} was granted the ability to ping any number of users and has just used this to their advantage. This one-time ability has been revoked until manually granted again by a moderator.");
				}
			} else {
				// Even if it doesn't exceed the limit, if they are given the pass, it needs to be removed.
				if (UsersWhoCanBypassPingLimits.Contains(executor.ID)) {
					UsersWhoCanBypassPingLimits.Remove(executor.ID);
					await ResponseUtil.RespondInAsync(Context.EventLog, HandlerLogger, $"User {executor.FullName} was granted the ability to ping any number of users and blew it by not pinging that many people in their message. This one-time ability has been revoked until manually granted again by a moderator.");
				}
			}
			if (message.Content.Contains("@everyone") || message.Content.Contains("@here")) {
				if (MuteForEveryoneAndHere && !message.AuthorMember.HasPermission(EtiBotCore.Payloads.Data.Permissions.MentionEveryone)) {
					// We mute for pings to everyone/here, specifically where the person doing it doesn't have the permission to do so.
					UserInfractionCount[userId].Count += MaximumInfractionsBeforeMute;
					await TestInfractionsAndMuteIfNecessary(executionContext, executor, message, "Attempt to ping everyone or here.");
					await message.DeleteAsync("Contains mass ping.");
					return true;
				}
			}

			#endregion

			#region Cooldown Time For Fast Messages

			// Parachute time handler: If a user is under parachute time, it means they're on a cooldown and can't send messages.
			//XanBotLogger.WriteLine("Testing parachute times.");
			if (UserParachuteTimes[userId] != 0) {
				//XanBotLogger.WriteLine("User has a parachute timer.");
				double currentTime = CurrentTimeInSeconds;
				if (currentTime - UserParachuteTimes[userId] >= UserTooFastCooldownTime) {
					UserParachuteTimes[userId] = 0;
				} else {
					// This will put it in the server's audit log. Logging is important.
					await message.DeleteAsync(string.Format("User has sent messages too fast and is on bot-enforced cooldown (Remaining time: {0} seconds)", UserParachuteTimes[userId]));
					return true;
				}
			}

			#endregion

			#region Invite Link

			if (!AllowPostingInvites && MessageContainsInviteLink(message.Content)) {
				NumberOfRecentInvites[userId]++;
				await message.DeleteAsync("This message contained an instant invite that was not acceptable for this server, so I'm removing it.");

				UserInfractionCount[userId].Count++;
				await TestInfractionsAndMuteIfNecessary(executionContext, executor, message, "Posted an instant invite.");
				await executor.TrySendDMAsync(string.Format(WARNING_SENT_INVITE_LINK, EmojiLookup.GetEmoji("warning"), EmojiLookup.GetEmoji("information_source")));
				return true;
			}

			#endregion

			#region Frequency & Saturation Systems

			if (UseFrequencyBasedSystem) {

				#region Ping Frequency

				Match ping = Regex.Match(userMessageArray[0].Content, @"(<@(!*|&*))(\d+)(>)");
				if (ping.Success) {
					// Only if the latest message is a ping. Say I send 4 immediate pings and then chat normally, if the threshold says that I have to have only 4 recent ping messages, that means *every message* I send will give me an infraction until those four are pushed out of my recent message list.

					// Count
					double nPings = 0;
					foreach (Message msg in userMessageArray) {
						if (msg?.Content != null) {
							if (!Regex.IsMatch(msg.Content, @"(<@(!*|&*))(\d+)(>)")) {
								continue;
							}
							nPings++;
						}
					}

					// If the % is bigger than what we want...
					if (nPings / NumTrackedMessages >= MaxPingSaturation) {
						HandlerLogger.WriteLine($"nPings={nPings} NumberOfStoredMessages={NumTrackedMessages} div={nPings / NumTrackedMessages} MaxSat={MaxPingSaturation}", LogLevel.Debug);
						UserInfractionCount[userId].Count++;
						await TestInfractionsAndMuteIfNecessary(executionContext, executor, message, "Exceeded maximum saturation of pings in the last few messages.");
						await executor.TrySendDMAsync(string.Format(WARNING_MAX_PING_SATURATION, EmojiLookup.GetEmoji("warning"), EmojiLookup.GetEmoji("information_source")));
						return true;
					}
				}

				#endregion

				#region Emoji Frequency

				//XanBotLogger.WriteLine("Testing emoji frequency");
				if (UseEmojiSystems) {
					// Number of Emojis.
					int numberOfEmojis = GetNumberOfEmojisInMessage(userMessageArray[0].Content);
					if (numberOfEmojis > 0) {
						// Latest emoji has a message. Same reasoning as the ping idea above.

						// Count
						double nEmojis = 0;
						foreach (int i in userEmojiCount) {
							nEmojis += i;
						}

						if (nEmojis / NumTrackedMessages >= MaxEmojiSaturation) {
							UserInfractionCount[userId].Count++;
							await message.DeleteAsync("Message had too many emojis (judged via the % of emojis in their last messages) so I am removing it to prevent spam.");
							await TestInfractionsAndMuteIfNecessary(executionContext, executor, message, "Exceeded maximum saturation of emojis in the last few messages.");
							await executor.TrySendDMAsync(string.Format(WARNING_MAX_EMOJI_SATURATION, EmojiLookup.GetEmoji("warning"), EmojiLookup.GetEmoji("information_source")));
							return true;
						}
					}
				}

				#endregion

				#region Long Message Length Frequency

				//XanBotLogger.WriteLine("Testing message size");
				if (UseCharacterSaturationSystems) {
					int length = userMessageArray[0].Content.Length;
					if (length >= (2000 * MaxCharacterSaturationRatio)) {
						double numChars = 0;
						foreach (Message msg in UserLatestMessages[userId]) {
							if (msg?.Content != null) {
								numChars += msg.Content.Length;
							}
						}

						if ((numChars / TotalPossibleCharacters) > MaxCharacterSaturationRatio) {
							UserInfractionCount[userId].Count++;
							await message.DeleteAsync("Saturation of text is too high, this user has sent a bunch of huge blocks of text so I am removing it to prevent spam.");
							await TestInfractionsAndMuteIfNecessary(executionContext, executor, message, "Exceeded maximum saturation of message content, sending a lot of huge messages in a row.");
							await executor.TrySendDMAsync(string.Format(WARNING_MAX_TEXT_SATURATION, EmojiLookup.GetEmoji("warning"), EmojiLookup.GetEmoji("information_source")));
							
							return true;
						}
					}
				}

				#endregion

			}

			#region Emoji Count
			if (UseEmojiSystems) {
				// If there's too many emojis in this message alone
				if (GetNumberOfEmojisInMessage(message.Content) >= MaxEmojisInOneMessage) {
					UserInfractionCount[userId].Count++;

					await message.DeleteAsync("Too many emojis in this message.");
					await TestInfractionsAndMuteIfNecessary(executionContext, executor, message, "Exceeded limit of emojis in a single message.");
					await executor.TrySendDMAsync(string.Format(WARNING_TOO_MANY_EMOJIS, EmojiLookup.GetEmoji("warning"), EmojiLookup.GetEmoji("information_source")));

					return true;
				}
			}
			#endregion

			#endregion

			#region Message Comparator

			// If there's at least two messages in this array...
			//XanBotLogger.WriteLine("Testing # of messages...");
			if (userMessageArray[0] != null && userMessageArray[1] != null) {
				#region Identical Messages

				// And the two messages are identical (case insensitive)...
				if (userMessageArray[0].Content.ToLower() == userMessageArray[1].Content.ToLower()) {
					// Increment the consecutive message count.
					// CATCH CASE: If users send embeds without a caption (so just the image or whatever) the message is sent as "". Don't let this count!
					string spamContent = userMessageArray[0].Content;
					if (spamContent != "") {
						// Increment identical message count
						UserConsecutiveIdenticalMessageCount[userId]++;

						// If they exceed the limit, log the infraction and report the event.
						if (UserConsecutiveIdenticalMessageCount[userId] >= MaxConsecutiveIdenticalMessages) {
							UserInfractionCount[userId].Count++;
							bool muted = await TestInfractionsAndMuteIfNecessary(executionContext, executor, message, "Too many identical messages sent in a row");
							if (UserConsecutiveIdenticalMessageCount[userId] == MaxConsecutiveIdenticalMessages) {
								await executor.TrySendDMAsync(string.Format(WARNING_REPEATED_CONTENT, EmojiLookup.GetEmoji("warning"), EmojiLookup.GetEmoji("information_source")));
							}
							if (muted) {
								foreach (Message msg in userMessageArray) {
									if (msg.Content == spamContent && !msg.Deleted) {
										await msg.DeleteAsync("This message has the same content as a message that resulted in a mute for spam.");
									}
								}
							}
							
							return true;
						}
					}
				} else {
					UserConsecutiveIdenticalMessageCount[userId] = 0;
				}

				#endregion

				#region Consecutive Pings

				// Or the message has a ping...
				bool isConsecutivePing = Regex.IsMatch(userMessageArray[0].Content, @"(<@(!*|&*))(\d+)(>)") && Regex.IsMatch(userMessageArray[1]?.Content ?? string.Empty, @"(<@(!*|&*))(\d+)(>)");
				bool isConsecutiveReply = CountRepliesAsPings && userReplyArray[0] && userReplyArray[1];

				if (isConsecutivePing || isConsecutiveReply) {
					UserConsecutivePingMessageCount[userId]++;
					if (UserConsecutivePingMessageCount[userId] >= MaximumNumberOfConsecutiveMessagesWithPings) {
						UserInfractionCount[userId].Count++;
						await TestInfractionsAndMuteIfNecessary(executionContext, executor, message, "Too many consecutive pings sent in a row");
						if (UserConsecutivePingMessageCount[userId] == MaximumNumberOfConsecutiveMessagesWithPings) {
							await ResponseUtil.RespondInAsync(Context.EventLog, HandlerLogger, string.Format("User {0} has gained an infraction for the reason of: Exceeded maximum amount of consecutive messages that ping anyone. Use `>> config list` to see the limit values. Current infraction count: {1} (This will only be sent when the consecutive messages is exclusively equal to the limit. This prevents spamming this channel.)", executor.FullName, UserInfractionCount[userId].Count));
							await executor.TrySendDMAsync(string.Format(WARNING_REPEATED_PINGS, EmojiLookup.GetEmoji("warning"), EmojiLookup.GetEmoji("information_source")));
						}
						return true;
					}
				} else {
					UserConsecutivePingMessageCount[userId] = 0;
				}

				#endregion
			}

			#endregion

			#region Message Speed
			if (IsMessageFast(userId)) {
				UserFastMessageCount[userId].Count++;
				if (UserFastMessageCount[userId].Count >= MaxFastMessagesBeforeDeletion) {
					UserParachuteTimes[userId] = UserTooFastCooldownTime;
					UserInfractionCount[userId].Count++;
					await TestInfractionsAndMuteIfNecessary(executionContext, executor, message, "Sending messages far too quickly");
					if (UserFastMessageCount[userId].Count == MaxFastMessagesBeforeDeletion) {
						await executor.TrySendDMAsync(string.Format(WARNING_TOO_FAST, EmojiLookup.GetEmoji("warning"), EmojiLookup.GetEmoji("information_source")));
					}
					
				}
				return true;
			}
			#endregion

			return false;
		}
	}

	/// <summary>
	/// Represents a quantifier for user infractions that has an "expiration time" associated with it e.g. when a user makes a mistake that the bot catches, it will increment their infraction count by 1. This class will subtract 1 after a certain amount of time has passed.
	/// </summary>
	public class ExpiringCounter {

		private int CountInternal = 0;

		/// <summary>
		/// The amount of time it takes for the value to decrease whenever incremented.
		/// </summary>
		public double RemovalTimeSeconds { get; }

		/// <summary>
		/// The quantity of whatever this counter is representing.
		/// </summary>
		public int Count {
			get {
				return CountInternal;
			}
			set {
				if (value > CountInternal) {
					// Adding an amount of infractions.
					int addedInfractions = value - CountInternal;
					_ = RegisterRemoval(addedInfractions);
				} else if (value < CountInternal) {
					// throw new InvalidOperationException("Cannot manually subtract infractions.");
					CountInternal = Math.Max(CountInternal - value, 0);
				}
				CountInternal = value;
				// Being set equal so do nothing.
			}
		}

		/// <summary>
		/// Schedules the removal of a certain amount of infractions after a certain amount of time in milliseconds.
		/// </summary>
		/// <param name="count">The amount of infractions to remove.</param>
		private async Task RegisterRemoval(int count) {
			await Task.Delay((int)(RemovalTimeSeconds * 1000));
			CountInternal = Math.Max(CountInternal - count, 0);
		}

		/// <summary>
		/// Create a new ExpiringCounter.
		/// </summary>
		/// <param name="startAmount">The starting amount of infractions.</param>
		/// <param name="removalTimeInSeconds">The amount of time it takes to remove one infraction after it has been added.</param>
		public ExpiringCounter(int startAmount = 0, double removalTimeInSeconds = 60) {
			// Do not directly set internal as that won't schedule removal.
			Count = startAmount;
			RemovalTimeSeconds = removalTimeInSeconds;
		}

	}
}
