using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Factory;
using static EtiBotCore.DiscordObjects.Factory.SendableAPIRequestFactory;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Exceptions.Marshalling;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Extension;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using System.Threading.Tasks;
using System.Linq;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.Client;
using System.Net.Http;
using System.IO;
using EtiBotCore.Utility.Threading;
using System.Collections.Concurrent;

namespace EtiBotCore.DiscordObjects.Guilds {

	/// <summary>
	/// A text channel in a guild.
	/// </summary>

	public class TextChannel : GuildChannelBase {

		#region Requests

		/// <summary>
		/// Params: <c>channelId</c>
		/// </summary>
		internal static readonly SendableAPIRequestFactory DownloadAllMessages = new SendableAPIRequestFactory("channels/{0}/messages", HttpRequestType.Get);

		/// <summary>
		/// Params: <c>channelId, messageId</c>
		/// </summary>
		/// <remarks>
		/// Returns a message object
		/// </remarks>
		internal static readonly SendableAPIRequestFactory DownloadMessage = new SendableAPIRequestFactory("channels/{0}/messages/{1}", HttpRequestType.Get);

		/// <summary>
		/// Params: <c>channelId</c><para/>
		/// JSON: <para/>
		/// <code>
		/// content: string,<para/>
		/// nonce: unique ID, Variant&lt;string, int&gt;<para/>
		/// tts: bool,<para/>
		/// files: data,<para/>
		/// embed: an embed,<para/>
		/// payload_json: json encoded body of other request fields,<para/>
		/// allowed_mentions: <see href="https://discord.com/developers/docs/resources/channel#allowed-mentions-object">Allowed Mentions Object</see><para/>
		/// message_reference: <see href="https://discord.com/developers/docs/resources/channel#message-object-message-reference-structure">Message Reference Structure</see>
		/// </code>
		/// </summary>
		internal static readonly SendableAPIRequestFactory CreateMessage = new SendableAPIRequestFactory("channels/{0}/messages", HttpRequestType.Post);

		/// <summary>
		/// Params: <c>channelId, messageId</c>
		/// </summary>
		internal static readonly SendableAPIRequestFactory CrosspostMessage = new SendableAPIRequestFactory("channels/{0}/messages/{1}/crosspost", HttpRequestType.Post);

		/// <summary>
		/// Params: <c>channelId, messageId, emoji (URL Encoded)</c>
		/// </summary>
		/// <remarks>
		/// Malformed emojis will raise error 10014 Unknown Emoji
		/// </remarks>
		internal static readonly SendableAPIRequestFactory CreateReaction = new SendableAPIRequestFactory("channels/{0}/messages/{1}/reactions/{2}/@me", HttpRequestType.Put);

		/// <summary>
		/// Params: <c>channelId, messageId, emoji (URL Encoded)</c>
		/// </summary>
		/// <remarks>
		/// Malformed emojis will raise error 10014 Unknown Emoji
		/// </remarks>
		internal static readonly SendableAPIRequestFactory DeleteOwnReaction = new SendableAPIRequestFactory("channels/{0}/messages/{1}/reactions/{2}/@me", HttpRequestType.Delete);

		/// <summary>
		/// Params: <c>channelId, messageId, emoji (URL Encoded), userId</c>
		/// </summary>
		/// <remarks>
		/// Malformed emojis will raise error 10014 Unknown Emoji
		/// </remarks>
		internal static readonly SendableAPIRequestFactory DeleteUserReaction = new SendableAPIRequestFactory("channels/{0}/messages/{1}/reactions/{2}/{3}", HttpRequestType.Delete);

		/// <summary>
		/// Params: <c>channelId, messageId, emoji (URL Encoded)</c>
		/// </summary>
		/// <remarks>
		/// Malformed emojis will raise error 10014 Unknown Emoji
		/// </remarks>
		internal static readonly SendableAPIRequestFactory GetReactions = new SendableAPIRequestFactory("channels/{0}/messages/{1}/reactions/{2}", HttpRequestType.Get);

		/// <summary>
		/// Params: <c>channelId, messageId</c>
		/// </summary>
		internal static readonly SendableAPIRequestFactory DeleteAllReactions = new SendableAPIRequestFactory("channels/{0}/messages/{1}/reactions", HttpRequestType.Delete);

		/// <summary>
		/// Params: <c>channelId, messageId, emoji (URL Encoded)</c>
		/// </summary>
		internal static readonly SendableAPIRequestFactory DeleteAllReactionsForEmoji = new SendableAPIRequestFactory("channels/{0}/messages/{1}/reactions/{2}", HttpRequestType.Delete);

		/// <summary>
		/// Params: <c>channelId, messageId</c><para/>
		/// JSON: <para/>
		/// <code>
		/// content: string <para/>
		/// embed: embed object <para/>
		/// flags: new flags (only SUPPRESS_EMBEDS is allowed) <para/>
		/// allowed_mentions: <see href="https://discord.com/developers/docs/resources/channel#allowed-mentions-object">Allowed Mentions Object</see><para/>
		/// </code>
		/// </summary>
		internal static readonly SendableAPIRequestFactory EditMessage = new SendableAPIRequestFactory("channels/{0}/messages/{1}", HttpRequestType.Patch);

		/// <summary>
		/// Params: <c>channelId, messageId</c>
		/// </summary>
		internal static readonly SendableAPIRequestFactory DeleteMessage = new SendableAPIRequestFactory("channels/{0}/messages/{1}", HttpRequestType.Delete);

		/// <summary>
		/// Params: <c>channelId</c><para/>
		/// JSON: <para/>
		/// <c>messages: array of snowflakes</c>
		/// </summary>
		/// <remarks>
		/// This endpoint will not delete messages older than 2 weeks, and will fail with a 400 BAD REQUEST if any message provided is older than that or if any duplicate message IDs are provided.
		/// </remarks>
		internal static readonly SendableAPIRequestFactory BulkDeleteMessages = new SendableAPIRequestFactory("channels/{0}/messages/bulk-delete", HttpRequestType.Post);

		/// <summary>
		/// Params: <c>channelId, userOrRoleId</c><para/>
		/// JSON: <para/>
		/// <code>
		/// allow: bitwise value of allowed permissions (as string)<para/>
		/// deny: bitwise value of denied permissions (as string)<para/>
		/// type: 0 if this modifies a role, 1 if it's a member.
		/// </code>
		/// </summary>
		/// <remarks>
		/// Values not included in allow or deny will be changed to inherited.
		/// </remarks>
		internal static readonly SendableAPIRequestFactory EditChannelPermissions = new SendableAPIRequestFactory("channels/{0}/permissions/{1}", HttpRequestType.Put);

		/// <summary>
		/// Params: <c>channelId</c><para/>
		/// </summary>
		/// <remarks>
		/// Returns a list of invite objects.
		/// </remarks>
		internal static readonly SendableAPIRequestFactory GetChannelInvites = new SendableAPIRequestFactory("channels/{0}/invites", HttpRequestType.Get);

		/// <summary>
		/// Params: <c>channelId</c><para/>
		/// JSON: <para/>
		/// <code>
		/// max_age: Duration of the invite in seconds before it expires, or 0 for never<para/>
		/// max_uses: The maximum amount of times it can be used, or 0 for unlimited<para/>
		/// temporary: If true, any users that join the server through this invite, <em>don't</em> get any roles, and log off, will be booted from the server.<para/>
		/// unique: If true, don't try to reuse a similar invite (useful for creating many one-time invites)<para/>
		/// target_user?: The target user ID for this invite (as a string)<para/>
		/// target_user_type?: the type of user (int)
		/// </code>
		/// </summary>
		/// <remarks>
		/// Returns the new invite
		/// </remarks>
		internal static readonly SendableAPIRequestFactory CreateChannelInvite = new SendableAPIRequestFactory("channels/{0}/invites", HttpRequestType.Post);

		/// <summary>
		/// Params: <c>channelId, userOrRoleId</c>
		/// </summary>
		internal static readonly SendableAPIRequestFactory DeleteChannelPermission = new SendableAPIRequestFactory("channels/{0}/permissions/{1}", HttpRequestType.Delete);

		/// <summary>
		/// Params: <c>channelId</c><para/>
		/// JSON: <para/>
		/// <code>
		/// webhook_channel_id: The ID of the target channel
		/// </code>
		/// </summary>
		internal static readonly SendableAPIRequestFactory FollowNewsChannel = new SendableAPIRequestFactory("channels/{0}/followers", HttpRequestType.Post);

		/// <summary>
		/// Params: <c>channelId</c>
		/// </summary>
		/// <remarks>
		/// Advised to not use this if it can be avoided.
		/// </remarks>
		internal static readonly SendableAPIRequestFactory TriggerTypingInChannel = new SendableAPIRequestFactory("channels/{0}/typing", HttpRequestType.Post);

		/// <summary>
		/// Params: <c>channelId</c>
		/// </summary>
		/// <remarks>
		/// Returns all pinned messages in the channel as an array of message objects.
		/// </remarks>
		internal static readonly SendableAPIRequestFactory GetPinnedMessages = new SendableAPIRequestFactory("channels/{0}/pins", HttpRequestType.Get);

		/// <summary>
		/// Params: <c>channelId, messageIdToPin</c>
		/// </summary>
		internal static readonly SendableAPIRequestFactory AddPinnedMessage = new SendableAPIRequestFactory("channels/{0}/pins/{1}", HttpRequestType.Put);

		/// <summary>
		/// Params: <c>channelId, messageIdToUnpin</c>
		/// </summary>
		internal static readonly SendableAPIRequestFactory RemovePinnedMessage = new SendableAPIRequestFactory("channels/{0}/pins/{1}", HttpRequestType.Delete);

		#endregion

		internal readonly ThreadedDictionary<Snowflake, Message> Messages = new ThreadedDictionary<Snowflake, Message>();

		private bool HasDownloadedMessagesForBulk = false;

		#region Properties

		/// <summary>
		/// Whether or not this channel is NSFW. For <see cref="Thread"/>s, 
		/// this reflects on the <see cref="NSFW"/> property of the <strong>parent channel</strong>.
		/// </summary>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot edit this property.</exception>
		public virtual bool NSFW {
			get => _NSFW;
			set {
				EnforcePermissions(this, Payloads.Data.Permissions.ManageChannels);
				SetProperty(ref _NSFW, value);
			}
		}
		private bool _NSFW = false;

		/// <summary>
		/// Slow-mode timer duration in seconds.
		/// </summary>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot edit this property.</exception>
		public int RateLimitPerUser {
			get => _RateLimitPerUser;
			set {
				EnforcePermissions(this, Payloads.Data.Permissions.ManageChannels);
				SetProperty(ref _RateLimitPerUser, value);
			}
		}
		private int _RateLimitPerUser = 0;

		/// <summary>
		/// The topic of this channel, more commonly known as the channel description.<para/>
		/// This does not exist on <see cref="Thread"/>s, and attempting to reference this on a <see cref="Thread"/> will raise an <see cref="InvalidOperationException"/>.
		/// </summary>
		/// <exception cref="InvalidOperationException">If this is referenced in any way on a <see cref="Thread"/>.</exception>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the permissions needed to do this.</exception>
		/// <exception cref="ObjectDeletedException">If this object has been deleted and cannot be edited.</exception>
		/// <exception cref="ArgumentNullException">If the topic is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If it is longer than 1024 characters.</exception>
		public string Topic {
			get {
				if (Type.IsThreadChannel()) {
					throw new InvalidOperationException("Threads do not have channel topics.");
				}
				return _Topic;
			}
			set {
				if (Type.IsThreadChannel()) {
					throw new InvalidOperationException("Threads do not have channel topics.");
				}
				if (value == null) throw new ArgumentNullException(nameof(value));
				if (value.Length > 1024) throw new ArgumentOutOfRangeException(nameof(value), "The topic is too long!");
				EnforcePermissions(Server!, Payloads.Data.Permissions.ManageChannels);
				SetProperty(ref _Topic, value);
			}
		}
		private string _Topic = string.Empty;

		#endregion

		#region Extended Properties

		/// <summary>
		/// A reference to all threads of this channel. This raises <see cref="InvalidOperationException"/> if this is called on a thread.
		/// </summary>
		/// <exception cref="InvalidOperationException">If this is used on a thread.</exception>
		public IEnumerable<Thread> Threads {
			get {
				if (Type.IsThreadChannel()) {
					throw new InvalidOperationException("Threads cannot have threads of their own.");
				}
				return Server.Threads.Where(thread => thread.ParentID == ID);
			}
		}

		#endregion

		#region Threads

		/// <summary>
		/// Creates a new thread in this channel. Returns the new thread, or <see langword="null"/> if creation failed.
		/// </summary>
		/// <param name="name">The name of the thread. Must be between 1-100 chars.</param>
		/// <param name="archiveAfter">How long the thread must be inactive before archival.</param>
		/// <param name="isPrivate">Whether or not the thread is private. Cannot be true on threads of news channels.</param>
		/// <param name="reason">Why this thread is being created.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">If name is longer than 100 chars or less than 1 char.</exception>
		/// <exception cref="ArgumentException">If name is null or empty, the archive duration is not a predefined value, or isPrivate is true on a news thread.</exception>
		public virtual async Task<Thread?> CreateNewThread(string name, ThreadArchiveDuration archiveAfter, bool isPrivate, string? reason = null) {
			if (string.IsNullOrWhiteSpace(name) || name.Length > 100) throw new ArgumentOutOfRangeException(nameof(name));
			if (Type == ChannelType.News && isPrivate) throw new ArgumentException("Cannot create a private thread in a news channel.");
			if (!Enum.IsDefined(typeof(ThreadArchiveDuration), archiveAfter)) throw new ArgumentException("Invalid archive duration.");

			APIRequestData request = new APIRequestData {
				Params = {
					ID
				},
				Reason = reason
			};
			ChannelType properThreadType;
			if (Type == ChannelType.News) {
				properThreadType = ChannelType.NewsThread;
			} else {
				properThreadType = isPrivate ? ChannelType.PrivateThread : ChannelType.PublicThread;
			}
			request.SetJsonField("name", name);
			request.SetJsonField("auto_archive_duration", (int)archiveAfter);
			request.SetJsonField("type", (int)properThreadType);
			(var plChannel, var _) = await Thread.CreateNewThreadInstance.ExecuteAsync<Payloads.PayloadObjects.Channel>(request);
			if (plChannel != null) {
				Thread thread = await GetOrCreateAsync<Thread>(plChannel, Server);
				return thread;
			}
			return null;
		}

		#endregion

		#region Downloading Messages

		/// <summary>
		/// Gets a message with the given ID from this channel, or downloads it.
		/// </summary>
		/// <returns></returns>
		public async Task<Message> GetMessageAsync(Snowflake id) {
			if (Messages.TryGetValue(id, out Message? msg)) return msg;
			var plMessage = (await DownloadMessage.ExecuteAsync<Payloads.PayloadObjects.Message>(new APIRequestData { Params = { ID, id } })).Item1!;
			msg = await Message.GetOrCreateAsync(plMessage);
			return msg;
		}

		/// <summary>
		/// Tries to pull a message out of cache. Returns <see langword="null"/> if it wasn't downloaded.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Message? GetMessageFromCache(Snowflake id) {
			if (Messages.TryGetValue(id, out Message? msg)) return msg;
			return null;
		}

		private async Task<Message[]> GetAllMessagesAsync(Snowflake? before, Snowflake? after, Snowflake? around, int amount) {
			if (amount < 0 || amount > 100) throw new ArgumentOutOfRangeException(nameof(amount), "Expected an integer value in the range [0, 100]");

			APIRequestData request = new APIRequestData {
				Params = { ID }
			};

			request.SetJsonField("limit", amount);
			if (before != null) {
				request.SetJsonField("before", before.Value);
			} else if (after != null) {
				request.SetJsonField("after", after.Value);
			} else if (around != null) {
				request.SetJsonField("around", around.Value);
			}

			(var messages, _) = (await DownloadAllMessages.ExecuteAsync<List<Payloads.PayloadObjects.Message>>(request))!;
			if (messages == null) {
				ObjectLogger.WriteWarning("Failed to download messages for channel! Trying again in 5s...");
				await Task.Delay(5000);
				(messages, _) = (await DownloadAllMessages.ExecuteAsync<List<Payloads.PayloadObjects.Message>>(request))!;
			}

			if (messages == null) {
				ObjectLogger.WriteCritical("Failed to download messages!");
				return new Message[0];
			}

			List<Message> result = new List<Message>();
			foreach (var plMessage in messages) {
				//Message? existing = Messages.Find(message => message.ID == plMessage.ID);
				
				if (Messages.TryGetValue(plMessage.ID, out Message? existing)) {
					await existing.Update(plMessage);
				} else {
					existing = await Message.GetOrCreateAsync(plMessage);
					Messages[plMessage.ID] = existing;
				}
				result.Add(existing);
			}
			Message[] returnArray = result.ToArray();
			Array.Sort(returnArray, (self, other) => {
				if (other is null) return -1;
				if (self.ID.Timestamp < other.ID.Timestamp) return -1;
				if (self.ID.Timestamp > other.ID.Timestamp) return 1;
				return 0;
			});
			return returnArray;
		}

		/// <summary>
		/// Downloads all messages in this channel and populates them into this channel's message storage. The messages are guaranteed to be in chronological order.
		/// </summary>
		/// <param name="amount">MAX 100. The amount of messages to download.</param>
		/// <exception cref="ArgumentOutOfRangeException">If amount is less than 0 or greater than 100.</exception>
		/// <returns></returns>
		public Task<Message[]> GetAllMessagesAsync(int amount) => GetAllMessagesAsync(null, null, null, amount);

		/// <summary>
		/// Returns all messages in this channel sent before the given time. If a message was sent at this exact time (down to the millisecond), it will not be included.
		/// </summary>
		/// <param name="time">The latest time that a message can be sent at to qualify for download.</param>
		/// <param name="amount">MAX 100. The amount of messages to download.</param>
		/// <returns></returns>
		public Task<Message[]> GetMessagesBeforeAsync(DateTimeOffset time, int amount) => GetMessagesBeforeAsync(Snowflake.FromDateTimeOffset(time, true), amount);

		/// <summary>
		/// Returns all messages in this channel sent before the given message. If a message was sent at this exact time (down to the millisecond), it will not be included.
		/// </summary>
		/// <param name="message">The latest message that can be downloaded (non-inclusive -- this message won't be included in the list)</param>
		/// <param name="amount">MAX 100. The amount of messages to download.</param>
		/// <returns></returns>
		public Task<Message[]> GetMessagesBeforeAsync(Snowflake message, int amount) => GetAllMessagesAsync(message, null, null, amount);

		/// <summary>
		/// Returns all messages in this channel sent after the given time. If a message was sent at this exact time (down to the millisecond), it will not be included.
		/// </summary>
		/// <param name="time">The latest time that a message can be sent at to qualify for download.</param>
		/// <param name="amount">MAX 100. The amount of messages to download.</param>
		/// <returns></returns>
		public Task<Message[]> GetMessagesAfterAsync(DateTimeOffset time, int amount) => GetMessagesAfterAsync(Snowflake.FromDateTimeOffset(time, true), amount);

		/// <summary>
		/// Returns all messages in this channel sent after the given message. If a message was sent at this exact time (down to the millisecond), it will not be included.
		/// </summary>
		/// <param name="message">The latest message that can be downloaded (non-inclusive -- this message won't be included in the list)</param>
		/// <param name="amount">MAX 100. The amount of messages to download.</param>
		/// <returns></returns>
		public Task<Message[]> GetMessagesAfterAsync(Snowflake message, int amount) => GetAllMessagesAsync(null, message, null, amount);

		/// <summary>
		/// Returns all messages in this channel sent around the given time. How many is "around"? No idea! Discord doesn't say.
		/// </summary>
		/// <param name="time">The latest time that a message can be sent at to qualify for download.</param>
		/// <param name="amount">MAX 100. The amount of messages to download.</param>
		/// <returns></returns>
		public Task<Message[]> GetMessagesAroundAsync(DateTimeOffset time, int amount) => GetMessagesAroundAsync(Snowflake.FromDateTimeOffset(time), amount);

		/// <summary>
		/// Returns all messages in this channel sent around the given message. How many is "around"? No idea! Discord doesn't say.
		/// </summary>
		/// <param name="message">The latest message that can be downloaded (non-inclusive -- this message won't be included in the list)</param>
		/// <param name="amount">MAX 100. The amount of messages to download.</param>
		/// <returns></returns>
		public Task<Message[]> GetMessagesAroundAsync(Snowflake message, int amount) => GetAllMessagesAsync(null, null, message, amount);

		#endregion

		#region Sending / Deleting Messages

		/// <summary>
		/// Sends a message in this channel. Returns the <see cref="Message"/> that was created, or <see langword="null"/> if the message failed to send.
		/// </summary>
		/// /// <remarks>
		/// By default, specifying no <paramref name="mentionLimits"/> will allow anything and everything to be pinged, and will also ping the person that's being replied to.
		/// </remarks>
		/// <param name="text">The text to send.</param>
		/// <param name="embed">The embed to send.</param>
		/// <param name="mentionLimits">Limitations to who or what can or can't be mentioned.</param>
		/// <param name="attachments">One or more files to attach.</param>
		/// <returns></returns>
		/// <exception cref="InsufficientPermissionException">If the bot cannot send a message in this channel.</exception>
		/// <exception cref="ArgumentException">If text is null or empty AND embed is null.</exception>
		public async Task<Message?> SendMessageAsync(string? text = "", Embed? embed = null, AllowedMentions? mentionLimits = null, params FileInfo?[] attachments) {
			// if (string.IsNullOrWhiteSpace(text) && embed == null) throw new ArgumentException("Expected at least text or embed to be set, if not both.");

			APIRequestData request = new APIRequestData {
				Params = { ID }
			};
			request.SetFiles(attachments);

			if (!string.IsNullOrWhiteSpace(text)) request.SetJsonField("content", text);
			if (embed != null) request.SetJsonField("embed", new Payloads.PayloadObjects.Embed(embed));
			if (mentionLimits != null) request.SetJsonField("allowed_mentions", mentionLimits);

			(var message, _) = await CreateMessage.ExecuteAsync<Payloads.PayloadObjects.Message>(request);
			if (message != null) return await Message.GetOrCreateAsync(message);
			return null;
		}

		/// <summary>
		/// Sends a message in this channel. Returns the <see cref="Message"/> that was created, or <see langword="null"/> if the message failed to send.
		/// </summary>
		/// /// <remarks>
		/// By default, specifying no <paramref name="mentionLimits"/> will allow anything and everything to be pinged, and will also ping the person that's being replied to.
		/// </remarks>
		/// <param name="text">The text to send.</param>
		/// <param name="embed">The embed to send.</param>
		/// <param name="mentionLimits">Limitations to who or what can or can't be mentioned.</param>
		/// <param name="replyTo">The message to reply to</param>
		/// <param name="attachments">A file to attach.</param>
		/// <returns></returns>
		/// <exception cref="InsufficientPermissionException">If the bot cannot send a message in this channel.</exception>
		/// <exception cref="ArgumentException">If text is null or empty AND embed is null.</exception>
		public async Task<Message?> SendReplyMessageAsync(string? text = "", Embed? embed = null, AllowedMentions? mentionLimits = null, Message? replyTo = null, params FileInfo?[] attachments) {
			// if (string.IsNullOrWhiteSpace(text) && embed == null) throw new ArgumentException("Expected at least text or embed to be set, if not both.");

			APIRequestData request = new APIRequestData {
				Params = { ID }
			};
			request.SetFiles(attachments);

			if (!string.IsNullOrWhiteSpace(text)) request.SetJsonField("content", text);
			if (embed != null) request.SetJsonField("embed", new Payloads.PayloadObjects.Embed(embed));
			if (mentionLimits != null) request.SetJsonField("allowed_mentions", mentionLimits);
			if (replyTo != null) request.SetJsonField("message_reference", new MessageReference() {
				GuildID = ServerID,
				MessageID = replyTo.ID
			});

			(var message, _) = await CreateMessage.ExecuteAsync<Payloads.PayloadObjects.Message>(request);
			if (message != null) return await Message.GetOrCreateAsync(message);
			return null;
		}

		/// <summary>
		/// Make it look like the bot is typing. Does nothing in channels that this bot cannot send messages in.
		/// </summary>
		/// <returns></returns>
		public async Task StartTypingAsync() {
			await TriggerTypingInChannel.ExecuteAsync(new APIRequestData { Params = { ID } });
		}

		/// <summary>
		/// This will iterate through all messages in this channel (from most recent to oldest) and store up to <paramref name="limit"/> messages that satisfy the given condition.
		/// </summary>
		/// <param name="messageSelector">A predicate that can be used to select messages. Input null to use the latest <paramref name="limit"/> messages.</param>
		/// <param name="limit">The maximum number of messages to handle. 100 is Discord's limit, and any amount less than 2 is not acceptable (use DeleteMessageAsync instead)</param>
		/// <param name="reason">Why are the messages being deleted?</param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">If limit is over 100 or less than 2.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot manage messages in this channel.</exception>
		public async Task DeleteMessagesAsync(Predicate<Message>? messageSelector, int limit = 100, string? reason = null) {
			if (limit > 100 || limit < 1) {
				throw new ArgumentOutOfRangeException(nameof(limit));
			}

			if (!HasDownloadedMessagesForBulk) {
				await GetAllMessagesAsync(100);
				HasDownloadedMessagesForBulk = true;
			}

			Message[] messages = Messages.Values.ToArray();
			Array.Sort(messages);
			messages = messages.Reverse().ToArray(); // lol

			if (limit == 1) {
				// Special behavior
				Message single = messages.First(msg => messageSelector?.Invoke(msg) ?? true);
				await single.DeleteAsync(reason);
				return;
			}

			List<Snowflake> selectedMessages = new List<Snowflake>();
			for (int i = 0; i < messages.Length; i++) {
				Message msg = messages[i];
				if (messageSelector?.Invoke(msg) ?? true) {
					if (!selectedMessages.Contains(msg.ID)) {
						selectedMessages.Add(msg.ID);
						if (selectedMessages.Count >= limit) {
							break;
						}
					}
				}
			}

			APIRequestData bulkDeleteRequest = new APIRequestData {
				Params = { ID },
				Reason = reason
			};
			bulkDeleteRequest.SetJsonField("messages", selectedMessages);
			await BulkDeleteMessages.ExecuteAsync(bulkDeleteRequest);
		}

		#endregion

		#region Internals & Implementation

		/// <summary>
		/// For <see cref="Thread"/> only. This allows creating a <see cref="TextChannel"/> as a thread.
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="inServer"></param>
		/// <param name="type"></param>
		internal TextChannel(Payloads.PayloadObjects.Channel channel, Guild inServer, ChannelType type) : base(channel, inServer, type) { }

		internal TextChannel(Payloads.PayloadObjects.Channel channel, Guild inServer) : base(channel, inServer, channel.Type) {
			Task updateTask = Update(channel, false);
			updateTask.Wait();
		}

		/// <inheritdoc/>
		protected internal override async Task Update(PayloadDataObject obj, bool skipNonNullFields = false) {
			await base.Update(obj, skipNonNullFields);
			if (obj is Payloads.PayloadObjects.Channel channel) {
				if (!channel.Type.IsThreadChannel()) {
					_Topic = AppropriateValue(Topic, channel.Topic!, skipNonNullFields);
					_NSFW = channel.NSFW.GetValueOrDefault();
				}
				_RateLimitPerUser = channel.RateLimitPerUser.GetValueOrDefault();
			}
		}

		/// <inheritdoc/>
		protected override async Task<HttpResponseMessage?> SendChangesToDiscord(IReadOnlyDictionary<string, object> changes, string? reasons) {
			APIRequestData request = await SendChangesToDiscordCustom(changes, reasons);
			if (changes.ContainsKey("Topic")) request.SetJsonField("topic", _Topic);
			if (changes.ContainsKey("NSFW")) request.SetJsonField("nsfw", _NSFW);
			if (changes.ContainsKey("RateLimitPerUser")) request.SetJsonField("rate_limit_per_user", RateLimitPerUser);
			return await ModifyChannel.ExecuteAsync(request);
		}
		#endregion
	}
}
