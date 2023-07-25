using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Commands {

	/// <summary>
	/// Resumes a dropped gateway connection.
	/// </summary>
	internal class ResumeCommand : PayloadDataObject {

		/// <summary>
		/// The bot's token.
		/// </summary>
		[JsonProperty("token"), JsonRequired]
		public string? Token { get; set; }

		/// <summary>
		/// The ID of the current session to resume.
		/// </summary>
		[JsonProperty("session_id"), JsonRequired]
		public string? SessionID { get; set; }

		/// <summary>
		/// The last received sequence number.
		/// </summary>
		[JsonProperty("seq"), JsonRequired]
		public int Sequence { get; set; }

	}
}
