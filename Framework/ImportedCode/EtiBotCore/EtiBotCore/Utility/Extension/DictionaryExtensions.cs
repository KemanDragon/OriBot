using EtiBotCore.Data.Structs;
using EtiBotCore.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

#nullable disable
namespace EtiBotCore.Utility.Extension {

	/// <summary>
	/// Provides extensions to dictionaries.
	/// </summary>
	public static class DictionaryExtensions {

		/// <summary>
		/// Attempts to get the entry for the specified key within this dictionary. Returns <paramref name="defaultValue"/> if the key has not been populated.
		/// </summary>
		/// <typeparam name="TKey">The key value type for this <see cref="Dictionary{TKey, TValue}"/></typeparam>
		/// <typeparam name="TValue">The value type corresponding to the keys in this <see cref="Dictionary{TKey, TValue}"/></typeparam>
		/// <param name="dictionary">The target dictionary.</param>
		/// <param name="key">The key to search for.</param>
		/// <param name="defaultValue">The default value to return.</param>
		/// <param name="populateIfNull">If <see langword="true"/>, the default value will be put into the dictionary with the given key if it was not found.</param>
		public static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue, bool populateIfNull = false) {
			if (!dictionary.TryGetValue(key, out TValue retn)) {
				retn = defaultValue;
				if (populateIfNull) {
					dictionary[key] = defaultValue;
				}
			}

			return retn;
		}

		/// <summary>
		/// Attempts to do a reverse-lookup on the specified <paramref name="value"/>, returning the first key that corresponds to this value.
		/// </summary>
		/// <typeparam name="TKey">The key value type for this <see cref="Dictionary{TKey, TValue}"/></typeparam>
		/// <typeparam name="TValue">The value type corresponding to the keys in this <see cref="Dictionary{TKey, TValue}"/></typeparam>
		/// <param name="dictionary">The target dictionary to search from.</param>
		/// <param name="value">The value to find the corresponding key of.</param>
		/// <exception cref="ValueNotFoundException"/>
		/// <returns></returns>
		public static TKey KeyOf<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TValue value) {
			if (!dictionary.Values.Contains(value)) throw new ValueNotFoundException("The specified value does not exist in this dictionary.");
			foreach (TKey key in dictionary.Keys) {
				TValue v = dictionary[key];
				if (v.Equals(value)) {
					return key;
				}
			}
			throw new ValueNotFoundException("The specified value does not exist in this dictionary.");
		}

		/// <inheritdoc cref="KeyOf{TKey, TValue}(Dictionary{TKey, TValue}, TValue)"/>
		public static TKey KeyOf<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TValue value) => KeyOf(dictionary, value);

		/// <inheritdoc cref="GetOrDefault{TKey, TValue}(Dictionary{TKey, TValue}, TKey, TValue, bool)"/>
		public static TValue GetOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue, bool populateIfNull = false) => GetOrDefault(dictionary, key, defaultValue, populateIfNull);


	}
}
