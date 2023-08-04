using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.Utility;

namespace EtiBotCore.DiscordObjects.Universal {

	/// <summary>
	/// A partial user, which contains the raw information about them, such as their name#discriminator, ID, and avatar.<para/>
	/// Note that equality checks between <see cref="PartialUser"/> instances are by reference only, and they are not updated by any payload events (so they will contain outdated information if the user changes their name#discriminator or avatar)
	/// </summary>
	
	public class PartialUser {

		/// <summary>
		/// The ID of this partial user.
		/// </summary>
		public Snowflake ID { get; }

		/// <summary>
		/// The URL to this user's avatar, or their Discord-assigned default avatar if they don't have one set.
		/// </summary>
		public Uri AvatarURL => AvatarHash != null ? HashToUriConverter.GetUserAvatar(ID, AvatarHash)! : HashToUriConverter.GetUserDefaultAvatar(int.Parse(Discriminator));

		/// <summary>
		/// The has to this user's avatar, or <see langword="null"/> if they don't have done.
		/// </summary>
		public string? AvatarHash { get; }

		/// <summary>
		/// This user's discriminiator, which is the four digits after their username, e.g. <c>1760</c> in <c>Eti#1760</c>.
		/// </summary>
		public string Discriminator { get; } = "0000";

		/// <summary>
		/// The user's username.
		/// </summary>
		public string Username { get; } = string.Empty;

		/// <summary>
		/// This user's full name, which is their username#discriminator e.g. <c>Eti#1760</c>.
		/// </summary>
		public string FullName => Username + "#" + Discriminator;

		internal PartialUser(Snowflake id, string username, string discrim, string? avatarHash) {
			Username = username;
			ID = id;
			Discriminator = discrim;
			AvatarHash = avatarHash;
		}

	}
}
