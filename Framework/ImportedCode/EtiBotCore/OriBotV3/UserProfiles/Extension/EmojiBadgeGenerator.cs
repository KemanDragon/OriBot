using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EtiBotCore.DiscordObjects.Guilds;
using OldOriBot.CoreImplementation.Commands;

namespace OldOriBot.UserProfiles.Extension {
	public class EmojiBadgeGenerator {

		public const string BADGE_NAME = "Emoji Creator";
		public const string BADGE_DESC_FMT = "{0} of my works of art {1} approved for use as {2}!*\n\n{3}: ";
		public const string BADGE_MINI = "Emoji Machine Engineer";

		/// <summary>
		/// Creates the Emoji Machine badge and gives it to the given member, replacing their old instance of the badge if necessary.
		/// </summary>
		/// <param name="member"></param>
		public static void GenerateBadge(UserProfile profile) {
			/*
			 * >> profile GUID badge addcustom "Emoji Machine" "One of my works of art was approved for use as a server emoji!

Emoji: EMOJI HERE" "Emoji Machine No Longer :b:roke" ":naru:" 0*/

			if (CommandWhoMade.CreatorToEmojisMap.TryGetValue(profile.Member.ID.ToString(), out List<ulong> emojiIds) && emojiIds.Count > 0) {
				string badgeDesc;
				if (emojiIds.Count == 1) {
					badgeDesc = string.Format(BADGE_DESC_FMT, "One", "was", "a server emoji", "Emoji");
				} else {
					badgeDesc = string.Format(BADGE_DESC_FMT, "Some", "were", "server emojis", "Emojis");
				}

				foreach (ulong emojiId in emojiIds) {
					badgeDesc += $"<:emoji:{emojiId}> ";
				}

				Badge emojiBadge = profile.Badges.FirstOrDefault(badge => badge.Name == "Emoji Machine");
				Badge newEmojiBadge = new Badge(BADGE_NAME, badgeDesc, BADGE_MINI, "<:naru:671886905440206849>", (ushort)emojiIds.Count, 300);
				if (emojiBadge != null) {
					profile.RemoveBadge(emojiBadge);
				}

				profile.GrantBadge(newEmojiBadge);
			}
		}

	}
}
