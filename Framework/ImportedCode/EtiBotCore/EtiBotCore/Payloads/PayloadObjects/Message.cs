using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.JsonConversion;
using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads.Data;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// Represents a message in a server.
	/// </summary>
	internal class Message : PayloadDataObject {

		/// <summary>
		/// The ID of this message.
		/// </summary>
		[JsonProperty("id")]
		public ulong ID { get; set; }

		/// <summary>
		/// The channel this message exists in.
		/// </summary>
		[JsonProperty("channel_id")]
		public ulong ChannelID { get; set; }

		/// <summary>
		/// The server the channel exists in. For direct messages, this is <see langword="null"/>
		/// </summary>
		[JsonProperty("guild_id", NullValueHandling = NullValueHandling.Ignore)]
		public ulong? GuildID { get; set; }

		/// <summary>
		/// The user that created the message. Will be invalid if the message is a webhook, which can be determined via checking if <see cref="WebhookID"/> is non-<see langword="null"/>.
		/// </summary>
		[JsonProperty("author")]
		public User? Author { get; set; } = new User();

		/// <summary>
		/// A partial member object. Will be <see langword="null"/> if this was sent by a webhook.
		/// </summary>
		[JsonProperty("member", NullValueHandling = NullValueHandling.Ignore)]
		public Member? Member { get; set; }

		/// <summary>
		/// The content of the message.
		/// </summary>
		[JsonProperty("content")]
		public string Content { get; set; } = string.Empty;

		/// <summary>
		/// When this message was sent.
		/// </summary>
		[JsonProperty("timestamp")]
		public ISO8601 Timestamp { get; set; }

		/// <summary>
		/// When this message was edited, or <see langword="null"/> if it has not been edited.
		/// </summary>
		[JsonProperty("edited_timestamp")]
		public ISO8601? EditedTimestamp { get; set; }

		/// <summary>
		/// Whether or not this message uses text to speech.
		/// </summary>
		[JsonProperty("tts")]
		public bool TTS { get; set; }

		/// <summary>
		/// Whether or not this message contains @everyone
		/// </summary>
		[JsonProperty("mentions_everyone")]
		public bool MentionsEveryone { get; set; }

		/// <summary>
		/// The users this message pings.
		/// </summary>
		[JsonProperty("mentions")]
		public MessageUserExtension[] Mentions { get; set; } = new MessageUserExtension[0];

		/// <summary>
		/// The roles pinged by this message.
		/// </summary>
		[JsonProperty("mention_roles")]
		public ulong[] MentionedRoles { get; set; } = new ulong[0];

		/// <summary>
		/// An array of mentioned channels, which will be <see langword="null"/> if there are no visible mentioned channels.<para/>
		/// Channels in this array must be visible to everyone in a lurkable server.
		/// </summary>
		[JsonProperty("mention_channels", NullValueHandling = NullValueHandling.Ignore)]
		public ChannelMention[]? MentionedChannels { get; set; }

		/// <summary>
		/// The attachments on this message.
		/// </summary>
		[JsonProperty("attachments")]
		public Attachment[] Attachments { get; set; } = new Attachment[0];

		/// <summary>
		/// The embeds in this message.
		/// </summary>
		[JsonProperty("embeds")]
		public Embed[] Embeds { get; set; } = new Embed[0];

		/// <summary>
		/// The reactions that have been added to this message.
		/// </summary>
		[JsonProperty("reaction", NullValueHandling = NullValueHandling.Ignore)]
		public Reaction[]? Reactions { get; set; }

		/// <summary>
		/// Used for validating that a message was sent.<para/>
		/// Discord may send this as an integer or a string. It is classified as an object to allow ambiguity between these two types.
		/// </summary>
		[JsonProperty("nonce", NullValueHandling = NullValueHandling.Ignore)]
		public object? Nonce { get; set; }

		/// <summary>
		/// Whether or not this message is pinned.
		/// </summary>
		[JsonProperty("pinned")]
		public bool Pinned { get; set; }

		/// <summary>
		/// The ID of the webhook, or <see langword="null"/> if this was not sent by a webhook.
		/// </summary>
		[JsonProperty("webhook_id", NullValueHandling = NullValueHandling.Ignore)]
		public ulong? WebhookID { get; set; }

		/// <summary>
		/// The type of message that this is.
		/// </summary>
		[JsonProperty("type"), JsonConverter(typeof(EnumConverter))]
		public MessageType Type { get; set; }
		
		/// <summary>
		/// The message activity, used for when someone presses that little invite button to send that channel embed that lets you join, or <see langword="null"/> if the message does not use the assoociated feature.
		/// </summary>
		[JsonProperty("activity", NullValueHandling = NullValueHandling.Ignore)]
		public MessageActivity? Activity { get; set; }

		/// <summary>
		/// The application in the message, used in conjunction with <see cref="Activity"/>. This is <see langword="null"/> if the message does not have an embed with an application.
		/// </summary>
		[JsonProperty("application", NullValueHandling = NullValueHandling.Ignore)]
		public MessageApplication? Application { get; set; }

		/// <summary>
		/// If this is an announcement message, this is the original message that relayed this announcement.
		/// </summary>
		[JsonProperty("message_reference", NullValueHandling = NullValueHandling.Ignore)]
		public MessageReference? Reference { get; set; }

		/// <summary>
		/// Extra information about what kind of message this is.
		/// </summary>
		[JsonProperty("flags", NullValueHandling = NullValueHandling.Ignore)]
		public MessageFlags? Flags { get; set; }

		/// <summary>
		/// If this is a message in a thread, here's the whole freakin channel again for you.
		/// </summary>
		[JsonProperty("thread")]
		public Channel? Thread { get; set; }

		/// <summary>
		/// Stickers sent in the message.
		/// </summary>
		[JsonProperty("sticker_items")]
		public Sticker[]? Stickers { get; set; }

	}
}
