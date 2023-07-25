using EtiBotCore.Data.Structs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

#nullable disable
namespace EtiBotCore.Utility.Marshalling {

	/// <summary>
	/// A container for a value whose type could be a number of possible options that are not necessarily interchangeable nor convertible.
	/// </summary>
	public abstract class Variant {
		/// <summary>
		/// The index of type argument that this variant contains.<br/>
		/// <em>This is a one-based value</em>, that is, the number that this is will reflect the property it's set to. <see cref="ArgIndex"/>=1 means Value<strong>1</strong> is in use, <see cref="ArgIndex"/>=2 means Value<strong>2</strong> is in use, so on.
		/// <br/><br/>
		/// <see cref="ArgIndex"/>=0 means this variant is malformed.
		/// </summary>
		public int ArgIndex { get; }

		/// <summary>
		/// Construct a new <see cref="Variant"/> with the given argument index.
		/// </summary>
		/// <param name="index"></param>
		protected Variant(int index) {
			ArgIndex = index;
		}

		/// <summary>
		/// Attempts to parse the given string in the given BotContext (from OriBotV3) into the given type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="instance"></param>
		/// <param name="inContext"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		protected static bool TryParseComponentAs<T>(string instance, object inContext, out T value) {
			if (typeof(ICommandArg).IsAssignableFrom(typeof(T))) {
				ConstructorInfo ctor = typeof(T).GetConstructor(new Type[0]);
				ICommandArg targ = (ICommandArg)ctor.Invoke(null);
				T t = (T)targ.From(instance, inContext);
				if (!t.Equals(default)) {
					value = t;
					return true;
				}
			} else {
				TypeConverter conv = TypeDescriptor.GetConverter(typeof(T));
				if (conv != null && conv.CanConvertFrom(typeof(string))) {
					try {
						value = (T)conv.ConvertFrom(instance);
						return true;
					} catch {
						if (typeof(T) == typeof(Snowflake)) {
							if (Snowflake.TryParse(instance, out Snowflake id)) {
								value = (T)(object)id; // kek
								return true;
							}
						} else if (typeof(T) == typeof(bool)) {
							instance = instance.ToLower();
							if (instance == "yes") {
								value = (T)(object)true;
								return true;
							} else if (instance == "no") {
								value = (T)(object)false;
								return true;
							}
						}
					}
				}
			}
			value = default;
			return false;
		}

		/// <summary>
		/// Attempts to parse the given string in the given BotContext (from OriBotV3) into the given type.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="instance"></param>
		/// <param name="inContext"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		protected static bool TryParseComponentAs(Type type, string instance, object inContext, out object value) {
			if (typeof(ICommandArg).IsAssignableFrom(type)) {
				ConstructorInfo ctor = type.GetConstructor(new Type[0]);
				ICommandArg targ = (ICommandArg)ctor.Invoke(null);
				object o = targ.From(instance, inContext);
				if (!(o is null)) {
					value = o;
					return true;
				}
			} else {
				TypeConverter conv = TypeDescriptor.GetConverter(type);
				if (conv != null && conv.CanConvertFrom(typeof(string))) {
					try {
						value = conv.ConvertFrom(instance);
						return true;
					} catch { }
				}
				if (type == typeof(Snowflake)) {
					if (Snowflake.TryParse(instance, out Snowflake id)) {
						value = id;
						return true;
					}
				} else if (type == typeof(bool)) {
					instance = instance.ToLower();
					if (instance == "yes") {
						value = true;
						return true;
					} else if (instance == "no") {
						value = false;
						return true;
					}
				}
			}
			value = default;
			return false;
		}
	}

	/// <summary>
	/// A value that could be either a(n) <typeparamref name="T1"/> or a(n) <typeparamref name="T2"/>.<para/>
	/// When automatically translating args, they are parsed in the order the types are defined. This means more precise types (such as a numeric type) should always be put before a string type when converting from a string.
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	/// <typeparam name="T2"></typeparam>
	public class Variant<T1, T2> : Variant, ICommandArg<Variant<T1, T2>> {

		/// <summary>
		/// The <typeparamref name="T1"/> component of this <see cref="Variant"/>.
		/// </summary>
		public T1 Value1 { get; }

		/// <summary>
		/// The <typeparamref name="T2"/> component of this <see cref="Variant"/>.
		/// </summary>
		public T2 Value2 { get; }

		/// <summary>
		/// Do not use.
		/// </summary>
		public Variant() : base(0) { }

		/// <summary>
		/// Intended for superclases.
		/// </summary>
		/// <param name="argIdx"></param>
		protected Variant(int argIdx) : base(argIdx) { }

		/// <summary>
		/// Create a new <see cref="Variant"/> containing a(n) <typeparamref name="T1"/> value.
		/// </summary>
		/// <param name="value"></param>
		public Variant(T1 value) : base(1) {
			Value1 = value;
		}

		/// <summary>
		/// Create a new <see cref="Variant"/> containing a(n) <typeparamref name="T2"/> value.
		/// </summary>
		/// <param name="value"></param>
		public Variant(T2 value) : base(2) {
			Value2 = value;
		}

		/// <summary>
		/// The type of <typeparamref name="T1"/>
		/// </summary>
		public Type Type1 => typeof(T1);

		/// <summary>
		/// The type of <typeparamref name="T2"/>
		/// </summary>
		public Type Type2 => typeof(T2);


		/// <inheritdoc/>
		public Variant<T1, T2> From(string instance, object inContext) {
			object t;
			if (TryParseComponentAs(typeof(T1), instance, inContext, out t)) {
				return new Variant<T1, T2>((T1)t);
			} else if (TryParseComponentAs(typeof(T2), instance, inContext, out t)) {
				return new Variant<T1, T2>((T2)t);
			}
			return default;
		}

		/// <inheritdoc/>
		object ICommandArg.From(string instance, object inContext) => From(instance, inContext);
	}

	/// <summary>
	/// A value that could be either a(n) <typeparamref name="T1"/>, a(n) <typeparamref name="T2"/>, or a(n) <typeparamref name="T3"/>.<para/>
	/// When automatically translating args, they are parsed in the order the types are defined. This means more precise types (such as a numeric type) should always be put before a string type when converting from a string.
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	/// <typeparam name="T2"></typeparam>
	/// <typeparam name="T3"></typeparam>
	public class Variant<T1, T2, T3> : Variant<T1, T2>, ICommandArg<Variant<T1, T2, T3>> {

		/// <summary>
		/// The <typeparamref name="T3"/> component of this <see cref="Variant"/>.
		/// </summary>
		public T3 Value3 { get; }

		/// <inheritdoc/>
		public Variant() : base(0) { }

		/// <summary>
		/// Intended for superclases.
		/// </summary>
		/// <param name="argIdx"></param>
		protected Variant(int argIdx) : base(argIdx) { }

		/// <inheritdoc/>
		public Variant(T1 value) : base(value) { }

		/// <inheritdoc/>
		public Variant(T2 value) : base(value) { }

		/// <summary>
		/// Create a new <see cref="Variant"/> containing a(n) <typeparamref name="T3"/> value.
		/// </summary>
		/// <param name="value"></param>
		public Variant(T3 value) : base(3) {
			Value3 = value;
		}

		/// <summary>
		/// The type of <typeparamref name="T1"/>
		/// </summary>
		public Type Type3 => typeof(T3);

		/// <inheritdoc/>
		public new Variant<T1, T2, T3> From(string instance, object inContext) {
			object t;
			if (TryParseComponentAs(typeof(T1), instance, inContext, out t)) {
				return new Variant<T1, T2, T3>((T1)t);
			} else if (TryParseComponentAs(typeof(T2), instance, inContext, out t)) {
				return new Variant<T1, T2, T3>((T2)t);
			} else if (TryParseComponentAs(typeof(T3), instance, inContext, out t)) {
				return new Variant<T1, T2, T3>((T3)t);
			}
			return default;
		}

		/// <inheritdoc/>
		object ICommandArg.From(string instance, object inContext) => From(instance, inContext);
	}

	/// <summary>
	/// A value that could be either a(n) <typeparamref name="T1"/>, a(n) <typeparamref name="T2"/>, a(n) <typeparamref name="T3"/>, or a(n) <typeparamref name="T4"/>.<para/>
	/// When automatically translating args, they are parsed in the order the types are defined. This means more precise types (such as a numeric type) should always be put before a string type when converting from a string.
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	/// <typeparam name="T2"></typeparam>
	/// <typeparam name="T3"></typeparam>
	/// <typeparam name="T4"></typeparam>
	public class Variant<T1, T2, T3, T4> : Variant<T1, T2, T3>, ICommandArg<Variant<T1, T2, T3, T4>> {

		/// <summary>
		/// The <typeparamref name="T4"/> component of this <see cref="Variant"/>.
		/// </summary>
		public T4 Value4 { get; }

		/// <summary>
		/// Intended for superclases.
		/// </summary>
		/// <param name="argIdx"></param>
		protected Variant(int argIdx) : base(argIdx) { }

		/// <inheritdoc/>
		public Variant() : base(0) { }

		/// <inheritdoc/>
		public Variant(T1 value) : base(value) { }

		/// <inheritdoc/>
		public Variant(T2 value) : base(value) { }

		/// <inheritdoc/>
		public Variant(T3 value) : base(value) { }

		/// <summary>
		/// Create a new <see cref="Variant"/> containing a(n) <typeparamref name="T3"/> value.
		/// </summary>
		/// <param name="value"></param>
		public Variant(T4 value) : base(4) {
			Value4 = value;
		}

		/// <summary>
		/// The type of <typeparamref name="T1"/>
		/// </summary>
		public Type Type4 => typeof(T4);

		/// <inheritdoc/>
		public new Variant<T1, T2, T3, T4> From(string instance, object inContext) {
			object t;
			if (TryParseComponentAs(typeof(T1), instance, inContext, out t)) {
				return new Variant<T1, T2, T3, T4>((T1)t);
			} else if (TryParseComponentAs(typeof(T2), instance, inContext, out t)) {
				return new Variant<T1, T2, T3, T4>((T2)t);
			} else if (TryParseComponentAs(typeof(T3), instance, inContext, out t)) {
				return new Variant<T1, T2, T3, T4>((T3)t);
			} else if (TryParseComponentAs(typeof(T4), instance, inContext, out t)) {
				return new Variant<T1, T2, T3, T4>((T4)t);
			}
			return default;
		}

		/// <inheritdoc/>
		object ICommandArg.From(string instance, object inContext) => From(instance, inContext);

	}
}
