using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// Flags representing what can be done with an activity
	/// </summary>
	public enum ActivityFlags {

		/// <summary>
		/// This is a generic activity.
		/// </summary>
		None = 0,

		/// <summary>
		/// ?
		/// </summary>
		Instance = 1 << 0,

		/// <summary>
		/// This activity can be joined freely.
		/// </summary>
		Join = 1 << 1,

		/// <summary>
		/// You can spectate this activity.
		/// </summary>
		Spectate = 1 << 2,

		/// <summary>
		/// You can request to join this activity.
		/// </summary>
		JoinRequest = 1 << 3,

		/// <summary>
		/// ?
		/// </summary>
		Sync = 1 << 4,

		/// <summary>
		/// You can launch the game from this activity.
		/// </summary>
		Play = 1 << 5

	}
}
