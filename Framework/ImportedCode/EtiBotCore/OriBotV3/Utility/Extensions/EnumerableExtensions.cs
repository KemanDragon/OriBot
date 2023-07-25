using System;
using System.Collections.Generic;
using System.Text;

namespace OldOriBot.Utility.Extensions {
	public static class EnumerableExtensions {

		/// <summary>
		/// Returns whether or not this <see cref="IEnumerable{T}"/> contains something that satisfies the given element.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="enumerable"></param>
		/// <param name="element"></param>
		/// <returns></returns>
		public static bool Contains<T>(this IEnumerable<T> enumerable, Predicate<T> predicate) {
			foreach (T element in enumerable) {
				if (predicate.Invoke(element)) return true;
			}
			return false;
		}

	}
}
