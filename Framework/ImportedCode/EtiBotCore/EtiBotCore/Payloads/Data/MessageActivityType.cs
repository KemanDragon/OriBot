using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// A type of activity for a message.
	/// </summary>
	public enum MessageActivityType {

		/// <summary>
		/// I will join the content associated with this message.
		/// </summary>
		Join = 1,

		/// <summary>
		/// I will spectate the content associated with this message.
		/// </summary>
		Spectate = 2,

		/// <summary>
		/// I will listen to the content associated with this message.
		/// </summary>
		Listen = 3,

		/// <summary>
		/// I want to join the content associated with this message.
		/// </summary>
		JoinRequest = 4

	}
}
