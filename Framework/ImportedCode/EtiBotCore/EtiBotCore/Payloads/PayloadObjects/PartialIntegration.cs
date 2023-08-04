using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// A partial integration used in audit logs.
	/// </summary>
	internal class PartialIntegration {

		/// <summary>
		/// The ID of the integration.
		/// </summary>
		[JsonProperty("id")]
		public ulong ID { get; set; }

		/// <summary>
		/// The name of this integration.
		/// </summary>
		[JsonProperty("name")]
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// The type of integration that this is.
		/// </summary>
		[JsonProperty("type")]
		public string Type { get; set; } = string.Empty;

		/// <summary>
		/// The account that handles this integration.
		/// </summary>
		[JsonProperty("account")]
		public AccountObject Account { get; set; } = new AccountObject();

		/// <summary>
		/// An account object.
		/// </summary>
		public class AccountObject {

			/// <summary>
			/// The name of this account.
			/// </summary>
			[JsonProperty("name")]
			public string Name { get; set; } = string.Empty;

			/// <summary>
			/// The ID of this account.
			/// </summary>
			[JsonProperty("id")]
			public ulong ID { get; set; }

		}

	}
}
