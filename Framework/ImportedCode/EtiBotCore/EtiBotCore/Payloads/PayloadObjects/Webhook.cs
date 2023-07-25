using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.JsonConversion;
using EtiBotCore.Payloads.Data;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// Represents a webhook. 
	/// </summary>
	internal class Webhook : PayloadDataObject {

		/// <summary>
		/// The ID of this webhook.
		/// </summary>
		[JsonProperty("id")]
		public ulong ID { get; set; }

		/// <summary>
		/// What type of webhook this is.
		/// </summary>
		[JsonProperty("type"), JsonConverter(typeof(EnumConverter))]
		public WebhookType Type { get; set; }

		/// <summary>
		/// The server this webhook exists in.
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong? GuildID { get; set; }

		/// <summary>
		/// The channel this webhook targets.
		/// </summary>
		[JsonProperty("channel_id")]
		public ulong ChannelID { get; set; }

		/// <summary>
		/// The user that created this webhook.
		/// </summary>
		[JsonProperty("user")]
		public User? User { get; set; }

		/// <summary>
		/// The default name of the webhook.
		/// </summary>
		[JsonProperty("name")]
		public string? Name { get; set; }

		/// <summary>
		/// The default avatar of this webhook.
		/// </summary>
		[JsonProperty("avatar")]
		public string? Avatar { get; set; }

		/// <summary>
		/// The secure token of this webhook, only returned for Incoming Webhooks.
		/// </summary>
		[JsonProperty("token")]
		public string? Token { get; set; }

		/// <summary>
		/// The bot/OAuth2 application that created this webhook
		/// </summary>
		[JsonProperty("application_id")]
		public ulong? ApplicationID { get; set; }

	}
}
