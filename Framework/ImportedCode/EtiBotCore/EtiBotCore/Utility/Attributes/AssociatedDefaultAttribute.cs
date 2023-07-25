using System;
using System.Collections.Generic;
using System.Text;

#nullable disable
namespace EtiBotCore.Utility.Attributes {


	/// <summary>
	/// Intended for use where this enum is going to be a key in a dictionary or other lookup, this provides the default value for the associated value.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	public sealed class AssociatedDefaultAttribute : Attribute {

		/// <summary>
		/// The default value that is associated with this enum.
		/// </summary>
		public object DefaultValue { get; }

		/// <summary>
		/// Returns the default value as the given type <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T As<T>() => (T)DefaultValue;

		/// <summary>
		/// Construct a new <see cref="AssociatedDefaultAttribute"/>, setting the associated default to the given value.
		/// </summary>
		/// <param name="defaultValue"></param>
		public AssociatedDefaultAttribute(object defaultValue) {
			DefaultValue = defaultValue;
		}

	}
}
