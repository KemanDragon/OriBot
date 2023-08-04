using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using EtiBotCore.Payloads.PayloadObjects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#nullable disable
namespace EtiBotCore.Data.JsonConversion {

	/// <summary>
	/// Designed specifically for <see cref="Message.Nonce"/>, this will ensure that the input value (<see cref="int"/> or <see cref="string"/>) is always serialized as a string. It is a lazy method.
	/// </summary>
	class IntStringConverter : JsonConverter {

		/// <inheritdoc/>
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			new JObject(value.ToString()).WriteTo(writer);
		}

		/// <inheritdoc/>
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			JToken token = JToken.ReadFrom(reader);
			if (token.Type == JTokenType.Integer) {
				return token.ToObject<int>().ToString();
			} else if (token.Type == JTokenType.String) {
				return token.ToObject<string>();
			} else if (token.Type == JTokenType.Null) {
				return existingValue.ToString();
			}
			throw new InvalidCastException("Expected integer or string token, got " + token.Type);
		}

		public override bool CanConvert(Type objectType) {
			return (objectType == typeof(string) || objectType == typeof(int));
		}
	}
}
