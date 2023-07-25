using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.Utility;

namespace EtiBotCore.DiscordObjects.Guilds.ChannelData {
	/// <summary>
	/// An application embedded in a message.
	/// </summary>
	
	public class MessageApplication {

		/// <summary>
		/// The ID of the application.
		/// </summary>
		public Snowflake ID { get; internal set; }

		/// <summary>
		/// ID of the embed's image asset, if one exists.
		/// </summary>
		internal string? CoverImageHash { get; set; }

		/// <summary>
		/// The description of this application.
		/// </summary>
		public string Description { get; internal set; } = string.Empty;

		/// <summary>
		/// The ID of this application's icon, if one exists.
		/// </summary>
		internal string? IconHash { get; set; }

		/// <summary>
		/// The name of this application.
		/// </summary>
		public string Name { get; internal set; } = string.Empty;

		/// <summary>
		/// A link to the cover image, if one exists.
		/// </summary>
		public Uri? CoverImage => HashToUriConverter.GetApplicationAsset(ID, CoverImageHash);

		/// <summary>
		/// The icon of this application, if one exists.
		/// </summary>
		public Uri? Icon => HashToUriConverter.GetApplicationIcon(ID, IconHash);

		internal static MessageApplication? CreateFromPayload(Payloads.PayloadObjects.MessageApplication? pl) {
			if (pl == null) return null;
			return new MessageApplication {
				ID = pl.ID,
				CoverImageHash = pl.CoverImage,
				Description = pl.Description,
				IconHash = pl.Icon,
				Name = pl.Name
			};
		}

		internal MessageApplication() { }
		internal MessageApplication(MessageApplication other) {
			ID = other.ID;
			CoverImageHash = other.CoverImageHash;
			Description = other.Description;
			IconHash = other.IconHash;
			Name = other.Name;
		}

	}
}
