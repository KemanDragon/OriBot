using EtiBotCore.Data;
using EtiBotCore.Data.JsonConversion;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Payloads.PayloadObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Commands {

	/// <summary>
	/// Updates the client's presence.
	/// </summary>
	internal class UpdateStatusCommand : PayloadDataObject {

		/// <summary>
		/// Unix time (in millisconds) since the client went idle, or null if the client is not idle.<para/>
		/// <strong>Default:</strong> <see langword="null"/>
		/// </summary>
		[JsonProperty("since")]
		public int? Since { get; set; } = null;

		/// <summary>
		/// An array of activities. If setting or getting a single activity is desired, consider using <see cref="Activity"/><para/>
		/// <strong>Default:</strong> <see langword="null"/>
		/// </summary>
		[JsonProperty("activities")]
		public Activity[]? Activities { get; set; } = null;

		/// <summary>
		/// The new online status of the bot.<para/>
		/// <strong>Default:</strong> <see cref="StatusType.Online"/>
		/// </summary>
		[JsonProperty("status"), JsonConverter(typeof(EnumConverter))]
		public StatusType Status { get; set; } = StatusType.Online;

		/// <summary>
		/// Whether or not the client is AFK.
		/// <strong>Default:</strong> <see langword="null"/>
		/// </summary>
		[JsonProperty("afk")]
		public bool AFK { get; set; } = false;

		/// <summary>
		/// Sets <see cref="Activities"/> to a single-element array that consists of the provided <see cref="Activity"/> object. If the object is <see langword="null"/>, then the array will be set to <see langword="null"/> instead.
		/// </summary>
		public void SetActivity(Activity targetActivity) {
			if (targetActivity == null) {
				Activities = null;
			} else {
				Activities = new Activity[] { targetActivity };
			}
		}
	}
}
