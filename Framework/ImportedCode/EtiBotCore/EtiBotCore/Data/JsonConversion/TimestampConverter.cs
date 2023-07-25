using EtiBotCore.Data.Structs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Data.JsonConversion {

	/// <summary>
	/// Converts <see cref="ISO8601"/> objects to and from Json
	/// </summary>
	internal class TimestampConverter : JsonConverter<ISO8601?> {

		/// <inheritdoc/>
		public override ISO8601? ReadJson(JsonReader reader, Type objectType, ISO8601? existingValue, bool hasExistingValue, JsonSerializer serializer) {
			JToken token = JToken.ReadFrom(reader);
			if (token.Type == JTokenType.String) {
				return new ISO8601(token.ToObject<string>()!);
			} else if (token.Type == JTokenType.Date) {
				return new ISO8601(token.ToObject<DateTimeOffset>()!);
			} else if (token.Type == JTokenType.Null) {
				return null;
			}
			throw new FormatException("The given string was not an ISO8601 timestamp.");
		}

		/// <inheritdoc/>
		public override void WriteJson(JsonWriter writer, ISO8601? value, JsonSerializer serializer) {
			JToken fromValue = JToken.FromObject(value?.Timestamp ?? ISO8601.Epoch.Timestamp);
			fromValue.WriteTo(writer);
		}
	}
}
