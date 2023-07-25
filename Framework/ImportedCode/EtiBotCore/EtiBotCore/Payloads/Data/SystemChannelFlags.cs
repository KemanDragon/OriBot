using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// The settings for a server's system channel, if applicable.
	/// </summary>
	[Flags]
	public enum SystemChannelFlags {

		/// <summary>
		/// Nothing is suppressed.
		/// </summary>
		SuppressNone = 0,

		/// <summary>
		/// Join notifications will not be sent in the system channel.
		/// </summary>
		SuppressJoinNotifications = 1 << 0,

		/// <summary>
		/// Nitro boost notifications will not be sent in the system channel.
		/// </summary>
		SuppressPremiumSubscriptions = 1 << 1,

	}
}
