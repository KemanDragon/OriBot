using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.DiscordObjects.Guilds;
using OldOriBot.Interaction;
using OldOriBot.PermissionData;

namespace OldOriBot.Data.MemberInformation {

	/// <summary>
	/// A wrapper for <see cref="Member"/>s.
	/// </summary>
	public static class PermissionContainer {

		/// <summary>
		/// Acquires the permission level of the associated member, or returns <see cref="PermissionLevel.StandardUser"/> if a permission is not registered.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static PermissionLevel GetPermissionLevel(this Member member) {
			if (member.IsSelf) return PermissionLevel.Bot;

			BotContext ctx = BotContextRegistry.GetContext(member.Server.ID);
			if (ctx != null) {
				return ctx.GetPermissionsOf(member);
			}
			return PermissionLevel.StandardUser;
		}

		/// <summary>
		/// Sets the permission level of the given member to the given value.
		/// </summary>
		/// <param name="member"></param>
		/// <exception cref="InvalidOperationException">If the member's permission level is considered immutable.</exception>
		public static void SetPermissionLevel(this Member member, PermissionLevel newLevel) {
			if (member.IsSelf && newLevel != PermissionLevel.Bot) throw new InvalidOperationException(Personality.Get("err.perms.changebot"));

			BotContext ctx = BotContextRegistry.GetContext(member.Server.ID);
			if (ctx != null) {
				ctx.SetPermissionsOf(member, newLevel);
			}
			//PermissionRegistry[member] = newLevel;
		}

	}
}
