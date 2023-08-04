using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

namespace EtiBotCore.Utility.Extension {

	/// <summary>
	/// Provides extension methods for <see cref="Type"/>
	/// </summary>
	public static class TypeExtensions {


		/// <summary>
		/// Returns whether or not this type has an attribute of the given class present.
		/// </summary>
		/// <typeparam name="T">The type of the <see cref="Attribute"/>.</typeparam>
		/// <param name="type">The type to search on.</param>
		/// <returns>Whether or not this type has an attribute of the given class present.</returns>
		public static bool HasAttribute<T>(this Type type) where T : Attribute {
			object[] attrs = type.GetCustomAttributes(false);
			Type attrType = typeof(T);
			for (int index = 0; index < attrs.Length; index++) {
				if (attrs[index].GetType() == attrType) return true;
			}
			return false;
		}

		/// <summary>
		/// Specifically for <see cref="JsonProperty"/>, this will return the name of the associated property via looking for a <see cref="JsonProperty"/>
		/// </summary>
		/// <param name="prop"></param>
		/// <returns></returns>
		public static string? GetJsonName(this PropertyInfo prop) {
			object[] attrs = prop.GetCustomAttributes(false);
			Type attrType = typeof(JsonProperty);
			for (int index = 0; index < attrs.Length; index++) {
				if (attrs[index].GetType() == attrType) {
					JsonProperty jprop = (JsonProperty)attrs[index];
					return jprop.PropertyName;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns whether or not this member has an attribute of the given class present.
		/// </summary>
		/// <typeparam name="T">The type of the <see cref="Attribute"/>.</typeparam>
		/// <param name="member">The type to search on.</param>
		/// <param name="inherited">Whether or not this attribute can be inherited</param>
		/// <returns>Whether or not this type has an attribute of the given class present.</returns>
		public static bool HasAttribute<T>(this MemberInfo member, bool inherited = false) where T : Attribute {
			object[] attrs = member.GetCustomAttributes(inherited);
			Type attrType = typeof(T);
			for (int index = 0; index < attrs.Length; index++) {
				if (attrs[index].GetType() == attrType) return true;
			}
			return false;
		}

		/// <summary>
		/// Returns whether or not this type implements the given interface.
		/// </summary>
		/// <param name="type">The type to search on.</param>
		/// <param name="interfaceType">The type of the interface that might be implemented on this <see cref="Type"/></param>
		/// <returns>Whether or not this type implements the given interface.</returns>
		/// <exception cref="ArgumentException">If <paramref name="interfaceType"/> is not an interface type.</exception>
		/// <exception cref="ArgumentNullException">If <paramref name="interfaceType"/> is null</exception>
		public static bool Implements(this Type type, Type interfaceType) {
			if (interfaceType == null) throw new ArgumentNullException(nameof(interfaceType));
			if (!interfaceType.IsInterface) throw new ArgumentException("The given type was not an interface's type!", nameof(interfaceType));
			return type.GetInterfaces().Contains(interfaceType);
		}

		/// <summary>
		/// Returns whether or not this type is an instance of the given generic type.
		/// </summary>
		/// <param name="toCheck"></param>
		/// <param name="generic"></param>
		/// <returns></returns>
		public static bool IsSubclassOfRawGeneric(this Type toCheck, Type generic) {
			while (toCheck != null && toCheck != ObjectType) {
				var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
				if (generic == cur) {
					return true;
				}
				toCheck = toCheck.BaseType!;
			}
			return false;
		}
		private static readonly Type ObjectType = typeof(object);


		/// <summary>
		/// Returns whether or not this type is one of the stock numeric types. This works for nullables as well.
		/// </summary>
		/// <returns></returns>
		public static bool IsNumericType(this Type type) {
			return NumericTypes.Contains(Nullable.GetUnderlyingType(type) ?? type);
		}
		private static readonly HashSet<Type> NumericTypes = new HashSet<Type> {
			typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
			typeof(int), typeof(uint), typeof(long), typeof(ulong),
			typeof(decimal), typeof(float), typeof(double), typeof(BigInteger),
			typeof(Complex)
		};
	}
}
