using EtiBotCore.Data.JsonConversion;
using EtiBotCore.Payloads.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// Represents a client's status across their devices that could be logged into Discord.
	/// </summary>
	internal class ClientPlatformStatus {

		/// <summary>
		/// The status of this user if they are on Desktop, or <see langword="null"/> if they are not logged in on Desktop anywhere.
		/// </summary>
		[JsonProperty("desktop", NullValueHandling = NullValueHandling.Ignore), JsonConverter(typeof(EnumConverter))]
		public StatusType? Desktop { get; set; } = null;

		/// <summary>
		/// The status of this user if they are on their phone or tablet, or <see langword="null"/> if they are not logged in on mobile anywhere.
		/// </summary>
		[JsonProperty("mobile", NullValueHandling = NullValueHandling.Ignore), JsonConverter(typeof(EnumConverter))]
		public StatusType? Mobile { get; set; } = null;

		/// <summary>
		/// The status of this user if they are on the website, or <see langword="null"/> if they are not logged in on web anywhere.
		/// </summary>
		[JsonProperty("web", NullValueHandling = NullValueHandling.Ignore), JsonConverter(typeof(EnumConverter))]
		public StatusType? Web { get; set; } = null;

	}
}
