using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Payloads.PayloadObjects;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// Information about a <see cref="Message"/>
	/// </summary>
	public enum MessageFlags {

		/// <summary>
		/// No associated information.
		/// </summary>
		None = 0,

		/// <summary>
		/// This message has been crossposted to another channel.
		/// </summary>
		HasBeenCrossposted = 1 << 0,

		/// <summary>
		/// This message is a crosspost from another channel.
		/// </summary>
		IsCrosspost = 1 << 1,

		/// <summary>
		/// This message suppresses embed objects.
		/// </summary>
		SuppressesEmbeds = 1 << 2,

		/// <summary>
		/// The original message of this crosspost has been deleted.
		/// </summary>
		SourceDeleted = 1 << 3,

		/// <summary>
		/// This is an urgent system message.
		/// </summary>
		Urgent = 1 << 4

	}
}
