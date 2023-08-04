using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EtiBotCore.Payloads.Data;

namespace EtiBotCore.Data.Container {

	/// <summary>
	/// Stores information on what features a guild has enabled.
	/// </summary>
	
	public sealed class GuildFeatureInformation {

		/// <inheritdoc cref="GuildFeatures.ANIMATED_ICON"/>
		public bool CanUseAnimatedIcon => Features.Contains(GuildFeatures.ANIMATED_ICON);

		/// <inheritdoc cref="GuildFeatures.BANNER"/>
		public bool CanUseBanner => Features.Contains(GuildFeatures.BANNER);

		/// <inheritdoc cref="GuildFeatures.INVITE_SPLASH"/>
		public bool CanUseInviteSplash => Features.Contains(GuildFeatures.INVITE_SPLASH);

		/// <inheritdoc cref="GuildFeatures.WELCOME_SCREEN_ENABLED"/>
		public bool CanUseWelcomeScreen => Features.Contains(GuildFeatures.WELCOME_SCREEN_ENABLED);

		/// <inheritdoc cref="GuildFeatures.VANITY_URL"/>
		public bool CanUseVanityURL => Features.Contains(GuildFeatures.VANITY_URL);



		/// <inheritdoc cref="GuildFeatures.COMMERCE"/>
		public bool IsCommerceServer => Features.Contains(GuildFeatures.COMMERCE);

		/// <inheritdoc cref="GuildFeatures.NEWS"/>
		public bool IsNewsServer => Features.Contains(GuildFeatures.NEWS);

		/// <inheritdoc cref="GuildFeatures.COMMUNITY"/>
		public bool IsCommunityServer => Features.Contains(GuildFeatures.COMMUNITY);


		/// <inheritdoc cref="GuildFeatures.DISCOVERABLE"/>
		public bool IsDiscoverable => Features.Contains(GuildFeatures.DISCOVERABLE);

		/// <inheritdoc cref="GuildFeatures.FEATURABLE"/>
		public bool IsFeaturable => Features.Contains(GuildFeatures.FEATURABLE);

		/// <inheritdoc cref="GuildFeatures.PARTNERED"/>
		public bool IsPartnered => Features.Contains(GuildFeatures.PARTNERED);

		/// <inheritdoc cref="GuildFeatures.VERIFIED"/>
		public bool IsVerified => Features.Contains(GuildFeatures.VERIFIED);



		/// <inheritdoc cref="GuildFeatures.VIP_REGIONS"/>
		public bool CanAccessVIPVoiceRegions => Features.Contains(GuildFeatures.VIP_REGIONS);

		internal List<string> Features;

		internal GuildFeatureInformation() {
			Features = new List<string>();
		}

		internal GuildFeatureInformation(IEnumerable<string> features) {
			Features = features.ToList();
		}

		internal void SetToFeatures(IEnumerable<string> features) {
			Features = features.ToList();
		}

	}
}
