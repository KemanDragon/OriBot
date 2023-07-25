using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EtiBotCore.DiscordObjects;
using EtiBotCore.Utility.Threading;

#nullable disable
namespace EtiBotCore.Utility.Extension {

	/// <summary>
	/// Extends the functionality of List<![CDATA[>]]>
	/// </summary>
	public static class EnumerableExtensions {


		/// <summary>
		/// Adds <paramref name="count"/> elements from <paramref name="array"/> to this <see cref="List{T}"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <param name="array"></param>
		/// <param name="count"></param>
		public static void AddRangeFrom<T>(this List<T> source, T[] array, int count) {
			source.AddRange(array.Take(count));
		}

		/// <summary>
		/// Resets all elements in this array to the <see langword="default"/> value for their type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		public static void Reset<T>(this T[] source) {
			for (int idx = 0; idx < source.Length; idx++) {
				source[idx] = default;
			}
		}
		
		/// <summary>
		/// Returns the integer index of the given element in this array, or -1 if it could not be found.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static int IndexOf<T>(this T[] source, T value) {
			if (source is null) throw new ArgumentNullException(nameof(source));
			for (int index = 0; index < source.Length; index++) {
				if (source[index]?.Equals(value) ?? false) {
					return index;
				}
			}
			return -1;
		}

		/// <summary>
		/// Given an <see cref="IEnumerable{T}"/> of a given type, this will convert it to the new type <typeparamref name="TOut"/> granted the type <typeparamref name="TIn"/> can be cast into it.
		/// </summary>
		/// <typeparam name="TIn"></typeparam>
		/// <typeparam name="TOut"></typeparam>
		/// <param name="source"></param>
		/// <returns></returns>
		public static IEnumerable<TOut> ToType<TIn, TOut>(this IEnumerable<TIn> source) {
			using IEnumerator<TIn> enumerator = source.GetEnumerator();
			while (enumerator.MoveNext())
				yield return (TOut)(object)enumerator.Current;
		}

		/// <summary>
		/// Slices this array into a number of arrays of a given size.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="array"></param>
		/// <param name="sliceMaxSize"></param>
		/// <returns></returns>
		public static T[][] SliceInto<T>(this T[] array, int sliceMaxSize) {
			int numSegs = (int)Math.Ceiling(array.Length / (double)sliceMaxSize);
			List<T[]> ts = new List<T[]>(numSegs);
			for (int i = 0; i < numSegs; i++) {
				int start = i * sliceMaxSize;
				ts.Add(array.Skip(start).Take(sliceMaxSize).ToArray());
			}
			return ts.ToArray();
		}

		/// <summary>
		/// Selects a random element out of <paramref name="enumerable"/>. Returns <see langword="default"/> if the enumerable is empty.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="enumerable"></param>
		/// <returns></returns>
		public static T Random<T>(this IEnumerable<T> enumerable) {
			T[] arr = enumerable.ToArray();
			if (arr.Length == 0) return default;
			return arr[RNG.Next(arr.Length)];
		}
		private static readonly Random RNG = new Random();

		/// <summary>
		/// Uses a lazy means of copying this list. The contents are identical, but it creates a separate list reference.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <returns></returns>
		public static IReadOnlyList<T> LazyCopy<T>(this IReadOnlyList<T> source) {
			return source.ToArray().ToList().AsReadOnly();
		}

		/// <summary>
		/// Uses a lazy means of copying this list. The contents are identical by reference, but it creates a separate reference for the list itself (so <c>RefrenceEquals(source, returnValueFromThis)</c> will return false, but <c>ReferenceEquals(source[n], returnValueFromThis[n])</c> will return true.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <returns></returns>
		public static List<T> LazyCopy<T>(this List<T> source) {
			return source.ToArray().ToList();
		}

		/// <summary>
		/// Uses a lazy means of copying this list. The contents are identical by reference, but it creates a separate reference for the list itself (so <c>RefrenceEquals(source, returnValueFromThis)</c> will return false, but <c>ReferenceEquals(source[n], returnValueFromThis[n])</c> will return true.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <returns></returns>
		public static T[] LazyCopy<T>(this T[] source) {
			T[] newArray = new T[source.Length];
			for (int i = 0; i < newArray.Length; i++) {
				newArray[i] = source[i];
			}
			return newArray;
		}

		/// <summary>
		/// Uses a lazy means of copying this list. The contents are identical by reference, but it creates a separate reference for the list itself (so <c>RefrenceEquals(source, returnValueFromThis)</c> will return false, but <c>ReferenceEquals(source[n], returnValueFromThis[n])</c> will return true.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public static IList LazyCopy(this IList source) {
			return source.Cast<object>().ToList();
		}

		/// <summary>
		/// Uses a lazy means of copying this dictionary. The contents are identical references, but it creates a separate reference for the dictionary itself.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public static IDictionary LazyCopy(this IDictionary source) {
			Dictionary<object, object> objs = new Dictionary<object, object>();
			foreach (KeyValuePair<object, object> data in source) {
				objs[data.Key] = data.Value;
			}
			return objs;
		}

		/// <summary>
		/// Uses a lazy means of copying this dictionary. The contents are identical references, but it creates a separate reference for the dictionary itself.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public static IDictionary<TKey, TValue> LazyCopy<TKey, TValue>(this IDictionary<TKey, TValue> source) {
			Dictionary<TKey, TValue> objs = new Dictionary<TKey, TValue>();
			foreach (KeyValuePair<TKey, TValue> data in source) {
				objs[data.Key] = data.Value;
			}
			return objs;
		}

		/// <summary>
		/// Uses a lazy means of copying this dictionary. The contents are identical references, but it creates a separate reference for the dictionary itself.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public static ThreadedDictionary<TKey, TValue> LazyCopy<TKey, TValue>(this ThreadedDictionary<TKey, TValue> source) {
			ThreadedDictionary<TKey, TValue> objs = new ThreadedDictionary<TKey, TValue>();
			foreach (KeyValuePair<TKey, TValue> data in source) {
				objs[data.Key] = data.Value;
			}
			return objs;
		}

		/// <summary>
		/// Uses a lazy means of copying this dictionary. The contents are identical references, but it creates a separate reference for the dictionary itself.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		[Obsolete] public static ConcurrentDictionary<TKey, TValue> LazyCopy<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> source) {
			ConcurrentDictionary<TKey, TValue> objs = new ConcurrentDictionary<TKey, TValue>();
			foreach (KeyValuePair<TKey, TValue> data in source) {
				objs[data.Key] = data.Value;
			}
			return objs;
		}
	}
}
