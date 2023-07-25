using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Attributes;

#nullable disable
namespace EtiBotCore.Utility.Extension {

	/// <summary>
	/// Provides extra functionality to enums
	/// </summary>
	public static class EnumExtensions {


		/// <summary>
		/// If this enum has <see cref="AssociatedDefaultAttribute"/> on it, this will provide the internal value. 
		/// </summary>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="enumeration"></param>
		/// <param name="def"></param>
		/// <returns>Whether or not the attribute was present.</returns>
		public static bool GetDefaultAssociatedValueOf<TValue>(this Enum enumeration, out TValue def) {
			Type enumObj = enumeration.GetType();
			string name = Enum.GetName(enumObj, enumeration);
			if (name == null) {
				def = default;
				return false;
			}

			FieldInfo field = enumObj.GetField(name);
			if (field == null) {
				def = default;
				return false;
			}

			AssociatedDefaultAttribute assocDef = (AssociatedDefaultAttribute)field.GetCustomAttribute(typeof(AssociatedDefaultAttribute));
			if (assocDef == null) {
				def = default;
				return false;
			}

			def = assocDef.As<TValue>();
			return true;
		}

		/// <summary>
		/// Returns the name of each individual flag that is enabled in this enum. Entries are separated by bars <c>|</c>
		/// </summary>
		/// <param name="enumeration"></param>
		/// <returns></returns>
		public static string NameOfEach(this Enum enumeration) {
			Array values = Enum.GetValues(enumeration.GetType());
			string retn = string.Empty;
			foreach (object v in values) {
				if (enumeration.HasFlag((Enum)v)) {
					if (retn != string.Empty) {
						retn += " | ";
					}
					retn += Enum.GetName(enumeration.GetType(), v);
				}
			}
			return retn;
		}

		/// <summary>
		/// Given the input enum value and flags, this will return said enum with those flags switched off.
		/// </summary>
		/// <typeparam name="TEnum"></typeparam>
		/// <param name="enumeration"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static TEnum WithoutFlags<TEnum>(this Enum enumeration, TEnum flags) where TEnum : struct {
			if (typeof(TEnum) != enumeration.GetType()) {
				throw new ArgumentException("Flags is a different type than this enum!");
			}

			Type eType = enumeration.GetType();
			FieldInfo[] fields = eType.GetFields();
			ulong valueIn = Convert.ToUInt64(enumeration);
			ulong validFlagsMask = 0;
			foreach (FieldInfo field in fields) {
				if (field.Name.Equals("value__")) continue;
				validFlagsMask |= Convert.ToUInt64(field.GetRawConstantValue());
			}

			ulong antiFlags = ~Convert.ToUInt64(flags);
			valueIn &= antiFlags;
			valueIn &= validFlagsMask;
			return (TEnum)Enum.ToObject(eType, valueIn);
		}

		/// <summary>
		/// Returns the display name of the given status from its attribute.
		/// </summary>
		/// <param name="status"></param>
		/// <returns></returns>
		public static string GetStatusName(this StatusType status) {
			Type enumObj = status.GetType();
			string name = Enum.GetName(enumObj, status);
			if (name == null) {
				return null;
			}

			FieldInfo field = enumObj.GetField(name);
			if (field == null) {
				return null;
			}

			EnumConversionNameAttribute assocDef = field.GetCustomAttribute<EnumConversionNameAttribute>();
			if (assocDef == null) {
				return null;
			}

			return assocDef.Name;
		}

		/// <summary>
		/// Returns whether or not this channel type corresponds to that of a thread.
		/// </summary>
		/// <param name="type">The <see cref="ChannelType"/> to be tested.</param>
		/// <returns>Whether or not this channel type is a thread type.</returns>
		public static bool IsThreadChannel(this ChannelType type) {
			return type == ChannelType.NewsThread || type == ChannelType.PublicThread || type == ChannelType.PrivateThread;
		}

	}
}
