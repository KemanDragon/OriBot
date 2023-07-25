using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility;
using EtiBotCore.Utility.Attributes;
using EtiBotCore.Utility.Extension;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

#nullable disable
namespace EtiBotCore.Data.JsonConversion {

	/// <summary>
	/// Designed to convert payload enums. Default behavior converts enums to int32, but for some enums (e.g. <see cref="StatusType"/>) it will get their string.
	/// </summary>
	internal class EnumConverter : JsonConverter<Enum> {

		/// <inheritdoc/>
		public override Enum ReadJson(JsonReader reader, Type objectType, Enum existingValue, bool hasExistingValue, JsonSerializer serializer) {
			JToken token = JToken.ReadFrom(reader);
			JTokenType tokenType = token.Type;
			Type enumType = existingValue?.GetType(); 
			if (enumType == null) {
				enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;
			}

			if (tokenType == JTokenType.String) {
				string value = token.ToObject<string>();
				
				if (enumType.HasAttribute<ConvertEnumByNameAttribute>()) {
					FieldInfo[] values = enumType.GetFields().Where(field => field.Name != "value__").ToArray();
					for (int index = 0; index < values.Length; index++) {
						FieldInfo field = values[index];
						string fieldName = field.HasAttribute<EnumConversionNameAttribute>() ? EnumConversionNameAttribute.GetNameFrom(field) : field.Name;
						if (value == fieldName) {
							// Specifically use field.Name here as we need the instance name, not the (potentially) custom name.
							return (Enum)Enum.Parse(enumType, field.Name);
						}
					}
					throw new InvalidOperationException("Could not find the enum value associated with " + value);
				} else {
					throw new ArgumentException("This enum was created by name, but its associated enum doesn't have the ConvertEnumByNameAttribute applied!");
				}
			} else if (tokenType == JTokenType.Integer) {
				int value = token.ToObject<int>();
				return (Enum)Enum.Parse(enumType, value.ToString()); // Works for flags too
			} else {
				return existingValue;
			}

			/*
			if (tokenType == JTokenType.String) {
				string value = token.ToObject<string>();
				return StatusTypeExtension.GetStatusType(value);
			} else if (tokenType == JTokenType.Integer) {
				int value = token.ToObject<int>();
				return (Enum)Enum.Parse(existingValue.GetType(), value.ToString()); // Works for flags too
			} else {
				return existingValue;
			}
			*/
		}

		/// <inheritdoc/>
		public override void WriteJson(JsonWriter writer, Enum value, JsonSerializer serializer) {
			JToken token;
			if (value is StatusType statusType) {
				token = JToken.FromObject(statusType.GetStatusName());
			} else {
				token = JToken.FromObject(Convert.ToInt32(value));
			}
			token.WriteTo(writer);
		}

	}
}
