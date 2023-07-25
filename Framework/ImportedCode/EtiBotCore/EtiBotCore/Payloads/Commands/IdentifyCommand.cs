using EtiBotCore.Payloads.Data;
using EtiBotCore.Payloads.PayloadObjects;
using EtiBotCore.Utility.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Commands {

	/// <summary>
	/// The initialization payload / identify payload.
	/// </summary>
	internal class IdentifyCommand : PayloadDataObject {

		/// <summary>
		/// The bot's token. Required.
		/// </summary>
		[JsonProperty("token"), JsonRequired]
		public string? Token { get; set; }

		/// <summary>
		/// The properties of this connection. Required.<para/>
		/// This is preset to an instance of <see cref="IdentifyConnectionProperties"/> which you should edit.
		/// </summary>
		[JsonProperty("properties"), JsonRequired]
		public IdentifyConnectionProperties Properties { get; } = new IdentifyConnectionProperties();

		/// <summary>
		/// Whether or not this connection supports the compression of packets.<para/>
		/// <strong>Default:</strong> false
		/// </summary>
		[JsonProperty("compress")]
		public bool Compress { get; set; } = false;

		/// <summary>
		/// A value between 50 and 250, this is the total number of members that will be sent before the gateway stops sending offline members.<para/>
		/// <strong>Default:</strong> 50
		/// </summary>
		[JsonProperty("large_threshold")]
		public int LargeThreshold {
			get => _LargeThreshold;
			set {
				if (value < 50 || value > 250) {
					throw new ArgumentOutOfRangeException("value", "Large Threshold must be in the range of 50 to 250!");
				}
				_LargeThreshold = value;
			}
		}
		[JsonIgnore] private int _LargeThreshold = 50;

		/// <summary>
		/// Used for guild sharding.
		/// </summary>
		[JsonProperty("shard")]
		public int[] Shard { get; } = new int[2] { 0, 1 };

		/// <summary>
		/// An optional presence to start the bot with.<para/>
		/// <strong>Default:</strong> <see langword="null"/>
		/// </summary>
		[JsonProperty("presence")]
		public UpdateStatusCommand? Presence { get; set; } = null;

		/// <summary>
		/// The intents of this bot. <strong>This must be set, as its default value will result in an error.</strong>
		/// </summary>
		[JsonProperty("intents")]
		public GatewayIntent Intents { get; set; } = GatewayIntent.NULL;

	}
}
