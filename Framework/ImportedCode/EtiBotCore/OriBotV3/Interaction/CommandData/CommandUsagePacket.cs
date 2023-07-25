using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.DiscordObjects.Guilds;
using OldOriBot.Data;
using OldOriBot.Data.MemberInformation;
using OldOriBot.Utility.Extensions;

namespace OldOriBot.Interaction.CommandData {

	/// <summary>
	/// Stores information on if this command can be used.
	/// </summary>
	public class CommandUsagePacket {

		/// <summary>
		/// A default packet representing that a command can be used.
		/// </summary>
		public static readonly CommandUsagePacket Success = new CommandUsagePacket(true);

		/// <summary>
		/// Construct a packet representing insufficient permissions.
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		public static CommandUsagePacket ForInsufficientPermissions(Command cmd, Member member) {
			return new CommandUsagePacket(false, Personality.Get("cmd.err.reason.insufficientPerms", cmd.RequiredPermissionLevel.GetFullName(), member.GetPermissionLevel().GetFullName()));
		}

		/// <summary>
		/// Whether or not the current member that this object was returned for can use this command.
		/// </summary>
		public bool CanUse { get; }

		/// <summary>
		/// Why this command cannot be used.
		/// </summary>
		public string Reason { get; }

		/// <summary>
		/// Construct a new <see cref="CommandUsagePacket"/>. If <paramref name="canUse"/> is <see langword="true"/>, then <paramref name="reason"/> does not need to be defined.
		/// </summary>
		/// <param name="canUse">Whether or not the command is usable.</param>
		/// <param name="reason">If it's not usable, this is why.</param>
		public CommandUsagePacket(bool canUse, string reason = null) {
			CanUse = canUse;
			Reason = reason ?? Personality.Get("cmd.err.reason.null");
		}

	}
}
