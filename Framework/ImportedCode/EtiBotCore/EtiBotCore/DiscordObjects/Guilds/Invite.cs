using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Universal;

namespace EtiBotCore.DiscordObjects.Guilds {

	/// <summary>
	/// Represents an invite to a server.
	/// </summary>
	
	public class Invite {

		/// <summary>
		/// When this invite was created.
		/// </summary>
		public DateTimeOffset CreatedAt { get; internal set; }

		/// <summary>
		/// The ID of the server this invite exists for.
		/// </summary>
		public Snowflake? Server { get; internal set; }

		/// <summary>
		/// The ID of the channel this invite leads to.
		/// </summary>
		public Snowflake Channel { get; internal set; }

		/// <summary>
		/// The code of this invite.
		/// </summary>
		public string Code { get; internal set; } = string.Empty;

		/// <summary>
		/// The user who created the invite, or <see langword="null"/> if this is a vanity URL.
		/// </summary>
		public PartialUser? Inviter { get; internal set; }

		/// <summary>
		/// The time that the invite is valid for in seconds. This will be zero if it's infinite.
		/// </summary>
		public int MaxAge { get; internal set; }

		/// <summary>
		/// The maximum amount of times the invite can be used. This will be zero if it's infinite.
		/// </summary>
		public int MaxUses { get; internal set; }

		/// <summary>
		/// The user this invite was sent to, or <see langword="null"/> if this invite was not created for someone in specific.
		/// </summary>
		public PartialUser? TargetUser { get; internal set; }

		/// <summary>
		/// Whether or not this invite grants a temporary membership - If the user logs off and has no roles, they will be removed from the server.
		/// </summary>
		public bool Temporary { get; internal set; }

	}
}
