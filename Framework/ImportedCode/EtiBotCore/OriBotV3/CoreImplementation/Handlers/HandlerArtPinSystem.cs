using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.Payloads.Data;
using EtiLogger.Data.Structs;
using EtiLogger.Logging;
using OldOriBot.Data;
using OldOriBot.Data.MemberInformation;
using OldOriBot.Data.Persistence;
using OldOriBot.Interaction;
using OldOriBot.PermissionData;
using OldOriBot.UserProfiles;

namespace OldOriBot.CoreImplementation.Handlers {
	public class HandlerArtPinSystem : PassiveHandler {

		#region Valid Art (Extensions / Links) & Other Keywords
		/*
		public const string C_PUSHPIN = "📌";

		public const string C_NO_ENTRY = "⛔";

		public const string C_NO_ENTRY_SIGN = "🚫";
		*/
		public static readonly Emoji EMOJI_PUSHPIN = Emoji.GetOrCreate(EmojiLookup.GetEmoji("pushpin"));

		public static readonly Emoji EMOJI_MAX_PINS_REACHED = Emoji.GetOrCreate(EmojiLookup.GetEmoji("no_entry"));

		public static readonly Emoji EMOJI_PINS_DISABLED = Emoji.GetOrCreate(EmojiLookup.GetEmoji("no_entry_sign"));

		/// <summary>
		/// When finding invalid reactions or messages to track, this is the amount of messages that will be downloaded.
		/// </summary>
		public const int NUM_MESSAGES_TO_DOWNLOAD = 100;

		/// <summary>
		/// Out of the messages downloaded (see <see cref="NUM_MESSAGES_TO_DOWNLOAD"/>), only this many will be changed if any invalid emojis are found.
		/// </summary>
		public const int NUM_MESSAGES_TO_ALTER = 20;

		/// <summary>
		/// File formats that are counted as containers for art.
		/// </summary>
		private static readonly string[] SUPPORTED_FORMATS = {
			".webp",
			".png",
			".gif",
			".jpg",
			".jpeg",
			".mp3",
			".ogg",
			".wav",
			".flac",
			".mp4",
			".mov",
			".webm"
		};

		/// <summary>
		/// Websites that can host art.
		/// </summary>
		private static readonly string[] SUPPORTED_URLS = {
			"https://imgur.com/",
			"https://i.imgur.com/",
			"https://gyazo.com/",
			"https://i.gyazo.com/",
			"https://cdn.discordapp.com/attachments/",
			"https://www.deviantart.com/",
			"https://soundcloud.com/",
			"https://www.youtube.com/",
			"https://youtu.be/",
			"https://docs.google.com/",
			"https://twitter.com/",
			"https://pbs.twimg.com/media/",
			"https://bandcamp.com/",
			"https://musescore.com/",
			"https://www.fanfiction.net/",
			"https://www.wattpad.com/",
			"https://archiveofourown.org/",
			"https://www.songsterr.com",
			"https://steamcommunity.com/sharedfiles/filedetails/?id=",
			"https://www.artstation.com/artwork/",
			"https://www.reddit.com/r/",
			"https://redd.it/"
		};

		/// <summary>
		/// Queries that can trigger the system to remind people of guidelines for emoji submissions.
		/// </summary>
		public readonly IReadOnlyList<string> EmojiSubDetections = new List<string>() {
			"emote submission",
			"emoji submission",
			"emote sub",
			"emoji sub",
			//"emoji",
			//"emote",
		};

		#endregion

		#region Implementation

		public override string Name { get; } = "Art Pin System";

		public override string Description { get; } = "Manages the pins of the art gallery channel based on user reactions to pin popular works of art.";

		public override bool RunOnCommands { get; } = true;

		/// <summary>
		/// A reference to the server's art channel, where this handler runs.
		/// </summary>
		public TextChannel ArtChannel { get; protected set; }

		/// <summary>
		/// Data persistence for the art pin system.
		/// </summary>
		protected DataPersistence SystemPersistence { get; }

		/// <summary>
		/// Used to track pins.
		/// </summary>
		protected EagerPinTracker PinTracker { get; set; }

		/// <summary>
		/// A reference to the bot <see cref="Member"/>.
		/// </summary>
		public Member Bot => Context.Server.BotMember;

		/// <summary>
		/// A list of which messages out of the last 100 have the bot's special :no_entry: reaction on them.
		/// </summary>
		protected List<Message> MessagesFromAuthorsWithTooManyPins = new List<Message>();

		public HandlerArtPinSystem(Guild server, BotContext ctx) : base(ctx) {
			//DiscordClient.Current.Events.GuildEvents.OnGuildCreated += OnGuildCreated;
			SystemPersistence = DataPersistence.GetPersistence(ctx, "artgallery.cfg");
			_ = Initialize(server);
		}

		public async Task Initialize(Guild guild) {
			Log.WriteLine("Initializing...");
			ArtChannel = guild.GetChannel<TextChannel>(639160533076934686);
			if (ArtChannel == null) {
				await guild.ForcefullyAcquireChannelsAsync();
				ArtChannel = guild.GetChannel<TextChannel>(639160533076934686);
			}
			PinTracker = EagerPinTracker.GetTrackerFor(ArtChannel);
			PinTracker.OnMessagePinned += OnMessagePinned;
			PinTracker.OnMessageUnpinned += OnMessageUnpinned;

			Message[] latest = await ArtChannel.GetAllMessagesAsync(NUM_MESSAGES_TO_DOWNLOAD);
			for (int idx = 0; idx < latest.Length; idx++) {
				Message message = latest[idx];
				if (HasReachedMaxPinsEmoji(message)) {
					// Verify: Does this user actually have too many pinned messages still?
					if (HasTooManyPins(message.AuthorMember)) {
						// Yes, add it to the registry
						MessagesFromAuthorsWithTooManyPins.Add(message);
					} else {
						// No, remove the reaction (granted it's past the threshold)
						if (idx >= NUM_MESSAGES_TO_DOWNLOAD - NUM_MESSAGES_TO_ALTER) {
							await message.Reactions.RemoveReactionsOfEmojiAsync(EMOJI_MAX_PINS_REACHED);
						}
					}
				}
				message.Reactions.EagerTracking = true;
			}

			DiscordClient.Current.Events.ReactionEvents.OnReactionAdded += OnReactionAdded;
			DiscordClient.Current.Events.ReactionEvents.OnReactionRemoved += OnReactionRemoved;
			DiscordClient.Current.Events.ReactionEvents.OnAllReactionsRemoved += OnAllReactionsRemoved;
			DiscordClient.Current.Events.ReactionEvents.OnAllReactionsOfEmojiRemoved += OnAllReactionsOfEmojiRemoved;
			DiscordClient.Current.Events.MessageEvents.OnMessageCreated += OnMessageCreated;
		}

		#endregion

		#region Configuration

		/// <summary>
		/// Whether or not the art pin system is enabled.
		/// </summary>
		public bool IsSystemEnabled => SystemPersistence.TryGetType("Enabled", true);

		/// <summary>
		/// The amount of pin reactions required for something to get pinned.
		/// </summary>
		public int ReactionsToGetPinned => SystemPersistence.TryGetType("NumReactionsForArtPin", 31);

		/// <summary>
		/// Whether or not to give people the pin badge and creative badge.
		/// </summary>
		public bool AwardBadges => SystemPersistence.TryGetType("AwardBadges", false);

		/// <summary>
		/// The maximum amount of days old that a post can be. If a post is older than this, it shouldn't be pinned.
		/// </summary>
		public int MaxPostAge => SystemPersistence.TryGetType("MaxPostAgeDays", 2);

		/// <summary>
		/// The amount of pinned works that someone can have up at one time.
		/// </summary>
		public int MaxSimultaneousPins => SystemPersistence.TryGetType("MaxPinsForOnePerson", 5);

		/// <summary>
		/// Automatically remove or add the :no_entry: emoji on all known works from a given artist when one of their pin slots frees up or they all get taken.<para/>
		/// <strong>WARNING: May cause excessive network requests!</strong>
		/// </summary>
		public bool TryAutoUpdateMaxSign => SystemPersistence.TryGetType("AutoUpdateMaxSignAsNeeded", false);

		/// <summary>
		/// If <see langword="true"/>, more verbose information will be printed to trace logs for the art gallery.
		/// </summary>
		public bool ExtraLoggingDetails => SystemPersistence.TryGetType("ExtraVerboseGalleryLogging", false);

		/// <summary>
		/// Whether or not to delete posts from mods if it doesn't qualify as art.
		/// </summary>
		public bool TreatModsAsNormalUsers => SystemPersistence.TryGetType("TreatModsAsNormalUsers", false);

		#endregion

		protected Logger Log { get; } = new Logger(new LogMessage.MessageComponent("[Art Pin System] ", Color.SPIRIT_BLUE));

		/// <summary>
		/// The message ID being *in this dictionary* means it has been downloaded. Reaction events should yield while the bool is false, as this means it's not done actually downloading the reactions.
		/// </summary>
		private Dictionary<Snowflake, bool> MessagesWithDownloadedReactions = new Dictionary<Snowflake, bool>();

		public override async Task<bool> ExecuteHandlerAsync(Member executor, BotContext executionContext, Message message) {
			if (message.Channel != ArtChannel) return false;
			if (!IsSystemEnabled) return false;
			bool skipCheckOnMods = message.AuthorMember?.GetPermissionLevel() >= PermissionLevel.Operator && !TreatModsAsNormalUsers;

			UserProfile posterProfile = UserProfile.GetOrCreateProfileOf(executor);

			bool postIsArt = skipCheckOnMods; // If we skip the check on mods, then the post is art no matter what (if they are a mod)
			if (!postIsArt) {
				foreach (string url in SUPPORTED_URLS) {
					if (message.Content.ToLower().Contains(url)) {
						postIsArt = true;
						break;
					}
				}

				if (message.Attachments.Length > 0) {
					foreach (Attachment attachment in message.Attachments) {
						foreach (string extension in SUPPORTED_FORMATS) {
							if (attachment.FileName.ToLower().EndsWith(extension)) {
								postIsArt = true;
								break;
							}
						}
						if (postIsArt) break;
					}
				}
			}

			if (!postIsArt) {
				if (message.Type == MessageType.ChannelPinnedMessage) return true; // Ignore these, they're deleted elsewhere.

				await executor.TrySendDMAsync(Personality.Get("handler.artpin.unsupported", executor.Mention, ArtChannel.Mention));
				await message.DeleteAsync("The message was sent in the art gallery and wasn't counted as art due to missing a proper file or URL.");
				return true;
			}

			bool authorHasTooManyPins = HasReachedMaxPinsEmoji(message);
			if (HasTooManyPins(message.AuthorMember)) {
				if (!authorHasTooManyPins) await message.Reactions.AddReactionAsync(EMOJI_MAX_PINS_REACHED, $"{executor.FullName} has too many pinned works right now.");
				// Add the reaction if they have too many pins.
			} else {
				// They're good to go!
				await message.Reactions.AddReactionAsync(EMOJI_PUSHPIN, "Primer pin due to a valid art post.");
			}

			// DO NOT ENABLE EAGER REACTION DOWNLOADING. This is only useful for messages made BEFORE bot init.
			posterProfile.GrantBadge(BadgeRegistry.ART_POSTED_BADGE, true);
			return true; // return true here so that other handlers dont run in gallery
		}

		#region Reaction & Message Event Handlers

		private async Task OnMessageCreated(Message message, bool? pinned) {
			if (message.Channel != ArtChannel) return;
			if (message.Type == MessageType.ChannelPinnedMessage) {
				await message.DeleteAsync("This message was a pin notification in the art gallery.");
			}
		}

		/// <summary>
		/// Returns whether or not the bot is (probably) preventing this post from being pinned due to the user having enough pinned works already.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		private bool HasReachedMaxPinsEmoji(Message message) => message.Reactions.HasReactionFrom(Bot, EMOJI_MAX_PINS_REACHED);
		

		/// <summary>
		/// Returns whether or not the given member has too many messages pinned right now, and cannot have another message pinned.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		private bool HasTooManyPins(Member member) {
			IReadOnlyList<Message> pinnedMessages = PinTracker.GetPinnedMessages();
			int numPinned = 0;
			foreach (Message m in pinnedMessages) {
				if (m.AuthorMember == member) numPinned++;
			}
			return numPinned >= MaxSimultaneousPins;
		}

		/// <summary>
		/// Returns whether or not this message has enough pin reactions to get pinned.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		private (bool, int) HasEnoughPinReactions(Message message) {
			int count = message.Reactions.GetNumberOfReactions(EMOJI_PUSHPIN);
			if (message.Reactions.HasReactionFrom(Bot, EMOJI_PUSHPIN)) {
				count--;
			}
			if (message.Reactions.HasReactionFrom(message.Author, EMOJI_PUSHPIN)) {
				count--;
			}
			return (count >= ReactionsToGetPinned, count);
		}//=> message.Reactions.GetNumberOfReactionsExcluding(EMOJI_PUSHPIN, Bot) >= ReactionsToGetPinned;
		// ^ DEBUG: Remove the author of the message from the method so that they can count themselves.

		/// <summary>
		/// Returns whether or not this message cannot be pinned due to having the given no entry sign reaction reaction. This does not count the count limiter
		/// </summary>
		/// <remarks>
		/// This method may yield if it has to download any member objects.
		/// </remarks>
		/// <param name="message"></param>
		/// <returns></returns>
		private async Task<bool> IsPinDeniedByEmoji(Message message) {
			if (message.Reactions.ReactionsByEmoji.TryGetValue(EMOJI_PINS_DISABLED, out IReadOnlyList<User> users)) {
				if (users == null) return false;
				foreach (User user in users) {
					if (user.IsSelf) return true; // Bot added no entry, this is valid
					Member member = await Context.Server.GetMemberAsync(user.ID);
					if (member.GetPermissionLevel() >= PermissionLevel.Operator) return true; // A mod added no entry.
				}
			}
			return false;
		}

		/// <summary>
		/// Returns whether or not the emoji given has meaning to the pin system.
		/// </summary>
		/// <param name="emoji"></param>
		/// <returns></returns>
		private bool IsValidEmoji(Emoji emoji) {
			return emoji == EMOJI_PUSHPIN || emoji == EMOJI_PINS_DISABLED || emoji == EMOJI_MAX_PINS_REACHED;
		}


		/// <summary>
		/// A method that is run when the reactions on the given message have changed.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="emoji">The emoji that was altered, or null if all emojis were removed.</param>
		/// <param name="user">The user that added or removed the emoji, if it was not a bulk action.</param>
		/// <param name="added">True if was added or false if it was removed</param>
		/// <returns></returns>
		private async Task ReactionsChanged(Message message, Emoji emoji, User user, bool added) {
			// First-off: Emoji validation:
			if (user == null) return; // Bulk removal = skip!
			if (emoji == null) return;
			if (message.Channel is DMChannel) return;
			if (message.Channel != ArtChannel) return;
			DateTimeOffset messageCreationTime = message.ID.ToDateTimeOffset();
			if ((DateTimeOffset.UtcNow - messageCreationTime).Days > MaxPostAge) {
				if (ExtraLoggingDetails) Log.WriteLine($"Message {message.ID} got a reaction but it's age is over {MaxPostAge} day(s) and so it is being ignored.", LogLevel.Trace);
				return; // Post is too old to get pinned.
			}

			if (!MessagesWithDownloadedReactions.ContainsKey(message.ID)) {
				// Discord probably won't be happy with this.
				MessagesWithDownloadedReactions[message.ID] = false;
				Log.WriteLine($"Downloading all pins and no entry signs on message {message.ID}.", LogLevel.Info);
				await message.Reactions.DownloadReactions(EMOJI_PUSHPIN, 100, true, true);
				await message.Reactions.DownloadReactions(EMOJI_PINS_DISABLED, 100, true, true);
				MessagesWithDownloadedReactions[message.ID] = true;
			} else {
				while (MessagesWithDownloadedReactions[message.ID] == false) {
					await Task.Delay(1000);
				}
			}

			if (!IsValidEmoji(emoji)) {
				if (user.IsSelf) return;

				int unique = message.Reactions.GetNumberOfUniqueReactions();
				if (unique > 18) {
					if (ExtraLoggingDetails) Log.WriteLine($"Message {message.ID} has {unique} unique emoji reactions, which is more than 18, so I will be removing all of the latest added reaction ({emoji.Name})", LogLevel.Trace);
					await message.Reactions.RemoveReactionsOfEmojiAsync(emoji, "This reaction would cause more than 18 unique reactions to be on the message (of the 20 allowed by Discord).", true);
				} else {
					if (ExtraLoggingDetails) Log.WriteLine($"Message {message.ID} has {unique} unique emoji reactions.", LogLevel.Trace);
				}
				return;
			}

			bool isPin = emoji == EMOJI_PUSHPIN;
			bool isDisabler = emoji == EMOJI_PINS_DISABLED;
			bool isMaxSign = emoji == EMOJI_MAX_PINS_REACHED;

			#region Disabler

			if (isDisabler && added) {
				// No entry sign to disable pins. Valid action?
				Member mbr = await user.InServerAsync(message.Server);

				if (user != message.Author && !user.IsSelf && mbr.GetPermissionLevel() < PermissionLevel.Operator) {
					// Not the poster, not the bot. Remove it and abort.
					await message.Reactions.RemoveReactionAsync(user, emoji, $"Only the author or the bot can add {EMOJI_PINS_DISABLED}. It has been removed, because it was added by {user.FullName}.");
				} else {
					// This is valid. Perform a special action.
					await message.Reactions.RemoveReactionsOfEmojiAsync(EMOJI_PUSHPIN, $"Author or bot added {EMOJI_PINS_DISABLED}, which should remove all pin reactions."); // Remove all pushpins
					if (message.Pinned) {
						await PinTracker.UnpinMessageAsync(message, $"Author or bot added {EMOJI_PINS_DISABLED}, which should unpin this message if it's pinned.");
					}
				}
				return;
			} else if (isDisabler && !added) {
				// Was removed.
				if (!message.Reactions.HasReactionFrom(User.BotUser, EMOJI_PINS_DISABLED) && !message.Reactions.HasReactionFrom(message.Author, EMOJI_PINS_DISABLED)) {
					await message.Reactions.AddReactionAsync(EMOJI_PUSHPIN, $"All valid {EMOJI_PINS_DISABLED} reactions were removed, so the bot needs to re-add its primer pin.");
				}
				return;
			}

			#endregion

			if (message.Pinned) {
				if (ExtraLoggingDetails) Log.WriteLine($"Message {message.ID} is already pinned.");
				return;
			}

			#region Max-out

			if (isMaxSign) {
				// Sign that says there's too many pins from this user already. Valid?

				if (added && !user.IsSelf) {
					// Not the bot. Remove it and abort.
					await message.Reactions.RemoveReactionAsync(user, emoji, $"{user.FullName} added {EMOJI_MAX_PINS_REACHED}, but only the bot is allowed to add this.");
				}
				return;
			}

			#endregion

			// If we've made it here, it's a pushpin.
			// The bot's pin reaction shouldn't actually do anything, so stop if it's the bot.
			if (user.IsSelf) return;
			// ^ past that, the reaction is definitely a pin reaction. Just a note to self in case it's collapsed.

			if (added && (await IsPinDeniedByEmoji(message))) {
				// okay so one possibility here is that the author added the pin while it was denied.
				if (message.Reactions.GetNumberOfReactionsFrom(EMOJI_PINS_DISABLED, User.BotUser) == 1) {
					// If the bot reacted with no-entry, then we can remove it. If a mod added it, they're SOL
					// So first remove their pin reaction.
					await message.Reactions.RemoveReactionAsync(user, emoji, "Bot will be adding a pin. The user added this to re-enable pins on their work.");
					await message.Reactions.RemoveReactionAsync(User.BotUser, EMOJI_PINS_DISABLED, "Remove no entry to allow for pins on this work.");
					// ^ This will cause a pin update, which will run line 382
				} else {
					if (user != message.Author) {
						// someone else
						if (message.Reactions.ReactionsByEmoji.TryGetValue(emoji, out IReadOnlyList<User> users)) {
							if (users.Count == 1) {
								await message.Reactions.RemoveReactionAsync(user, emoji, "This message is blocking pins.");
							} else if (users.Count > 1) {
								// clean it up while we're here.
								await message.Reactions.RemoveReactionsOfEmojiAsync(emoji, "This message is blocking pins.");
							}
						}
					} else {
						// author
						await message.Reactions.RemoveReactionAsync(user, emoji, "Self-pin");
					}
				}
				return;
			}

			#region Calculate if the user has too many pins right now and update their message.

			bool messageHasTooManyPinsReaction = HasReachedMaxPinsEmoji(message);
			if (HasTooManyPins(message.AuthorMember)) {
				if (!messageHasTooManyPinsReaction) await message.Reactions.AddReactionAsync(EMOJI_MAX_PINS_REACHED, "This person has too many pinned works right now.");
				// Add the reaction if they have too many pins.
				return; // Abort the handler.
			} else {
				if (messageHasTooManyPinsReaction) await message.Reactions.RemoveReactionsOfEmojiAsync(EMOJI_MAX_PINS_REACHED, "This person has freed up a pin slot since the message was last checked.");
				MessagesFromAuthorsWithTooManyPins.Remove(message);
			}

			#endregion

			// Member asMember = user.InServer(Context.Server);
			(bool hasEnoughPins, int numReactions) = HasEnoughPinReactions(message);
			if (hasEnoughPins) {
				IReadOnlyList<Message> pinnedMessages = PinTracker.GetPinnedMessages();

				if (pinnedMessages.Count == 50) {
					// We've filled up all the slots. Need to remove one.
					Message last = pinnedMessages.Last();
					Log.WriteLine($"There's 50 pins, so I will be unpinning {last.ID} because we need more room.");
					await PinTracker.UnpinMessageAsync(last, "Needed to free up a slot to pin a different message.");
				}
				await PinTracker.PinMessageAsync(message, $"This message has at least {ReactionsToGetPinned} {EMOJI_PUSHPIN} reactions.");

				Member author = message.AuthorMember;
				if (author == null) author = await Context.Server.GetMemberAsync(message.Author.ID);
				UserProfile posterProfile = UserProfile.GetOrCreateProfileOf(author);
				posterProfile.GrantBadge(BadgeRegistry.ART_PINNED_BADGE, true);

				bool dm = false;
				if (posterProfile.UserData.TryGetValue("DMWhenArtPinned", ref dm) && dm)
					await message.Author.TrySendDMAsync($"Hey! Your art at {message.JumpLink} was just pinned! Nice work.\n\nTo disable this message, use `>> profile config set DMWhenArtPinned false` in <#621800841871097866>");
			} else {
				if (ExtraLoggingDetails) Log.WriteLine($"Message {message.ID} does not have enough reactions ({numReactions} of {ReactionsToGetPinned})", LogLevel.Trace);
			}

			// last little case
			if (!message.Reactions.HasReactionFrom(User.BotUser, EMOJI_PUSHPIN)) {
				// add bot reaction if it's missing
				await message.Reactions.AddReactionAsync(EMOJI_PUSHPIN, "Message was missing the bot's assigned pushpin.");
			}
		}

		private async Task OnMessageUnpinned(Message message) {
			if (!TryAutoUpdateMaxSign) return;
			if (message.Channel != ArtChannel) return;

			// Iterate through the messages in this channel. Any messages that we know about that are on an artist who was pin limited need the :no_entry: removed.
			foreach (Message limitedMsg in MessagesFromAuthorsWithTooManyPins) {
				if (limitedMsg.Author == message.Author) {
					if (HasReachedMaxPinsEmoji(limitedMsg)) {
						await limitedMsg.Reactions.RemoveReactionsOfEmojiAsync(EMOJI_MAX_PINS_REACHED, "This person has freed up a pin slot since the message was last checked.");
					}
				}
			}
		}

		private Task OnMessagePinned(Message message) {
			if (!TryAutoUpdateMaxSign) return Task.CompletedTask;
			if (message.Channel != ArtChannel) return Task.CompletedTask;

			// TODO: Implement this
			return Task.CompletedTask;
		}



		private async Task OnAllReactionsOfEmojiRemoved(Message message, Emoji emoji) {
			try {
				await ReactionsChanged(message, emoji, null, false);
			} catch (Exception exc) {
				Log.WriteException(exc);
			}
		}

		private async Task OnAllReactionsRemoved(Message message) {
			try {
				await ReactionsChanged(message, null, null, false);
			} catch (Exception exc) {
				Log.WriteException(exc);
			}
		}

		private async Task OnReactionRemoved(Message message, Emoji emoji, User user) {
			try {
				await ReactionsChanged(message, emoji, user, false);
			} catch (Exception exc) {
				Log.WriteException(exc);
			}
		}

		private async Task OnReactionAdded(Message message, Emoji emoji, User user) {
			try {
				await ReactionsChanged(message, emoji, user, true);
			} catch (Exception exc) {
				Log.WriteException(exc);
			}
		}

		#endregion

	}
}
