using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.DiscordObjects.Guilds.ChannelData {

	/// <summary>
	/// The valid thread archival durations.
	/// </summary>
	public enum ThreadArchiveDuration : int {
		/// <summary>
		/// Archive in 60 minutes of no activity.
		/// </summary>
		Minutes60 = 60,

		/// <summary>
		/// Archive in 1440 minutes of no activity.
		/// </summary>
		Minutes1440 = 1440,

		/// <summary>
		/// Archive in 4320 minutes of no activity.
		/// </summary>
		Minutes4320 = 4320,

		/// <summary>
		/// Archive in 10080 minutes of no activity.
		/// </summary>
		Minutes10080 = 10080

	}
}
