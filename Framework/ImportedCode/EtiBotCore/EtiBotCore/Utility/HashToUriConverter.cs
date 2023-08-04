using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.Structs;

namespace EtiBotCore.Utility {

	/// <summary>
	/// Takes in hashes (among other potentially necessary information) and returns a URL to the associated resource on Discord.
	/// </summary>
	public static class HashToUriConverter {

		/// <summary>
		/// The base URL that all assets on Discord are at.
		/// </summary>
		public const string BASE_URL = "https://cdn.discordapp.com/";

		/// <summary>
		/// A format string for if a URL has an endpoint, ID, a hash, and an extension.
		/// </summary>
		private const string FMT_ID_HASH = "{0}/{1}/{2}.{3}";

		/// <summary>
		/// A format string for if a URL has an endpoint, ID, a hash, and an extension.
		/// </summary>
		private const string FMT_ID_HASH_PNG = "{0}/{1}/{2}.png";



		/// <summary>
		/// Given the emoji has, this will return a URL to the Emoji image. If the hash is <see langword="null"/> then this will return <see langword="null"/>.
		/// </summary>
		/// <param name="emojiHash"></param>
		/// <returns></returns>
		public static Uri? GetCustomEmojiImage(string? emojiHash) {
			if (emojiHash == null) return null;
			return new Uri(BASE_URL + $"emojis/{emojiHash}.png");
		}

		/// <summary>
		/// Given the guild ID and its icon hash, this will return a URL to the icon's image. If the hash is <see langword="null"/> then this will return <see langword="null"/>.
		/// </summary>
		/// <param name="guildID"></param>
		/// <param name="iconHash"></param>
		/// <returns></returns>
		public static Uri? GetGuildIcon(Snowflake guildID, string? iconHash) {
			if (iconHash == null) return null;
			string extension = iconHash.StartsWith("a_") ? "gif" : "png";
			return new Uri(BASE_URL + string.Format(FMT_ID_HASH, "icons", guildID, iconHash, extension));
		}

		/// <summary>
		/// Given the guild ID and its splash's hash, this will return a URL to the splash image. If the hash is <see langword="null"/> then this will return <see langword="null"/>.
		/// </summary>
		/// <param name="guildID"></param>
		/// <param name="splashHash"></param>
		/// <returns></returns>
		public static Uri? GetGuildSplash(Snowflake guildID, string? splashHash) {
			if (splashHash == null) return null;
			return new Uri(BASE_URL + string.Format(FMT_ID_HASH_PNG, "splashes", guildID, splashHash));
		}

		/// <summary>
		/// Given the guild ID and its discovery image hash, this will return a URL to the discovery image. If the hash is <see langword="null"/> then this will return <see langword="null"/>.
		/// </summary>
		/// <param name="guildID"></param>
		/// <param name="discoverySplashHash"></param>
		/// <returns></returns>
		public static Uri? GetGuildDiscoverySplash(Snowflake guildID, string? discoverySplashHash) {
			if (discoverySplashHash == null) return null;
			return new Uri(BASE_URL + string.Format(FMT_ID_HASH_PNG, "discovery-splashes", guildID, discoverySplashHash));
		}

		/// <summary>
		/// Given the guild ID and its banner hash, this will return a URL to the banner image. If the hash is <see langword="null"/> then this will return <see langword="null"/>.
		/// </summary>
		/// <param name="guildID"></param>
		/// <param name="bannerHash"></param>
		/// <returns></returns>
		public static Uri? GetGuildBanner(Snowflake guildID, string? bannerHash) {
			if (bannerHash == null) return null;
			return new Uri(BASE_URL + string.Format(FMT_ID_HASH_PNG, "banners", guildID, bannerHash));
		}

		/// <summary>
		/// Given a user's discriminator, this will return their default avatar.
		/// </summary>
		/// <param name="discriminator"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">If discriminator is less than 1 or greater than 9999</exception>
		public static Uri GetUserDefaultAvatar(int discriminator) {
			if (discriminator < 1 || discriminator > 9999) throw new ArgumentOutOfRangeException(nameof(discriminator));
			return new Uri(BASE_URL + string.Format(FMT_ID_HASH_PNG, "embed", "avatars", discriminator % 5));
		}

		/// <summary>
		/// Given a user's ID and their avatar's hash, this will return the URL to their avatar. If the hash is <see langword="null"/> then this will return <see langword="null"/>.
		/// </summary>
		/// <param name="userID"></param>
		/// <param name="avatarHash"></param>
		/// <returns></returns>
		public static Uri? GetUserAvatar(Snowflake userID, string? avatarHash) {
			if (avatarHash == null) return null;
			string extension = avatarHash.StartsWith("a_") ? "gif" : "png";
			return new Uri(BASE_URL + string.Format(FMT_ID_HASH, "avatars", userID, avatarHash, extension));
		}

		/// <summary>
		/// Given an app's ID and its icon's hash, this will return the URL to the icon. If the hash is <see langword="null"/> then this will return <see langword="null"/>.
		/// </summary>
		/// <param name="appID"></param>
		/// <param name="iconHash"></param>
		/// <returns></returns>
		public static Uri? GetApplicationIcon(Snowflake appID, string? iconHash) {
			if (iconHash == null) return null;
			return new Uri(BASE_URL + string.Format(FMT_ID_HASH_PNG, "app-icons", appID, iconHash));
		}

		/// <summary>
		/// Given an app's ID and an asset's hash, this will return the URL to the asset. If the hash is <see langword="null"/> then this will return <see langword="null"/>.
		/// </summary>
		/// <param name="appID"></param>
		/// <param name="assetHash"></param>
		/// <returns></returns>
		public static Uri? GetApplicationAsset(Snowflake appID, string? assetHash) {
			if (assetHash == null) return null;
			return new Uri(BASE_URL + string.Format(FMT_ID_HASH_PNG, "app-assets", appID, assetHash));
		}

		/// <summary>
		/// Given an app's ID, an achievement's ID, and the hash to the achievement, this will return the URL to the achievement. If the hash is <see langword="null"/> then this will return <see langword="null"/>.
		/// </summary>
		/// <param name="appID"></param>
		/// <param name="achievementID"></param>
		/// <param name="iconHash"></param>
		/// <returns></returns>
		public static Uri? GetAchievementIcon(Snowflake appID, Snowflake achievementID, string? iconHash) {
			if (iconHash == null) return null;
			return new Uri(BASE_URL + string.Format(FMT_ID_HASH_PNG, "app-assets", appID + "/achievements/" + achievementID + "/icons", iconHash));
		}

		/// <summary>
		/// Given a team's ID and their icon's hash, this will return the URL to the team's icon. If the hash is <see langword="null"/> then this will return <see langword="null"/>.
		/// </summary>
		/// <param name="teamID"></param>
		/// <param name="teamHash"></param>
		/// <returns></returns>
		public static Uri? GetTeamIcon(Snowflake teamID, string? teamHash) {
			if (teamHash == null) return null;
			return new Uri(BASE_URL + string.Format(FMT_ID_HASH_PNG, "team-icons", teamID, teamHash));
		}
	}
}
