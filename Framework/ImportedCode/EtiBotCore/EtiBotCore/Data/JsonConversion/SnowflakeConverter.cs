using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using EtiBotCore.Data.Structs;
using Newtonsoft.Json;

namespace EtiBotCore.Data.JsonConversion {
	internal class SnowflakeConverter : JsonConverter<Snowflake> {
		public override void WriteJson(JsonWriter writer, [AllowNull] Snowflake value, JsonSerializer serializer) {
			if (value != null) writer.WriteValue(value.Value);
		}

		public override Snowflake ReadJson(JsonReader reader, Type objectType, [AllowNull] Snowflake existingValue, bool hasExistingValue, JsonSerializer serializer) {
			if (ulong.TryParse(reader.ReadAsString(), out ulong id)) {
				return id;
			}
			if (hasExistingValue) return existingValue;
			return Snowflake.Invalid;
		}
	}
}
