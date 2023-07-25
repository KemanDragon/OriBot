using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Universal;

namespace EtiBotCore.DiscordObjects.Guilds.MemberData {

	/// <summary>
	/// Represents an entry in the ban logs.
	/// </summary>
	
	public class Ban {

		/// <summary>
		/// The reason this user was banned.
		/// </summary>
		public string Reason { get; }

		/// <summary>
		/// The username#discriminator of the banned user, e.g. Eti#1760
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// The ID of the banned user.
		/// </summary>
		public Snowflake ID { get; }

		/// <summary>
		/// The guild that this user was banned from.
		/// </summary>
		public Guild Server { get; }

		internal Ban(Guild server, Payloads.PayloadObjects.Ban banObj) {
			Reason = banObj.Reason;
			Name = banObj.User.Username + "#" + banObj.User.Discriminator;
			ID = banObj.User.ID;
			Server = server;
		}

	}
}
