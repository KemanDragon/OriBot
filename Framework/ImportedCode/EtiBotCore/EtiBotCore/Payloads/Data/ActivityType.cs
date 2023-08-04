using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// Represents an activity on Discord, which is used for user status.
	/// </summary>
	public enum ActivityType {

		/// <summary>
		/// The activity type for Playing.<para/>
		/// <strong>Format:</strong> Playing {name}
		/// </summary>
		Playing = 0,

		/// <summary>
		/// The activity type for Streaming. <strong>This is not usable by bots and cannot be implemented.</strong><para/>
		/// <strong>Format:</strong> Streaming {details}
		/// </summary>
		Streaming = 1,

		/// <summary>
		/// The activity type for Listening.<para/>
		/// <strong>Format:</strong> Listening to {name}
		/// </summary>
		Listening = 2,

		/// <summary>
		/// The activity type for watching.<para/>
		/// <strong>Format:</strong> Watching {name}
		/// </summary>
		Watching = 3,

		/// <summary>
		/// A custom activity. <strong>This is not usable by bots and cannot be implemented.</strong><para/>
		/// <strong>Format:</strong> {emoji} {state}
		/// </summary>
		Custom = 4,

		/// <summary>
		/// Activity type for competing.<para/>
		/// <strong>Format:</strong> Competing in {name}
		/// </summary>
		Competing = 5,

	}
}
