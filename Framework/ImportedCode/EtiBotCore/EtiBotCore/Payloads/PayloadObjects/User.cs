using EtiBotCore.Payloads.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.PayloadObjects {
	
	/// <summary>
	/// Represents a Discord User.
	/// </summary>
	internal class User : PayloadDataObject {

		/// <summary>
		/// The user's unique ID.
		/// </summary>
		[JsonProperty("id")]
		public ulong UserID { get; set; }

		/// <summary>
		/// The user's username.
		/// </summary>
		[JsonProperty("username"), JsonRequired]
		public string Username { get; set; } = string.Empty;

		/// <summary>
		/// The user's discriminator, or, the four digits after their name.
		/// </summary>
		[JsonProperty("discriminator"), JsonRequired]
		public string Discriminator { get; set; } = string.Empty;

		/// <summary>
		/// The user's avatar hash. The location of the image is at /avatars/userID/THIS_HASH.fileformat. <see langword="null"/> if they do not have an avatar. The string <c>%NULL%</c> if this is not sent (used to differentiate between "data wasn't sent" and "user has no avatar")
		/// </summary>
		[JsonProperty("avatar", NullValueHandling = NullValueHandling.Ignore)]
		public string? AvatarHash { get; set; } = string.Empty;

		/// <summary>
		/// Whether or not this user is a bot.
		/// </summary>
		[JsonProperty("bot")]
		public bool? IsBot { get; set; } = false;

		/// <summary>
		/// Whether or not this is a system user, which is used in the urgent message system.
		/// </summary>
		[JsonProperty("system")]
		public bool? IsSystem { get; set; } = false;

		/// <summary>
		/// Whether or not this user has multi-factor authentication enabled.
		/// </summary>
		[JsonProperty("mfa_enabled")]
		public bool? MFAEnabled { get; set; } = false;

		/// <summary>
		/// This user's chosen language option.
		/// </summary>
		[JsonProperty("locale")]
		public string? Locale { get; set; }

		/// <summary>
		/// Whether or not this user has a verified email. This will be <see langword="null"/> if the bot does not have the email oauth2 grant.
		/// </summary>
		[JsonProperty("verified")]
		public bool? EmailVerified { get; set; } = null;

		/// <summary>
		/// This user's email, or <see langword="null"/> if the bot does not have the email oauth2 grant.
		/// </summary>
		[JsonProperty("email")]
		public string? Email { get; set; } = null;

		/// <summary>
		/// Identical to <see cref="Flags"/> -- Would presumably contain undocumented flags (e.g. <c>1 &lt;&lt; 4</c>), but it does not.
		/// </summary>
		[Obsolete, JsonProperty("flags")]
		private UserFlags? PrivateFlags { get; set; } = null;

		/// <summary>
		/// The type of Nitro subscription this user has.
		/// </summary>
		[JsonProperty("premium_type")]
		public PremiumType? PremiumType { get; set; } = null;

		/// <summary>
		/// The attributes this user has.
		/// </summary>
		[JsonProperty("public_flags")]
		public UserFlags? Flags { get; set; } = null;

	}


	/// <summary>
	/// Identical to <see cref="User"/>, but it has a partial member object as a field.
	/// </summary>
	internal class MessageUserExtension : User {

		/// <summary>
		/// A partial member that is sent with this user.
		/// </summary>
		[JsonProperty("member")]
		public Member? Member { get; set; } = null;

	}

}
