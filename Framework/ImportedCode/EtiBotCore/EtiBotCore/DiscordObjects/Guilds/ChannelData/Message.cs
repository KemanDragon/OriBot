using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Exceptions.Marshalling;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Payloads.Events.Intents.GuildOrDirectMessages;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Marshalling;
using EtiBotCore.Utility.Threading;

namespace EtiBotCore.DiscordObjects.Guilds.ChannelData {

	/// <summary>
	/// A message in a channel.
	/// </summary>
	
	public class Message : DiscordObject {

		/// <summary>
		/// A binding from message ID to message.
		/// </summary>
		internal static readonly ThreadedDictionary<Snowflake, Message> InstantiatedMessages = new ThreadedDictionary<Snowflake, Message>();

		#region Properties

		/// <summary>
		/// Whether or not this message is shallow. In the event of a network error, this message may not exist until it is edited by the author.<para/>
		/// When messages are edited, only minimal content is sent. If the message is not able to be redownloaded, this will be set to true, which means that it is unsafe to reference most properties.<para/>
		/// TODO: What properties, boy?!
		/// </summary>
		public bool IsShallow { get; internal set; }

		/// <summary>
		/// The channel this message exists in. This could be a DM channel or a guild channel, which can be checked via testing if <see cref="Channel"/> <see langword="is"/> <see cref="GuildChannelBase"/>.
		/// </summary>
		public ChannelBase Channel { get; private set; }

		/// <summary>
		/// The channel this message exists in as a <see cref="TextChannel"/>, or <see langword="null"/> if this is a DM.
		/// </summary>
		public TextChannel? ServerChannel => Channel is TextChannel textChannel ? textChannel : null;

		/// <summary>
		/// The <see cref="Guild"/> this message exists in, or <see langword="null"/> if this is a DM.
		/// </summary>
		public Guild? Server => ServerChannel?.Server;

		/// <summary>
		/// The user that created the message. Will be <see langword="null"/> if the message is a webhook, which can be determined via checking if <see cref="WebhookID"/> is non-<see langword="null"/>.
		/// </summary>
		public User? Author { get; private set; }

		/// <summary>
		/// The member that created the message. Will be <see langword="null"/> if this was sent by a webhook or if this is a DM message.
		/// </summary>
		public Member? AuthorMember { get; private set; }

		/// <summary>
		/// The content of the message, which is its raw text.
		/// </summary>
		/// <remarks>
		/// <strong>This reference is cloned in clone objects.</strong>
		/// </remarks>
		public string Content {
			get => _Content;
			set {
				if (Author != User.BotUser) throw new InvalidOperationException("This message does not belong to the bot and cannot be edited as a result!");
				SetProperty(ref _Content, value);
			}
		}
		private string _Content = string.Empty;

		/// <summary>
		/// Controls the mentions that are allowed in this message.
		/// </summary>
		/// <exception cref="InvalidOperationException">If the message does not belong to the bot.</exception>
		public AllowedMentions? AllowedMentions {
			get => _AllowedMentions;
			set {
				if (Author != User.BotUser) throw new InvalidOperationException("This message does not belong to the bot and cannot be edited as a result!");
				SetProperty(ref _AllowedMentions, value);
			}
		}
		private AllowedMentions? _AllowedMentions = null;

		/// <summary>
		/// When this message was sent.
		/// </summary>
		public DateTimeOffset Timestamp { get; private set; }

		/// <summary>
		/// When this message was edited, or <see langword="null"/> if it has not been edited.
		/// </summary>
		public DateTimeOffset? EditedTimestamp { get; private set; }

		/// <summary>
		/// Whether or not this message uses text to speech.
		/// </summary>
		public bool TTS { get; private set; }

		/// <summary>
		/// Whether or not this message contains @everyone
		/// </summary>
		public bool MentionsEveryone { get; private set; }

		/// <summary>
		/// The users this message pings.
		/// </summary>
		public User[] Mentions { get; private set; } = new User[0];

		/// <summary>
		/// The roles pinged by this message.
		/// </summary>
		public Role[] MentionedRoles { get; private set; } = new Role[0];

		/// <summary>
		/// An array of mentioned channels, which will be <see langword="null"/> if there are no visible mentioned channels.<para/>
		/// Channels in this array must be visible to everyone in a lurkable server.
		/// </summary>
		public ChannelMention[] MentionedChannels { get; private set; }

		/// <summary>
		/// The attachments on this message.
		/// </summary>
		public Attachment[] Attachments { get; private set; } = new Attachment[0];

		/// <summary>
		/// All embeds in this message.
		/// </summary>
		/// <remarks>
		/// <strong>This reference is cloned in clone objects.</strong>
		/// </remarks>
		public Embed[] Embeds { get; private set; } = new Embed[0];

		/// <summary>
		/// The first embed in <see cref="Embeds"/>, or <see langword="null"/> if there are none.
		/// </summary>
		/// <remarks>
		/// Only <see langword="set"/> will throw the given exceptions.
		/// </remarks>
		/// <exception cref="PropertyLockedException">If the object is not in a modifiable state.</exception>
		/// <exception cref="ObjectDeletedException">If this message was deleted.</exception>
		/// <exception cref="InvalidOperationException">If this message does not belong to the bot.</exception>
		public Embed? Embed {
			get => _Embed;
			set {
				if (Author != User.BotUser) throw new InvalidOperationException("This message does not belong to the bot and cannot be edited as a result!");
				SetProperty(ref _Embed, value);
				if (value != null) {
					if (Embeds.Length > 0 && Embeds[0] != value) {
						// Insert
						Embeds = Embeds.Prepend(value).ToArray();
					} else if (Embeds.Length == 0) {
						Embeds = new Embed[] { value };
					}
				} else {
					if (Embeds.Length == 1) {
						Embeds = new Embed[0];
					} else {
						Embeds = Embeds.Skip(1).ToArray();
					}
				}
			}
		}

		private Embed? _Embed = null;

		/// <summary>
		/// Keeps track of the reactions on this message.
		/// </summary>
		/// <remarks>
		/// <strong>This reference is cloned in clone objects.</strong>
		/// </remarks>
		public ReactionContainer Reactions { get; internal set; }

		/// <summary>
		/// Used for validating that a message was sent.<para/>
		/// Discord may send this as an integer or a string. It is classified as an object to allow ambiguity between these two types.
		/// </summary>
		public Variant<string, int>? Nonce { get; private set; }

		/// <summary>
		/// Whether or not this message is pinned.
		/// </summary>
		/// <remarks>
		/// Only <see langword="set"/> will throw the given exceptions. 
		/// </remarks>
		/// <exception cref="InsufficientPermissionException">If the bot cannot pin messages.</exception>
		public bool Pinned {
			get => _Pinned;
			set {
				if (Channel is GuildChannelBase channel) EnforcePermissions(channel, Permissions.ManageMessages);
				SetProperty(ref _Pinned, value);
			}
		}
		internal bool _Pinned = false;

		/// <summary>
		/// The ID of the webhook, or <see langword="null"/> if this was not sent by a webhook.
		/// </summary>
		public Snowflake? WebhookID { get; private set; }

		/// <summary>
		/// The message activity, used for when someone presses that little invite button to send that channel embed that lets you join, or <see langword="null"/> if the message does not use the assoociated feature.
		/// </summary>
		public MessageActivity? Activity { get; private set; }

		/// <summary>
		/// The application in the message, used in conjunction with <see cref="Activity"/>. This is <see langword="null"/> if the message does not have an embed with an application.
		/// </summary>
		public MessageApplication? Application { get; private set; }

		/// <summary>
		/// If this is an announcement message, this is the original message that relayed this announcement. If this is a reply, this is the message it's replying to.
		/// </summary>
		public MessageReference? Reference { get; private set; }

		/// <summary>
		/// The type of message that this is.
		/// </summary>
		public MessageType Type { get; private set; }

		/// <summary>
		/// Extra information about what kind of message this is.
		/// </summary>
		public MessageFlags Flags { get; private set; }

		/// <summary>
		/// Returns <see langword="true"/> if this message is a reply, regardless of its mention state.
		/// </summary>
		/// <remarks>
		/// For cases where knowing if the reply is a ping, use <see cref="IsMentionedReply"/>
		/// </remarks>
		public bool IsReply => Type == MessageType.Reply;

		/// <summary>
		/// Returns <see langword="true"/> if this message is a reply and it pings the person who it's replying to.
		/// </summary>
		public bool IsMentionedReply => IsReply && (AllowedMentions?.PingRepliedUser ?? false);

		/// <summary>
		/// Whether or not embeds are suppressed in this message.
		/// </summary>
		/// <remarks>
		/// Only <see langword="set"/> will throw the given exceptions.
		/// </remarks>
		/// <exception cref="InsufficientPermissionException">If the bot cannot manage messages.</exception>
		/// <exception cref="PropertyLockedException">If the message is not available for editing.</exception>
		/// <exception cref="ObjectDeletedException">If this message was deleted.</exception>
		/// <exception cref="InvalidOperationException">If this message does not belong to the bot (only thrown for a DM message)</exception>
		public bool EmbedsSuppressed {
			get => _EmbedsSuppressed;
			set {
				if (Channel is GuildChannelBase channel) EnforcePermissions(channel, Permissions.ManageMessages);
				else if (Author != User.BotUser) throw new InvalidOperationException("This message does not belong to the bot and cannot be edited as a result!");
				SetProperty(ref _EmbedsSuppressed, value);
				if (value) {
					Flags |= MessageFlags.SuppressesEmbeds;
				} else {
					Flags &= ~MessageFlags.SuppressesEmbeds;
				}
				_EmbedsSuppressed = Flags.HasFlag(MessageFlags.SuppressesEmbeds);
			}
		}
		private bool _EmbedsSuppressed = false;

		/// <summary>
		/// Returns a link that, when clicked, jumps to this message.
		/// </summary>
		/// <exception cref="NotSupportedException">If this is not part of a <see cref="TextChannel"/> or <see cref="DMChannel"/></exception>
		public string JumpLink {
			get {
				if (Channel is TextChannel txt) {
					return $"https://discord.com/channels/{txt.ServerID}/{txt.ID}/{ID}";
				} else if (Channel is DMChannel dm) {
					return $"https://discord.com/channels/@me/{dm.ID}/{ID}";
				} else {
					throw new NotSupportedException();
				}
			}
		}

		#endregion

		#region Extensions

		/// <summary>
		/// Replies to this message, if possible. If <paramref name="asReply"/> is <see langword="true"/>, then this will be sent as a reply, from which <paramref name="mentionLimits"/> will be used to determine whether or not to ping the user.
		/// </summary>
		/// <remarks>
		/// By default, specifying no <paramref name="mentionLimits"/> will allow anything and everything to be pinged, and will also ping the person that's being replied to.
		/// </remarks>
		/// <param name="text">The text to send.</param>
		/// <param name="embed">The embed to send.</param>
		/// <param name="mentionLimits">Limitations to who or what can or can't be mentioned.</param>
		/// <param name="attachments">One or more files to attach.</param>
		/// <param name="asReply">If true, this message will be an actual <em>reply</em> reply. If false, this will just send a message in the same channel as this <see cref="Message"/></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">If text is null or empty AND embed is null.</exception>
		public async Task<Message?> ReplyAsync(string? text = null, Embed? embed = null, AllowedMentions? mentionLimits = null, bool asReply = false, params FileInfo[]? attachments) {
			if (Channel is TextChannel textChannel) {
				return await textChannel.SendReplyMessageAsync(text, embed, mentionLimits, asReply ? this : null, attachments ?? Array.Empty<FileInfo>());
			} else if (Channel is DMChannel dmChannel) {
				return await dmChannel.SendReplyMessageAsync(text, embed, mentionLimits, asReply ? this : null, attachments ?? Array.Empty<FileInfo>());
			}
			throw new NotSupportedException("This channel is not a TextChannel or DMChannel!");
		}

		/// <summary>
		/// Replies to this message, if possible. If <paramref name="asReply"/> is <see langword="true"/>, then this will be sent as a reply, from which <paramref name="mentionLimits"/> will be used to determine whether or not to ping the user.
		/// </summary>
		/// <remarks>
		/// By default, specifying no <paramref name="mentionLimits"/> will allow anything and everything to be pinged, and will also ping the person that's being replied to.
		/// </remarks>
		/// <param name="text">The text to send.</param>
		/// <param name="embed">The embed to send.</param>
		/// <param name="mentionLimits">Limitations to who or what can or can't be mentioned.</param>
		/// <param name="attachment">A file to attach.</param>
		/// <param name="asReply">If true, this message will be an actual <em>reply</em> reply. If false, this will just send a message in the same channel as this <see cref="Message"/></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">If text is null or empty AND embed is null.</exception>
		public async Task<Message?> ReplyAsync(string? text = null, Embed? embed = null, AllowedMentions? mentionLimits = null, FileInfo? attachment = null, bool asReply = false) {
			if (Channel is TextChannel textChannel) {
				return await textChannel.SendReplyMessageAsync(text, embed, mentionLimits, asReply ? this : null, attachment);
			} else if (Channel is DMChannel dmChannel) {
				return await dmChannel.SendReplyMessageAsync(text, embed, mentionLimits, asReply ? this : null, attachment);
			}
			throw new NotSupportedException("This channel is not a TextChannel or DMChannel!");
		}

		/// <summary>
		/// Delete this message with the optionally defined reason for deletion. Throws <see cref="InvalidOperationException"/> if this is a message in a DM, and the message does not belong to this bot.
		/// </summary>
		/// <param name="reason">Why did you delete this message?</param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public Task DeleteAsync(string? reason = null) {
			if (Channel is TextChannel || (Author!.IsSelf && Channel is DMChannel)) {
				if (Channel is TextChannel txt && !Author!.IsSelf) {
					// Text channel, not my message.
					EnforcePermissions(txt.Server, Permissions.ManageMessages);
				}
				return TextChannel.DeleteMessage.ExecuteAsync(new APIRequestData {
					Params = { Channel.ID, ID },
					Reason = reason
				});
			}
			throw new InvalidOperationException("Cannot delete other users' messages in a DM");
		}

		#endregion

		#region Ctor and internals


		/// <summary>
		/// Constructs a new message and adds it to the channel's message registry.
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="channel">The channel this message exists in.</param>
		/// <param name="authorMember">If this message is a server, this is the member that sent it.</param>
		/// <param name="channelMentions">Any mentioned channels.</param>
		/// <param name="mentionedRoles">The roles that were mentioned.</param>
		/// <param name="mentionedUsers">The users that were mentioned.</param>
		private Message(Payloads.PayloadObjects.Message evt, ChannelBase? channel, Member? authorMember, ChannelMention[] channelMentions, Role[] mentionedRoles, User[] mentionedUsers) : base(evt.ID) {
			Reactions = new ReactionContainer(this);

			WebhookID = evt.WebhookID;

			// Start with guild and channel
			if (channel is TextChannel textChannel) {
				Channel = channel;
				// ObjectLogger.WriteLine($"Message {ID} was in guild.");
				textChannel.Messages.TryAdd(ID, this);
			} else if (channel is DMChannel dmChannel) {
				// ObjectLogger.WriteLine($"Message {ID} was in DM.");
				Channel = dmChannel;
				dmChannel.Messages.TryAdd(ID, this);
			} else {
				throw new InvalidOperationException("Invalid channel type for a message!");
			}

			Activity = MessageActivity.CreateFromPayload(evt.Activity);
			Application = MessageApplication.CreateFromPayload(evt.Application);
			Attachments = new Attachment[evt.Attachments.Length];
			for (int idx = 0; idx < Attachments.Length; idx++) {
				Attachments[idx] = Attachment.CreateFromPayload(evt.Attachments[idx])!;
			}
			if (WebhookID == null) {
				ulong userId = evt.Author?.UserID ?? 0;
				if (userId == 0) userId = evt.Member?.User?.UserID ?? 0;
				if (userId == 0) ObjectLogger.WriteCritical("A message with an author ID of 0 was encountered");
				
				Author = User.EventGetOrCreate(evt.Author!);
				if (authorMember != null) AuthorMember = authorMember;
			}
			_Content = evt.Content;
			EditedTimestamp = evt.EditedTimestamp?.DateTime;
			Embeds = new Embed[evt.Embeds.Length];
			for (int idx = 0; idx < Embeds.Length; idx++) {
				Embeds[idx] = new Embed(evt.Embeds[idx]);
			}
			_Embed = Embeds.FirstOrDefault();

			Flags = evt.Flags ?? MessageFlags.None;
			_EmbedsSuppressed = Flags.HasFlag(MessageFlags.SuppressesEmbeds);

			MentionedChannels = channelMentions;
			MentionedRoles = mentionedRoles;
			Mentions = mentionedUsers;

			MentionsEveryone = evt.MentionsEveryone;

			if (evt.Nonce is string nonceStr) {
				Nonce = new Variant<string, int>(nonceStr);
			} else if (evt.Nonce is int nonceInt) {
				Nonce = new Variant<string, int>(nonceInt);
			}

			_Pinned = evt.Pinned;

			//Reaction[] rxns = new Reaction[evt.Reactions?.Length ?? 0];
			Reactions.RawReactionsInternal = new SynchronizedCollection<Reaction>();
			for (int idx = 0; idx < (evt.Reactions?.Length ?? 0); idx++) {
				var plRxn = evt.Reactions![idx];
				//rxns[idx] = Reaction.CreateFromPayload(plRxn)!;
				Reactions.RawReactionsInternal.Add(Reaction.CreateFromPayload(plRxn)!);
			}

			if (evt.Reference != null) {
				Reference = MessageReference.CreateFromPayload(evt.Reference);
			}

			Timestamp = evt.Timestamp.DateTime;
			EditedTimestamp = evt.EditedTimestamp?.DateTime;

			TTS = evt.TTS;
			Type = evt.Type;
		}

		/// <summary>
		/// Gets an existing message from the given payload's ID or creates a new one from the payload's contained information. The message ctor registers it to the channel.
		/// </summary>
		/// <param name="evt"></param>
		/// <returns></returns>
		internal static async Task<Message> GetOrCreateAsync(Payloads.PayloadObjects.Message evt) {
			if (InstantiatedMessages.ContainsKey(evt.ID)) {
				return InstantiatedMessages[evt.ID];
			}
			// ChannelBase? channel, Member? authorMember, ChannelMention[] channelMentions, Role[] mentionedRoles, User[] mentionedUsers

			Snowflake channelId = evt.ChannelID;
			Snowflake? guildId = evt.GuildID;

			ChannelBase? channel = null;
			Guild? guild = null;
			Member? authorMember = null;

			// This is required because sometimes they only send the message ID.
			if (guildId == null) {
				foreach (Guild g in Guild.InstantiatedGuilds.Values) {
					if (!g.Unavailable) {
						channel = g.GetChannel(channelId);
						if (channel != null) {
							guild = g;
							guildId = g.ID;
							break;
						}
					}
				}
			}

			if (channel == null) {
				if (guildId != null) {
					guild = await Guild.GetOrDownloadAsync(guildId.Value, true);
					GuildChannelBase? ch = GuildChannelBase.GetFromCache<GuildChannelBase>(channelId);
					if (ch == null) {
						(var plChannel, _) = await ChannelBase.GetChannel.ExecuteAsync<Payloads.PayloadObjects.Channel>(new APIRequestData() { Params = { channelId } });
						ch = await GuildChannelBase.GetOrCreateAsync<GuildChannelBase>(plChannel!);
					}
					channel = ch;
					if (evt.Author != null) authorMember = await guild.GetMemberAsync(evt.Author.UserID);
				} else {
					(var plChannel, _) = await ChannelBase.GetChannel.ExecuteAsync<Payloads.PayloadObjects.Channel>(new APIRequestData() { Params = { channelId } });
					channel = await DMChannel.GetOrCreateAsync(plChannel!);
				}
			}

			ChannelMention[] channelMentions = new ChannelMention[evt.MentionedChannels?.Length ?? 0];
			if (evt.MentionedChannels != null) {
				for (int idx = 0; idx < channelMentions.Length; idx++) {
					channelMentions[idx] = await ChannelMention.CreateFromPayloadAsync(evt.MentionedChannels[idx]);
				}
			}

			Role[] mentionedRoles;
			if (guild != null) {
				mentionedRoles = new Role[evt.MentionedRoles.Length];
				for (int idx = 0; idx < mentionedRoles.Length; idx++) {
					mentionedRoles[idx] = await Role.GetOrDownloadAsync(evt.MentionedRoles[idx], guild);
				}
			} else {
				mentionedRoles = new Role[0];
			}

			User[] mentionedUsers = new User[evt.Mentions.Length];
			for (int idx = 0; idx < mentionedUsers.Length; idx++) {
				// mentionedUsers[idx] = User.EventGetOrCreate(evt.Mentions[idx]);
				// ^ returns a partial user, I don't want to create a full user from a partial.
				User? usr = await User.GetOrDownloadUserAsync(evt.Mentions[idx].UserID);
				if (usr != null) {
					mentionedUsers[idx] = usr;
				}
			}

			InstantiatedMessages[evt.ID] = new Message(evt, channel, authorMember, channelMentions, mentionedRoles, mentionedUsers);
			return InstantiatedMessages[evt.ID];
		}

		/// <summary>
		/// Updates a given message from the given event. If the message didn't exist prior, it will attempt to download the message.
		/// </summary>
		/// <param name="evt"></param>
		/// <returns></returns>
		internal static async Task UpdateMessageAsync(MessageUpdateEvent evt) {
			if (!InstantiatedMessages.ContainsKey(evt.ID)) {
				(var msg, _) = (await TextChannel.DownloadMessage.ExecuteAsync<Payloads.PayloadObjects.Message>(new APIRequestData {
					Params = { evt.ChannelID, evt.ID }
				}))!;
				InstantiatedMessages[evt.ID] = await GetOrCreateAsync(msg!);
			}
			await InstantiatedMessages[evt.ID].Update(evt, true); // Will have override for content in the update code
			// Skip the lock check too, for messages.
		}

		/// <inheritdoc/>
		protected internal override Task Update(PayloadDataObject obj, bool skipNonNullFields = false) {
			if (obj is Payloads.PayloadObjects.Message msg) {
				_Content = msg.Content;
				_EmbedsSuppressed = msg.Flags?.HasFlag(MessageFlags.SuppressesEmbeds) ?? false;
				if (_EmbedsSuppressed) {
					Flags |= MessageFlags.SuppressesEmbeds;
				} else {
					Flags &= ~MessageFlags.SuppressesEmbeds;
				}
				_Pinned = msg.Pinned;
			}
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		protected override async Task<HttpResponseMessage?> SendChangesToDiscord(IReadOnlyDictionary<string, object> changesAndOriginalValues, string? reasons) {
			bool pinChanged = changesAndOriginalValues.ContainsKey(nameof(Pinned));
			bool infoChanged = false;
			APIRequestData request = new APIRequestData {
				Params = { Channel.ID, ID },
				Reason = reasons
			};

			bool contentExists = true;
			bool embedExists = true;

			if (changesAndOriginalValues.ContainsKey(nameof(Content))) {
				request.SetJsonField("content", Content);
				infoChanged = true;
				if (string.IsNullOrWhiteSpace(Content)) {
					contentExists = false;
				}
			}
			if (changesAndOriginalValues.ContainsKey(nameof(Embed))) {
				request.SetJsonField("embed", Embed != null ? new Payloads.PayloadObjects.Embed(Embed) : null);
				infoChanged = true;
				if (Embed == null) {
					embedExists = false;
				}
			}
			if (!contentExists && !embedExists) {
				throw new InvalidOperationException("Cannot send a blank message. At least one of two components (content and/or embed) must be defined.");
			}
			if (changesAndOriginalValues.ContainsKey(nameof(EmbedsSuppressed))) {
				request.SetJsonField("flags", _EmbedsSuppressed ? (int)MessageFlags.SuppressesEmbeds : 0);
				infoChanged = true;
			}
			if (changesAndOriginalValues.ContainsKey(nameof(AllowedMentions))) {
				request.SetJsonField("allowed_mentions", AllowedMentions);
				infoChanged = true;
			}
			
			if (infoChanged) {
				return await TextChannel.EditMessage.ExecuteAsync(request);
			}
			if (pinChanged) {
				if (Pinned) return await TextChannel.AddPinnedMessage.ExecuteAsync(request);
				else return await TextChannel.RemovePinnedMessage.ExecuteAsync(request);
			}
			return null;
		}

		#endregion

		/// <inheritdoc/>
		public override DiscordObject MemberwiseClone() {
			Message msg = (Message)base.MemberwiseClone();
			msg._Content = _Content;
			msg.Embeds = new Embed[Embeds.Length];
			for (int idx = 0; idx < msg.Embeds.Length; idx++) {
				msg.Embeds[idx] = Embeds[idx].Clone();
			}
			msg.Reactions = Reactions.Clone(msg);
			return msg;
		}
	}
}
