using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.Exceptions.Marshalling;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Threading;
using EtiLogger.Logging;

namespace EtiBotCore.DiscordObjects.Universal {

	/// <summary>
	/// A container class for reactions on a message.
	/// </summary>
	
	public class ReactionContainer {

		private static Logger ReactionLogger = new Logger("[Reaction Container] ") {
			DefaultLevel = LogLevel.Trace
		};

		/// <summary>
		/// The message these reactions are on.
		/// </summary>
		public Message Message { get; }

		/// <summary>
		/// If true, this will download reactions of a given emoji once added.
		/// </summary>
		/// <remarks>
		/// This <strong>will</strong> cause extensive network overhead and may result in rate limits occurring. <strong>It should <em>only</em> be enabled on messages sent <em>before bot initialization</em> that need their reactions tracked in detail.</strong> Messages sent post-init will be able to stay up to date on their own.
		/// </remarks>
		public bool EagerTracking { get; set; } = false;

		/// <summary>
		/// Tracks emojis downloaded by the event container when <see cref="EagerTracking"/> is <see langword="true"/>
		/// </summary>
		private readonly SynchronizedCollection<Emoji> DownloadedEmojis = new SynchronizedCollection<Emoji>();

		/// <summary>
		/// The reactions that have been added to this message in their raw form.<para/>
		/// These only store the actual emojis on the message and the amount of emojis that were added. This does not container member information (<em>who</em> added the emojis).
		/// </summary>
		/// <remarks>
		/// <strong>This may be more reliable than <see cref="ReactionsByEmoji"/> and <see cref="ReactionsByUser"/></strong>. This is because data sent by Discord does not always include the members associated with a reaction (unless explicitly requested).
		/// </remarks>
		public IReadOnlyList<Reaction> RawReactions => RawReactionsInternal.ToList().AsReadOnly();
		internal SynchronizedCollection<Reaction> RawReactionsInternal = new SynchronizedCollection<Reaction>();

		/// <summary>
		/// The reactions on this message indexed by the emoji that one or more users may have added. Each emoji corresponds to a list of the users that have reacted with that emoji.
		/// </summary>
		/// <remarks>
		/// <strong>This risks being out of sync from <see cref="RawReactions"/>.</strong> Discord does not send <em>who</em> reacted by default, only what the reactions are and how many there are.
		/// </remarks>
		public IReadOnlyDictionary<Emoji, IReadOnlyList<User>> ReactionsByEmoji {
			get {
				if (ReactionsByEmojiCache != null && !ReactionsByEmojiChanged) return ReactionsByEmojiCache;
				Dictionary<Emoji, IReadOnlyList<User>> bindings = new Dictionary<Emoji, IReadOnlyList<User>>();
				foreach (KeyValuePair<Emoji, SynchronizedCollection<User>> userBinding in ReactionsByEmojiInternal) {
					bindings.Add(userBinding.Key, userBinding.Value.ToList().AsReadOnly());
				}
				ReactionsByEmojiCache = bindings;
				return bindings;
			}
		}
		private IReadOnlyDictionary<Emoji, IReadOnlyList<User>>? ReactionsByEmojiCache = null;

		/// <summary>
		/// The reactions on this message indexed by a user that has added one or more reactions. Each user corresponds to a list of the emojis they have reacted with.
		/// </summary>
		/// <remarks>
		/// <strong>This risks being out of sync from <see cref="RawReactions"/>.</strong> Discord does not send <em>who</em> reacted by default, only what the reactions are and how many there are.
		/// </remarks>
		public IReadOnlyDictionary<User, IReadOnlyList<Emoji>> ReactionsByUser {
			get {
				if (ReactionsByUserCache != null && !ReactionsByUserChanged) return ReactionsByUserCache;
				Dictionary<User, IReadOnlyList<Emoji>> bindings = new Dictionary<User, IReadOnlyList<Emoji>>();
				foreach (KeyValuePair<User, SynchronizedCollection<Emoji>> emojiBinding in ReactionsByUserInternal) {
					bindings.Add(emojiBinding.Key, emojiBinding.Value.ToList().AsReadOnly());
				}
				ReactionsByUserCache = bindings;
				return bindings;
			}
		}
		private IReadOnlyDictionary<User, IReadOnlyList<Emoji>>? ReactionsByUserCache = null;

		private readonly ThreadedDictionary<Emoji, SynchronizedCollection<User>> ReactionsByEmojiInternal = new ThreadedDictionary<Emoji, SynchronizedCollection<User>>();

		private readonly ThreadedDictionary<User, SynchronizedCollection<Emoji>> ReactionsByUserInternal = new ThreadedDictionary<User, SynchronizedCollection<Emoji>>();

		private bool ReactionsByEmojiChanged = true;

		private bool ReactionsByUserChanged = true;

		internal ReactionContainer(Message forMessage) {
			Message = forMessage;
		}

		/// <summary>
		/// Returns whether or not the given user has reacted to this message with the given emoji.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="emoji"></param>
		/// <returns></returns>
		public bool HasReactionFrom(User user, Emoji emoji) {
			if (ReactionsByEmoji.TryGetValue(emoji, out IReadOnlyList<User>? reactors)) {
				return reactors!.Contains(user);
			}
			return false;
		}

		/// <summary>
		/// Returns the amount of reactions on this message of the given emoji using <see cref="RawReactions"/>.
		/// </summary>
		/// <param name="emoji"></param>
		/// <returns></returns>
		public int GetNumberOfReactions(Emoji emoji) {
			Reaction raw = RawReactions.FirstOrDefault(reaction => reaction.Emoji == emoji);
			if (raw == null) return 0;
			return raw.Count;
		}

		/// <summary>
		/// Returns the amount of distinct reactions on this message using <see cref="RawReactions"/>.
		/// </summary>
		/// <returns></returns>
		public int GetNumberOfUniqueReactions() {
			List<Emoji> counted = new List<Emoji>();
			return RawReactions.Count(rxn => {
				if (rxn.Count == 0) return false;
				// ^ Don't count zero-emoji reactions.

				if (!counted.Contains(rxn.Emoji)) {
					counted.Add(rxn.Emoji);
					return true;
				}
				return false;
			});
		}

		/// <summary>
		/// Returns the amount of reactions on this message of the given emoji, excluding reactions from the given users.
		/// </summary>
		/// <param name="emoji"></param>
		/// <param name="exclude">A list of users to exclude. If they have reacted with this emoji, it will not be counted.</param>
		/// <returns></returns>
		public int GetNumberOfReactionsExcluding(Emoji emoji, params User[] exclude) {
			if (ReactionsByEmoji.ContainsKey(emoji)) {
				IReadOnlyList<User> users = ReactionsByEmoji[emoji];
				if (exclude != null && exclude.Length > 0) return users.Where(user => !exclude.Contains(user)).Count();
				return users.Count;
			}
			return 0;
		}

		/// <summary>
		/// Returns the amount of reactions on this message of the given emoji, only counting reactions from the given users.
		/// </summary>
		/// <param name="emoji"></param>
		/// <param name="include">A list of users to include. Only these users will be searched for.</param>
		/// <returns></returns>
		public int GetNumberOfReactionsFrom(Emoji emoji, params User[] include) {
			if (ReactionsByEmoji.ContainsKey(emoji)) {
				IReadOnlyList<User> users = ReactionsByEmoji[emoji];
				if (include != null && include.Length > 0) return users.Where(user => include.Contains(user)).Count();
			}
			return 0;
		}

		/// <summary>
		/// Adds a reaction to this message. Requires <see cref="Permissions.AddReactions"/>.
		/// </summary>
		/// <param name="emoji">The emoji to add.</param>
		/// <param name="reason">Why was this reaction added?</param>
		/// <returns></returns>
		/// <exception cref="InsufficientPermissionException">If the bot cannot add reactions.</exception>
		/// <exception cref="ArgumentNullException">If emoji is null.</exception>
		/// <exception cref="ObjectDeletedException">If the emoji or message was deleted.</exception>
		public async Task AddReactionAsync(Emoji emoji, string? reason = null) {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
			if (emoji == null) throw new ArgumentNullException(nameof(emoji));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
			if (Message.Deleted) throw new ObjectDeletedException(Message);
			if (emoji.Deleted) throw new ObjectDeletedException(emoji);
			if (Message.Channel is TextChannel textChannel) {
				DiscordObject.EnforcePermissions(textChannel, Permissions.AddReactions);
			}

			await TextChannel.CreateReaction.ExecuteAsync(new APIRequestData { Params = { Message.Channel.ID, Message.ID, emoji.ToURLEncoding() }, Reason = reason });
		}

		/// <summary>
		/// Removes the reaction made by the given user of the given emoji. Requires <see cref="Permissions.ManageMessages"/>.
		/// </summary>
		/// <param name="user">The user that created the reaction we want to remove.</param>
		/// <param name="emoji">The emoji to remove.</param>
		/// <param name="reason">Why was this reaction removed?</param>
		/// <exception cref="InsufficientPermissionException">If the bot cannot manage messages, and this is not the bot's message.</exception>
		/// <exception cref="InvalidOperationException">If the bot attempts to remove a reaction added by another user in a DM.</exception>
		/// <exception cref="ObjectDeletedException">If the message was deleted.</exception>
		/// <exception cref="ArgumentNullException">If either user or emoji are null.</exception>
		public async Task RemoveReactionAsync(User user, Emoji emoji, string? reason = null) {
			// First, test: Have they actually reacted with this emoji?
			// If not, do nothing.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
			if (emoji == null) throw new ArgumentNullException(nameof(emoji));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
			if (Message.Deleted) throw new ObjectDeletedException(Message);

			if (!ReactionsByUser.ContainsKey(user)) return;
			if (!ReactionsByUser[user].Contains(emoji)) return;

			if (Message.Channel is TextChannel textChannel) {
				if (!user.IsSelf) {
					DiscordObject.EnforcePermissions(textChannel, Permissions.ManageMessages);
					await TextChannel.DeleteUserReaction.ExecuteAsync(new APIRequestData { Params = { textChannel.ID, Message.ID, emoji.ToURLEncoding(), user.ID }, Reason = reason });
				} else {
					await TextChannel.DeleteOwnReaction.ExecuteAsync(new APIRequestData { Params = { textChannel.ID, Message.ID, emoji.ToURLEncoding() }, Reason = reason });
				}
			} else if (Message.Channel is DMChannel dm) { 
				if (!user.IsSelf) {
					throw new InvalidOperationException("You cannot remove reactions added by the message recipient, only your own.");
				}
				await TextChannel.DeleteOwnReaction.ExecuteAsync(new APIRequestData { Params = { dm.ID, Message.ID, emoji.ToURLEncoding() }, Reason = reason });
			}
		}

		/// <summary>
		/// Removes all of the reactions of the given emoji. Requires <see cref="Permissions.ManageMessages"/>
		/// </summary>
		/// <param name="emoji">The emoji to remove.</param>
		/// <param name="reason">Why were all reactions of this emoji removed?</param>
		/// <param name="performDeletionOnSuccess">If the request succeeded, this will manually set the reaction count for the given emoji to 0.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">If the emoji is null</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the manage messages permission.</exception>
		/// <exception cref="NotSupportedException">If this is called in a DM.</exception>
		/// <exception cref="ObjectDeletedException">If the message was deleted.</exception>
		public async Task RemoveReactionsOfEmojiAsync(Emoji emoji, string? reason = null, bool performDeletionOnSuccess = false) {
			if (Message.Deleted) throw new ObjectDeletedException(Message);

			if (Message.Channel is TextChannel textChannel) {
				DiscordObject.EnforcePermissions(textChannel, Permissions.ManageMessages);
				var response = await TextChannel.DeleteAllReactionsForEmoji.ExecuteAsync(new APIRequestData { Params = { textChannel.ID, Message.ID, emoji.ToURLEncoding() }, Reason = reason });
				if (response!.IsSuccessStatusCode && performDeletionOnSuccess) {
					foreach (Reaction rxn in RawReactionsInternal) {
						if (rxn.Emoji == emoji) {
							rxn.Count = 0;
						}
					}
					if (ReactionsByEmojiInternal.ContainsKey(emoji)) {
						SynchronizedCollection<User> users = ReactionsByEmojiInternal[emoji];
						ReactionsByEmojiInternal[emoji].Clear();
						ReactionsByEmojiChanged = true;

						foreach (User user in users) {
							ReactionsByUserInternal[user]?.Remove(emoji);
						}
						ReactionsByUserChanged = true;
					}
				}
			} else if (Message.Channel is DMChannel _) {
				throw new NotSupportedException("Cannot use Remove Reactions for Emoji gateway in DMs yet.");
			}
		}

		/// <summary>
		/// Removes every reaction from this message. Requires <see cref="Permissions.ManageMessages"/>
		/// </summary>
		/// <returns></returns>
		/// <param name="reason">Why were all of the reactions removed?</param>
		/// <param name="performDeletionOnSuccess">If the request succeeded, this will manually set the reaction count to 0.</param>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the manage messages permission.</exception>
		/// <exception cref="NotSupportedException">If this is called in a DM.</exception>
		/// <exception cref="ObjectDeletedException">If the message was deleted.</exception>
		public async Task RemoveAllReactionsAsync(string? reason = null, bool performDeletionOnSuccess = false) {
			if (Message.Deleted) throw new ObjectDeletedException(Message);

			if (Message.Channel is TextChannel textChannel) {
				DiscordObject.EnforcePermissions(textChannel, Permissions.ManageMessages);
				var response = await TextChannel.DeleteAllReactions.ExecuteAsync(new APIRequestData { Params = { textChannel.ID, Message.ID }, Reason = reason });
				if (response!.IsSuccessStatusCode && performDeletionOnSuccess) {
					RawReactionsInternal.Clear();
					ReactionsByEmojiInternal.Clear();
					ReactionsByUserInternal.Clear();
					ReactionsByEmojiChanged = true;
					ReactionsByUserChanged = true;
				}
			} else if (Message.Channel is DMChannel _) {
				throw new NotSupportedException("Cannot use Remove All Reactions gateway in DMs yet.");
			}
		}

		/// <summary>
		/// Checks if the reactions for the given emoji might be desynchronized by looking at the amount of users in <see cref="ReactionsByEmoji"/> compared to the count for this emoji in <see cref="RawReactions"/>.<para/>
		/// In short, this tests if <see cref="RawReactions"/> and <see cref="ReactionsByEmoji"/> agree with eachother in terms of how many people reacted. This may not be perfect, but it should be a good enough gauge for typical usage.
		/// </summary>
		/// <param name="emoji">The emoji to check for desyncs.</param>
		/// <returns></returns>
		public bool AreReactionsDesynchronized(Emoji emoji) {
			Reaction container = RawReactions.Where(rxn => rxn.Emoji.Name == emoji.Name || (rxn.Emoji.ID != null && rxn.Emoji.ID == emoji.ID)).FirstOrDefault();
			if (container == null) {
				// Container is null, so is the registry null or empty
				if (ReactionsByEmoji.ContainsKey(emoji) && ReactionsByEmoji[emoji].Count == 0) return false;
				else if (!ReactionsByEmoji.ContainsKey(emoji)) return false;
				return true;
			} else {
				// Container is not null, does the registry exist and have the same count
				if (ReactionsByEmoji.ContainsKey(emoji) && ReactionsByEmoji[emoji].Count == container.Count) return false;
				return true;
			}
		}

		/// <summary>
		/// Downloads the reactions for the given emoji including user information, and populates <see cref="ReactionsByEmoji"/>, <see cref="ReactionsByUser"/>, and <see cref="RawReactions"/> with the data.<para/>
		/// This will test <see cref="AreReactionsDesynchronized(Emoji)"/> unless <paramref name="force"/> is <see langword="true"/>.<para/>
		/// By default, if the amount of downloaded reactors is less than or equal to the amount of users that are registered to have reacted right now, the list of reactors will be replaced with the downloaded list to ensure the list is as clean as possible. If the amount downloaded is less than the amount of registered reactors, the list will not be cleared (because that would throw out some reactors) unless <paramref name="clearAnyway"/> is <see langword="true"/>.
		/// </summary>
		/// <param name="emoji">The emoji to acquire.</param>
		/// <param name="users">The amount of users to acquire. Maximum 100.</param>
		/// <param name="force">If <see langword="true"/>, the reactions will be downloaded anyway.</param>
		/// <param name="clearAnyway">If <paramref name="users"/> is less than the amount of reactions stored right now, and if this is <see langword="true"/>, the registry will be <em>replaced</em> with the new downloaded list instead of merged. This will effectively discard a number of reactors, but guarantees the list is as clean as possible.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="emoji"/> is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="users"/> is less than 0 or greater than 100.</exception>
		/// <exception cref="Exception">If the request fails and the reactions could not be downloaded.</exception>
		public async Task DownloadReactions(Emoji emoji, int users = 100, bool force = false, bool clearAnyway = false) {
			if (users < 0 || users > 100) throw new ArgumentOutOfRangeException(nameof(users));
			if (!force && !AreReactionsDesynchronized(emoji)) {
				// Reactions are not desynced, not forcing a download
				return; // abort
			}

			APIRequestData request = new APIRequestData {
				Params = { Message.Channel.ID, Message.ID, emoji.ToURLEncoding() }
			};
			request.SetJsonField("limit", users);

			(var reactors, var response) = await TextChannel.GetReactions.ExecuteAsync<Payloads.PayloadObjects.User[]>(request);
			// muh yt reaction channel !!!1

			if (reactors != null && response!.IsSuccessStatusCode) {
				Message.ObjectLogger.WriteLine($"Reactions: Downloaded {reactors.Length} reactors.", LogLevel.Trace);
				if (ReactionsByEmoji.ContainsKey(emoji) && (ReactionsByEmoji[emoji].Count < reactors.Length || clearAnyway)) {
					RemoveAllReactionsOf(emoji);
				}
				foreach (var plUser in reactors) {
					User user = User.EventGetOrCreate(plUser);
					AddReactionFrom(user, emoji);
				}
				Reaction container = RawReactions.FirstOrDefault(rxn => rxn.Emoji.Name == emoji.Name || rxn.Emoji.ID == emoji.ID);
				if (container == null) {
					RawReactionsInternal.Add(new Reaction(emoji) {
						SelfIncluded = ReactionsByEmoji.ContainsKey(emoji) && ReactionsByEmoji[emoji].Contains(User.BotUser),
						Count = reactors.Length
					});
				} else {
					container.Count = reactors.Length;
					container.SelfIncluded = ReactionsByEmoji.ContainsKey(emoji) && ReactionsByEmoji[emoji].Contains(User.BotUser);
				}
			} else {
				string message = $"Unable to acquire reactions for emoji {emoji.Name} due to a network error!\n{response!.StatusCode} {response.RequestMessage}";
				Message.ObjectLogger.WriteCritical(message);
				throw new Exception(message); // TODO: More precise Exception
			}
		}

		/// <summary>
		/// Register the given user and reaction.
		/// </summary>
		/// <remarks>
		/// This does not send a network request, and is exclusively for internal storage.
		/// </remarks>
		/// <param name="user"></param>
		/// <param name="emoji"></param>
		internal void AddReactionFrom(User user, Emoji emoji) {
			if (!ReactionsByUserInternal.ContainsKey(user)) {
				ReactionsByUserInternal[user] = new SynchronizedCollection<Emoji>();
			}
			if (!ReactionsByEmojiInternal.ContainsKey(emoji)) {
				ReactionsByEmojiInternal[emoji] = new SynchronizedCollection<User>();
			}
			if (!ReactionsByUserInternal[user].Contains(emoji)) ReactionsByUserInternal[user].Add(emoji);
			if (!ReactionsByEmojiInternal[emoji].Contains(user)) ReactionsByEmojiInternal[emoji].Add(user);
			ReactionsByEmojiChanged = true;
			ReactionsByUserChanged = true;

			Reaction container = RawReactions.Where(rxn => rxn.Emoji.Name == emoji.Name || (rxn.Emoji.ID != null && rxn.Emoji.ID == emoji.ID)).FirstOrDefault();
			if (container == null) {
				RawReactionsInternal.Add(new Reaction(emoji) {
					SelfIncluded = user.IsSelf,
					Count = ReactionsByEmoji[emoji].Count
				});
			} else {
				container.Count = ReactionsByEmoji[emoji].Count;
				container.SelfIncluded = ReactionsByEmoji[emoji].Contains(User.BotUser);
			}
		}

		/// <summary>
		/// Unregister the given user and reaction. This will leave a potentially empty list behind rather than removing it from the dictionary.
		/// </summary>
		/// <remarks>
		/// This does not send a network request, and is exclusively for internal storage.
		/// </remarks>
		/// <param name="user"></param>
		/// <param name="emoji"></param>
		internal void RemoveReactionFrom(User user, Emoji emoji) {
			// note to future self: Remove will just do nothing if the item isn't in the list, no worries about KeyNotFoundException or whatever.
			if (ReactionsByUserInternal.ContainsKey(user)) {
				ReactionsByUserInternal[user].Remove(emoji);
				ReactionsByUserChanged = true;
			}
			if (ReactionsByEmojiInternal.ContainsKey(emoji)) {
				ReactionsByEmojiInternal[emoji].Remove(user);
				ReactionsByEmojiChanged = true;
			}

			Reaction container = RawReactions.Where(rxn => rxn.Emoji.Name == emoji.Name || (rxn.Emoji.ID != null && rxn.Emoji.ID == emoji.ID)).FirstOrDefault();
			if (container != null) {
				container.Count = ReactionsByEmoji[emoji].Count;
				container.SelfIncluded = ReactionsByEmoji[emoji].Contains(User.BotUser);
			}
		}

		/// <summary>
		/// Remove all of the reactions of the given emoji from the list.
		/// </summary>
		/// <remarks>
		/// This does not send a network request, and is exclusively for internal storage.
		/// </remarks>
		internal void RemoveAllReactionsOf(Emoji emoji) {
			if (ReactionsByEmojiInternal.ContainsKey(emoji)) {
				ReactionsByEmojiInternal[emoji].Clear();
			} else {
				ReactionsByEmojiInternal[emoji] = new SynchronizedCollection<User>();
			}
			foreach (KeyValuePair<User, SynchronizedCollection<Emoji>> emojiBinding in ReactionsByUserInternal) {
				emojiBinding.Value.Remove(emoji);
			}
			ReactionsByEmojiChanged = true;
			ReactionsByUserChanged = true;

			Reaction container = RawReactions.Where(rxn => rxn.Emoji.Name == emoji.Name || (rxn.Emoji.ID != null && rxn.Emoji.ID == emoji.ID)).FirstOrDefault();
			container.Count = 0;
			container.SelfIncluded = false;
		}

		/// <summary>
		/// Resets this <see cref="ReactionContainer"/> to an empty state.
		/// </summary>
		internal void RemoveAll() {
			ReactionsByEmojiInternal.Clear();
			ReactionsByUserInternal.Clear();
			ReactionsByEmojiCache = null;
			ReactionsByUserCache = null;
			RawReactionsInternal.Clear();
		}

		/// <summary>
		/// Returns <see langword="true"/> if reactions have not been downloaded for this given emoji. Also sets the state of the download to true, but does NOT download them.
		/// </summary>
		/// <returns></returns>
		internal bool ShouldEagerDownload(Emoji emoji) {
			if (Message.Deleted) {
				// Never!
				EagerTracking = false; 
				return false;
			}
			if (DownloadedEmojis.Count >= 20) {
				// Max potential reached. Any new emojis past this point will be acquired through standard means.
				EagerTracking = false;
				return false;
			}

			bool hasDownloadedAlready = DownloadedEmojis.Contains(emoji);
			if (!hasDownloadedAlready) {
				DownloadedEmojis.Add(emoji);
				ReactionLogger.WriteLine($"Should download all reactions for emoji {emoji}");
			}

			return !hasDownloadedAlready;
		}

		/// <summary>
		/// Returns a deep-copy of this <see cref="ReactionContainer"/>
		/// </summary>
		/// <returns></returns>
		internal ReactionContainer Clone(Message cloneMessage) {
			ReactionContainer clone = new ReactionContainer(cloneMessage);
			foreach (Reaction rxn in RawReactionsInternal) {
				clone.RawReactionsInternal.Add(rxn.Clone());
			}
			return clone;
		}

	}
}
