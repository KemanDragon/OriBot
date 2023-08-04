using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Utility.Attributes {
	/// <summary>
	/// When implemented on an enum, this changes the name that it is serialized to and from should its type use <see cref="ConvertEnumByNameAttribute"/>
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	public sealed class EnumConversionNameAttribute : System.Attribute {

		/// <summary>
		/// The name to use for this Enum entry.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Construct a new <see cref="EnumConversionNameAttribute"/>
		/// </summary>
		public EnumConversionNameAttribute(string name) {
			Name = name;
		}

		/// <summary>
		/// When given a <see cref="FieldInfo"/>, this will return the Name value stored in this attribute on that given field.
		/// </summary>
		/// <param name="field">The field to check.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">If the field does not have this attribute.</exception>
		public static string GetNameFrom(FieldInfo field) {
			Attribute? attr = field.GetCustomAttribute(typeof(EnumConversionNameAttribute));
			if (attr == null) throw new ArgumentException("The given field does not have the EnumConversionName attribute.");
			return ((EnumConversionNameAttribute)attr).Name;
		}
	}
}
