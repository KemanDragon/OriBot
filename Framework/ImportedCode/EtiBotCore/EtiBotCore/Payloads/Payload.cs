using EtiBotCore.Data.JsonConversion;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Payloads.Events;
using EtiLogger.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads {

	/// <summary>
	/// The base class for a payload. All payloads follow this format.
	/// </summary>
	internal class Payload : ILoggable {

		/// <summary>
		/// The opcode for the payload.
		/// </summary>
		[JsonProperty("op"), JsonConverter(typeof(EnumConverter))]
		public PayloadOpcode Operation { get; set; } = PayloadOpcode.Dispatch;

		/// <summary>
		/// The data included with the payload. When reading from this property, it will likely be a <see cref="JObject"/> 
		/// unless specifically set to something else.
		/// </summary>
		[JsonProperty("d")]
		public object? Data { get; set; } = null;

		/// <summary>
		/// The sequence number, used for resuming sessions and heartbeats.<para/>
		/// This is <see langword="null"/> (not sent) unless <see cref="Operation"/> is <see cref="PayloadOpcode.Dispatch"/>
		/// </summary>
		[JsonProperty("s")]
		public int? Sequence { get; set; } = null;

		/// <summary>
		/// The name of the transmitted event.<para/>
		/// This is <see langword="null"/> (not sent) unless <see cref="Operation"/> is <see cref="PayloadOpcode.Dispatch"/>
		/// </summary>
		[JsonProperty("t")]
		public string? EventName { get; set; } = null;

		/// <summary>
		/// Calls JObject.ToObject on <see cref="Data"/> and returns its value.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T GetObjectFromData<T>() {
			return ((JObject)Data!).ToObject<T>()!;
		}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public bool ShouldSerializeDataReal() {
			return Data != null;
		}

		public bool ShouldSerializeSequence() {
			return Operation == PayloadOpcode.Dispatch;
		}

		public bool ShouldSerializeEventName() {
			return Operation == PayloadOpcode.Dispatch;
		}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// An alias to easily convert this Payload to json.
		/// </summary>
		/// <returns></returns>
		public string ToJson() {
			return JsonConvert.SerializeObject(this);
		}

		/// <summary>
		/// Calls <see cref="ToJson"/> but populates the <see cref="ArraySegment{T}"/> with the string in the form of UTF8 text.
		/// </summary>
		/// <param name="data"></param>
		public void ToJsonBytes(out ArraySegment<byte> data) {
			string json = ToJson();
			data = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
		}

		/// <summary>
		/// Calls <see cref="ToJson"/> and returns the string as a byte array encoded as UTF8.
		/// </summary>
		public byte[] ToJsonBytes() {
			string json = ToJson();
			return Encoding.UTF8.GetBytes(json);
		}

		/// <summary>
		/// Returns a lightweight representation of this payload, excluding its data.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return $":::: PAYLOAD OBJECT ::::\nOpcode: {Operation}\nEventName: {EventName}\nSequence Number: {Sequence}\n::::::::::::::::::::::::";
		}

		public LogMessage ToMessage() {
			LogMessage retnMsg = new LogMessage($"^#AAAAAA;:::: PAYLOAD OBJECT ::::\n§7Opcode: §b{Operation}\n§7EventName: §e{EventName}\n§7Sequence Number: §e{Sequence}\n::::::::::::::::::::::::");
			return retnMsg;
		}
	}
}
