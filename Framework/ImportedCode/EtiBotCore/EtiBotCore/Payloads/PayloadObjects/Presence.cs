using EtiBotCore.Data.JsonConversion;
using EtiBotCore.Payloads.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Payloads.PayloadObjects {
	internal class Presence : PayloadDataObject {

		/// <summary>
		/// The user associated with this event. This may be partial, in which just the ID is populated.
		/// </summary>
		[JsonProperty("user"), JsonRequired]
		public PresenceUser? User { get; set; } = new PresenceUser();

		/// <summary>
		/// The ID of the server that this event has fired from.
		/// </summary>
		[JsonProperty("guild_id")]
		public ulong? GuildID { get; set; }

		/// <summary>
		/// The status of this user. Will never be <see cref="StatusType.Invisible"/>.
		/// </summary>
		[JsonProperty("status"), JsonConverter(typeof(EnumConverter))]
		public StatusType? Status { get; set; } = StatusType.Offline;

		/// <summary>
		/// The user's current activities.
		/// </summary>
		[JsonProperty("activities"), JsonRequired]
		public Activity[]? Activities { get; set; }

		/// <summary>
		/// The status of this client across multiple devices.
		/// </summary>
		[JsonProperty("client_status"), JsonRequired]
		public ClientPlatformStatus? ClientStatus { get; set; }

	}
}
