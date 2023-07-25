using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// The multi-factor authentication level of a server.
	/// </summary>
	public enum MFALevel {

		/// <summary>
		/// This server does not require users to have 2FA enabled.
		/// </summary>
		Normal = 0,

		/// <summary>
		/// This server's users must have 2FA enabled.
		/// </summary>
		Elevated = 1

	}
}
