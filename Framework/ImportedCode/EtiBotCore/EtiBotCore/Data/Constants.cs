using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.DiscordObjects;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.Exceptions;
using EtiBotCore.Exceptions.Marshalling;

namespace EtiBotCore.Data {

	/// <summary>
	/// A collection of constant values.
	/// </summary>
	public static class Constants {

		/// <summary>
		/// An exception message used for setting enum items in <see cref="DiscordObject"/>s.
		/// </summary>
		public const string INVALID_ENUM_NAME_ERROR = "Expected a valid enum. Do not cast arbitrary integer values into this enum.";

		/// <summary>
		/// For an <see cref="ObjectUnavailableException"/> thrown by a <see cref="Guild"/>.
		/// </summary>
		public const string GUILD_OUTAGE = "This guild is unavailable due to an outage. Altering properties is not possible at this time.";

		/// <summary>
		/// A placeholder for use in payloads when a string value is not sent, and in cases where the string is also optional (to be specific, in scenarios where <see langword="null"/> would have two meanings).<para/>
		/// In general, this should only be used if <see cref="string.Empty"/> is a legal value for the given text (rendering that ambiguous as well).
		/// </summary>
		public const string UNSENT_STRING_DEFAULT = "\0";

		/// <summary>
		/// A regex string matches a mention to a user or role. Examples include:
		/// <list type="bullet">
		/// <item>
		/// <term>User</term>
		/// <description>&lt;@0123456789&gt;</description>
		/// </item>
		/// <item>
		/// <term>User</term>
		/// <description>&lt;@!0123456789&gt; (n.b. this is identical to the first. It used to force display the nickname of the user but this is no longer the case.)</description>
		/// </item>
		/// <item>
		/// <term>Role</term>
		/// <description>&lt;@&amp;0123456789&gt;</description>
		/// </item>
		/// </list>
		/// The match will have three groups. The first is the opening entry (with the less than symbol and @ / other symbols), the second is the ID, and the third is the closing symbol.
		/// </summary>
		// language=regex
		public const string REGEX_ANY_MENTION = @"(<@(!?|&?))(\d+)(>)";

		/// <summary>
		/// A regex string that matches a mention to a user. Examples include:
		/// <list type="bullet">
		/// <item>
		/// <term>Form A</term>
		/// <description><c>&lt;@0123456789&gt;</c></description>
		/// </item>
		/// <item>
		/// <term>Form B</term>
		/// <description><c>&lt;@!0123456789&gt;</c> (n.b. this is identical to the first. It used to force display the nickname of the user but this is no longer the case.)</description>
		/// </item>
		/// </list>
		/// The match will have three groups. The first is the opening entry (with the less than symbol and @ / other symbols), the second is the ID, and the third is the closing symbol.
		/// </summary>
		// language=regex
		public const string REGEX_USER_MENTION = @"(<@!?)(\d+)(>)";

		/// <summary>
		/// A regex string matches a mention to a role: <c>&lt;@&amp;0123456789&gt;</c>
		/// </summary>
		// language=regex
		public const string REGEX_ROLE_MENTION = @"(<@&)(\d+)(>)";

		/// <summary>
		/// A regex string matches a mention to a user or channel: <c>&lt;#0123456789&gt;</c>
		/// </summary>
		// language=regex
		public const string REGEX_CHANNEL = @"(<#(!?|&?))(\d+)(>)";

	}
}
