using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// The verification level of a server.
	/// </summary>
	public enum VerificationLevel {

		/// <summary>
		/// The server is unrestricted.
		/// </summary>
		None = 0,

		/// <summary>
		/// Users that join must have a verified email.
		/// </summary>
		Low = 1,

		/// <summary>
		/// Users that join must be members of Discord for longer than 5 minutes.
		/// </summary>
		Medium = 2,

		/// <summary>
		/// Users that join must be a member of the server for 10 minutes before they can do anything.
		/// </summary>
		High = 3,

		/// <summary>
		/// The user must have a verified phone number.
		/// </summary>
		VeryHigh = 4

	}
}
