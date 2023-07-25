using EtiBotCore.Data.Structs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// Represents a member in a server rather than just a user.
	/// </summary>
	internal class Member : PayloadDataObject {

		/// <summary>
		/// The underlying <see cref="User"/> that this member is built on. <strong>Not included in MESSAGE_CREATE and MESSAGE_UPDATE events.</strong>
		/// </summary>
		[JsonProperty("user", NullValueHandling = NullValueHandling.Ignore)]
		public User? User { get; set; }

		/// <summary>
		/// This member's nickname in this server, or <see langword="null"/> if they don't have one.
		/// </summary>
		[JsonProperty("nick")]
		public string? Nickname { get; set; }

		/// <summary>
		/// The roles this member has by ID.
		/// </summary>
		[JsonProperty("roles")]
		public ulong[] Roles { get; set; } = new ulong[0];

		/// <summary>
		/// When this member joined the server.
		/// </summary>
		[JsonProperty("joined_at")]
		public ISO8601 JoinedAt { get; set; }

		/// <summary>
		/// When this member subscribed to Nitro, or <see langword="null"/> if they are not subscribed.
		/// </summary>
		[JsonProperty("premium_since", NullValueHandling = NullValueHandling.Ignore)]
		public ISO8601? PremiumSince { get; set; }

		/// <summary>
		/// Whether or not this user is deafened in voice chat.
		/// </summary>
		[JsonProperty("deaf")]
		public bool Deafened { get; set; }

		/// <summary>
		/// Whether or not this user is muted in voice chat.
		/// </summary>
		[JsonProperty("mute")]
		public bool Muted { get; set; }

		/// <summary>
		/// Whether or not this member has passed the rules screening step.
		/// </summary>
		[JsonProperty("pending")]
		public bool? Pending { get; set; }

	}
}
