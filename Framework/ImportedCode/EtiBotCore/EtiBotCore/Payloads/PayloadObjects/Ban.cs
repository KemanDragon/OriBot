#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.Structs;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// Represents a ban object.
	/// </summary>
	internal class Ban {

		/// <summary>
		/// The reason for the ban.
		/// </summary>
		[JsonProperty("reason")]
		public string Reason { get; } = string.Empty;

		[JsonProperty("user")]
		public BanUser User { get; }


		internal class BanUser {

			/// <summary>
			/// The username of this banned user.
			/// </summary>
			[JsonProperty("username")]
			public string Username { get; }

			/// <summary>
			/// The discriminator of this banned user.
			/// </summary>
			[JsonProperty("discriminator")]
			public string Discriminator { get; }

			/// <summary>
			/// The ID of this banned user.
			/// </summary>
			[JsonProperty("id")]
			public Snowflake ID { get; }
			
			/// <summary>
			/// The hash of this user's avatar.
			/// </summary>
			[JsonProperty("avatar")]
			public string AvatarHash { get; }

		}

	}
}
