using EtiBotCore.Utility.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// Features a guild might have available to it.
	/// </summary>
	public static class GuildFeatures {

		/// <summary>
		/// This server can set an invite splash.
		/// </summary>
		public const string INVITE_SPLASH = "INVITE_SPLASH";

		/// <summary>
		/// This server can set its voice bitrate to 384kbps (name is misleading)
		/// </summary>
		public const string VIP_REGIONS = "VIP_REGIONS";

		/// <summary>
		/// The server can use a vanity URL.
		/// </summary>
		public const string VANITY_URL = "VANITY_URL";

		/// <summary>
		/// The server is verified.
		/// </summary>
		public const string VERIFIED = "VERIFIED";

		/// <summary>
		/// The server is partnered.
		/// </summary>
		public const string PARTNERED = "PARTNERED";

		/// <summary>
		/// This server is a community server.
		/// </summary>
		public const string COMMUNITY = "COMMUNITY";

		/// <summary>
		/// This server has access to commerce features, such as creating shop channels.
		/// </summary>
		public const string COMMERCE = "COMMERCE";

		/// <summary>
		/// This server can create news channels.
		/// </summary>
		public const string NEWS = "NEWS";

		/// <summary>
		/// This server is on the discovery directory.
		/// </summary>
		public const string DISCOVERABLE = "DISCOVERABLE";

		/// <summary>
		/// This server can be featured in the discovery directory.
		/// </summary>
		public const string FEATURABLE = "FEATURABLE";

		/// <summary>
		/// This server can have an animated icon.
		/// </summary>
		public const string ANIMATED_ICON = "ANIMATED_ICON";

		/// <summary>
		/// This server can have a banner image.
		/// </summary>
		public const string BANNER = "BANNER";

		/// <summary>
		/// This server can add a welcome screen.
		/// </summary>
		public const string WELCOME_SCREEN_ENABLED = "WELCOME_SCREEN_ENABLED";

	}
}
