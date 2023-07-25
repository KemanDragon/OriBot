using System;
using System.Collections.Generic;
using System.Text;

namespace OldOriBot.PermissionData {

	/// <summary>
	/// A value storing permission levels for a given user or entity. Modeled after ye olde bot system.<para/>
	/// Use pascal case for spaces, or use the <see cref="EnumConversionNameAttribute"/> to give the entries a specific name.
	/// </summary>
	public enum PermissionLevel : byte {
		
		/// <summary>
		/// This ... THING is not a valid member of the server.
		/// </summary>
		Nonmember = 0,

		/// <summary>
		/// This member is not allowed to use commands.
		/// </summary>
		Blacklisted = 1,

		/// <summary>
		/// This is a standard user.
		/// </summary>
		StandardUser = 2,

		/// <summary>
		/// This is a reputable and well-recognized user.
		/// </summary>
		ReputableUser = 3,

		/// <summary>
		/// This user is trusted with the management of simple bot mechanics.
		/// </summary>
		TrustedUser = 31,

		/// <summary>
		/// This user is trusted with general administrative things.
		/// </summary>
		Operator = 63,

		/// <summary>
		/// This user is trusted with control of the bot itself.
		/// </summary>
		Archon = 127,

		/// <summary>
		/// This user is trusted with control of the root systems of the bot.
		/// </summary>
		Overseer = 192,

		/// <summary>
		/// This user has access to the backend console that the bot is running on.
		/// </summary>
		BotDeveloper = 254,

		/// <summary>
		/// This member is the bot itself.
		/// </summary>
		Bot = 255

	}
}
