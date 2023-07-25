using EtiBotCore.Utility.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// Represents a presence status, e.g. online, away, etc.
	/// </summary>
	[ConvertEnumByName]
	public enum StatusType {

		/// <summary>
		/// The user is online right now.
		/// </summary>
		[EnumConversionName("online")]
		Online,

		/// <summary>
		/// The user is online, but has Do Not Disturb enabled.
		/// </summary>
		[EnumConversionName("dnd")]
		DoNotDisturb,

		/// <summary>
		/// The user is online, but away from their device.
		/// </summary>
		[EnumConversionName("idle")]
		Idle,

		/// <summary>
		/// The user is online, but appears to be offline.
		/// </summary>
		[EnumConversionName("invisible")]
		Invisible,

		/// <summary>
		/// The user is offline.
		/// </summary>
		[EnumConversionName("offline")]
		Offline

	}
}
