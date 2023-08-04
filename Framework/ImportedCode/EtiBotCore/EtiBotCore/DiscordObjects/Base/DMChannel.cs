using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Exceptions;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Threading;

namespace EtiBotCore.DiscordObjects.Base {

	/// <summary>
	/// Represents a channel for a direct message.
	/// </summary>

	public class DMChannel : ChannelBase {

		internal static readonly ThreadedDictionary<Snowflake, DMChannel> DMChannelCache = new ThreadedDictionary<Snowflake, DMChannel>();

		internal readonly ThreadedDictionary<Snowflake, Message> Messages = new ThreadedDictionary<Snowflake, Message>();

		/// <summary>
		/// The ID of the application that created this DM if it's a group DM, or <see langword="null"/> if a human started it.
		/// </summary>
		public Snowflake? ApplicationID { get; }

		/// <summary>
		/// The hash of the DM's icon, if it's a group DM.
		/// </summary>
		public string? IconHash { get; private set; } = null;

		/// <summary>
		/// The users in this DM.
		/// </summary>
		public IReadOnlyList<User> Recipients { get; private set; } = new List<User>();

		#region Downloading Messages

		/// <summary>
		/// Gets a message with the given ID from this channel, or downloads it.
		/// </summary>
		/// <returns></returns>
		public async Task<Message> GetMessageAsync(Snowflake id) {
			//Message? msg = Messages.Where(message => message.ID == id).FirstOrDefault();
			if (Messages.TryGetValue(id, out Message? msg)) return msg!;
			var plMessage = (await TextChannel.DownloadMessage.ExecuteAsync<Payloads.PayloadObjects.Message>(new APIRequestData { Params = { ID, id } })).Item1!;
			msg = await Message.GetOrCreateAsync(plMessage);
			return msg;
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

			(var messages, _) = (await TextChannel.DownloadAllMessages.ExecuteAsync<List<Payloads.PayloadObjects.Message>>(request))!;
			List<Message> result = new List<Message>();
			foreach (var plMessage in messages!) {
				if (Messages.TryGetValue(plMessage.ID, out Message? existing)) {
					await existing.Update(plMessage);
				} else {
					existing = await Message.GetOrCreateAsync(plMessage);
					Messages.TryAdd(plMessage.ID, existing);
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

		#region Sending Messages

		/// <summary>
		/// Sends a message in this channel.
		/// </summary>
		/// /// <remarks>
		/// By default, specifying no <paramref name="mentionLimits"/> will allow anything and everything to be pinged, and will also ping the person that's being replied to.
		/// </remarks>
		/// <param name="text">The text to send.</param>
		/// <param name="embed">The embed to send.</param>
		/// <param name="mentionLimits">Limitations to who or what can or can't be mentioned.</param>
		/// <param name="attachments">The files to attach.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">If text is null or empty AND embed is null.</exception>
		public async Task<Message?> SendMessageAsync(string? text = "", Embed? embed = null, AllowedMentions? mentionLimits = null, params FileInfo?[] attachments) {
			if (string.IsNullOrWhiteSpace(text) && embed == null) throw new ArgumentException("Expected at least text or embed to be set, if not both.");

			APIRequestData request = new APIRequestData {
				Params = { ID }
			};
			request.SetFiles(attachments);

			if (!string.IsNullOrWhiteSpace(text)) request.SetJsonField("content", text);
			if (embed != null) request.SetJsonField("embed", new Payloads.PayloadObjects.Embed(embed));
			if (mentionLimits != null) request.SetJsonField("allowed_mentions", mentionLimits);

			(var message, _) = await TextChannel.CreateMessage.ExecuteAsync<Payloads.PayloadObjects.Message>(request);
			if (message != null) return await Message.GetOrCreateAsync(message);
			return null;
		}

		/// <summary>
		/// Sends a message in this channel.
		/// </summary>
		/// /// <remarks>
		/// By default, specifying no <paramref name="mentionLimits"/> will allow anything and everything to be pinged, and will also ping the person that's being replied to.
		/// </remarks>
		/// <param name="text">The text to send.</param>
		/// <param name="embed">The embed to send.</param>
		/// <param name="mentionLimits">Limitations to who or what can or can't be mentioned.</param>
		/// <param name="replyTo">The message to reply to</param>
		/// <param name="attachments">A file to attach</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">If text is null or empty AND embed is null.</exception>
		public async Task<Message?> SendReplyMessageAsync(string? text = "", Embed? embed = null, AllowedMentions? mentionLimits = null, Message? replyTo = null, params FileInfo?[] attachments) {
			if (string.IsNullOrWhiteSpace(text) && embed == null) throw new ArgumentException("Expected at least text or embed to be set, if not both.");

			APIRequestData request = new APIRequestData {
				Params = { ID }
			};
			request.SetFiles(attachments);

			if (!string.IsNullOrWhiteSpace(text)) request.SetJsonField("content", text);
			if (embed != null) request.SetJsonField("embed", new Payloads.PayloadObjects.Embed(embed));
			if (mentionLimits != null) request.SetJsonField("allowed_mentions", mentionLimits);
			if (replyTo != null) request.SetJsonField("message_reference", new MessageReference() {
				MessageID = replyTo.ID
			});

			(var message, _) = await TextChannel.CreateMessage.ExecuteAsync<Payloads.PayloadObjects.Message>(request);
			if (message != null) return await Message.GetOrCreateAsync(message);
			return null;
		}


		/// <summary>
		/// Make it look like the bot is typing.
		/// </summary>
		/// <returns></returns>
		public async Task StartTypingAsync() {
			await TextChannel.TriggerTypingInChannel.ExecuteAsync(new APIRequestData { Params = { ID } });
		}

		#endregion

		/// <inheritdoc/>
		internal DMChannel(Payloads.PayloadObjects.Channel dmc) : base(dmc.ID, ChannelType.DM) {
			ApplicationID = dmc.ApplicationID;
			OwnerID = dmc.OwnerID;
		}

		#region Instantiation and Networking

		/// <summary>
		/// Gets or creates a DM channel from the given ID. This will download the channel if it doesn't exist.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		internal static async Task<DMChannel> GetOrCreateAsync(Snowflake id) {
			if (!DMChannelCache.ContainsKey(id)) {
				(var plChannel, _) = await GetChannel.ExecuteAsync<Payloads.PayloadObjects.Channel>(new APIRequestData() { Params = { id } });
				DMChannelCache[id] = await GetOrCreateAsync(plChannel!);
			}
			return DMChannelCache[id];
		}

		/// <summary>
		/// Gets or creates a DM channel from the given payload.
		/// </summary>
		/// <param name="dmc"></param>
		/// <returns></returns>
		internal static async Task<DMChannel> GetOrCreateAsync(Payloads.PayloadObjects.Channel dmc) {
			Snowflake id = dmc.ID;
			if (!DMChannelCache.ContainsKey(id)) {
				DMChannelCache[id] = new DMChannel(dmc);
			}
			await DMChannelCache[id].Update(dmc, false);
			return DMChannelCache[id];
		}

		/// <inheritdoc/>
		protected internal override async Task Update(PayloadDataObject obj, bool skipNonNullFields = false) {
			if (obj is Payloads.PayloadObjects.Channel channel) {
				IconHash = AppropriateNullableString(IconHash, channel.Icon, skipNonNullFields);

				if (channel.Recipients != null) {
					List<User> newRecipients = new List<User>();
					foreach (var payloadUser in channel.Recipients) {
						if (User.UserExists(payloadUser.UserID)) {
							User? usr = await User.GetOrDownloadUserAsync(payloadUser.UserID);
							if (usr != null) {
								newRecipients.Add(usr);
							}
						} else {
							newRecipients.Add(new User(payloadUser));
						}
					}
				}
			}
		}

		/// <inheritdoc/>
		protected override Task<HttpResponseMessage?> SendChangesToDiscord(IReadOnlyDictionary<string, object> changes, string? reasons) {
			return Task.FromResult<HttpResponseMessage?>(null);
		}



		#endregion

	}
}
