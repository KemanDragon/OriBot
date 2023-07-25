using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.Utility.Marshalling;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.Utility.Arguments;

namespace OldOriBot.Utility.Extensions {

	/// <summary>
	/// Provides a means of lazily trying to get an object out of an array.
	/// </summary>
	public static class LazyArrayExtensions {

		/// <summary>
		/// Checks if it's possible to get something out of this <paramref name="array"/> at index <paramref name="index"/>, or returns <paramref name="def"/> if the array doesn't have that index.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array">The array to search.</param>
		/// <param name="index">The desired index.</param>
		/// <param name="def">The value to return if the index cannot be used.</param>
		/// <returns></returns>
		public static T GetOrDefault<T>(this T[] array, int index, T def = default) {
			if (array.Length > index) return array[index];
			return def;
		}

		/// <summary>
		/// Checks if it's possible to get something out of this <paramref name="array"/> at index <paramref name="index"/>, or returns <paramref name="def"/> if the array doesn't have that index (note that "doesn't have" does not necessarily equate to "is null").
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array">The array to search.</param>
		/// <param name="index">The desired index.</param>
		/// <param name="context">The BotContext that ran the command to get this arg, or <see langword="null"/> if this is being used outside of a command environment.</param>
		/// <param name="def">The value to return if the index cannot be used.</param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException">If the object in the array at the given index cannot be converted to <typeparamref name="T"/></exception>
		public static T GetOrDefault<T>(this object[] array, ArgumentMapProvider provider, int index, BotContext context, T def = default) {
			if (array.Length > index) {
				if (array[index] is null) return default;
				if (array[index] is T t) return t;
				TypeConverter conv = TypeDescriptor.GetConverter(typeof(T));
				if (typeof(ICommandArg).IsAssignableFrom(typeof(T))) {
					ConstructorInfo ctor = typeof(T).GetConstructor(new Type[0]);
					ICommandArg arg = (ICommandArg)ctor.Invoke(null);
					return (T)arg.From(array[index].ToString(), context);
				} else if (conv != null) {
					try {
						if (array[index] is string str) {
							return (T)conv.ConvertFromString(str);
						} else {
							return (T)conv.ConvertFrom(array[index]);
						}
					} catch (Exception) {
						// Special handling
						if (typeof(T) == typeof(Snowflake)) {
							if (Snowflake.TryParse(array[index] as string, out Snowflake id)) {
								return (T)(object)id; // kek
							}
						} else if (typeof(T) == typeof(bool)) {
							string str = array[index] as string;
							str = str.ToLower();
							if (str == "yes") {
								return (T)(object)true;
							} else if (str == "no") {
								return (T)(object)false;
							}
						}
					}
				}
				//throw new InvalidCastException($"Cannot convert input `{array[index]}` to type {typeof(T).FullName}");
				throw new CommandArgumentException(provider, index, array[index]);
			}
			return def;
		}

	}
}
