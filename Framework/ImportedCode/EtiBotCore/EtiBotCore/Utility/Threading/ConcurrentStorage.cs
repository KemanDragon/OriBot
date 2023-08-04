using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Utility.Threading {

	/// <summary>
	/// A multi-threaded collection that allows looking for specific items. This does not guarantee any specific order of elements.<para/>
	/// This does not support duplicates.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Obsolete("Use SynchronizedCollection<T> instead.", true)]
	public class ConcurrentStorage<T> : ICollection<T> {

		/// <summary>
		/// A placeholder object instance representing a null key. It is possible to add null to this storage, and the dictionary does not support null keys.
		/// </summary>
		private static readonly object FAKE_NULL = new object();

		/// <summary>
		/// The backing <see cref="ConcurrentDictionary{TKey, TValue}"/> that this <see cref="ConcurrentStorage{T}"/> wraps around.
		/// </summary>
		private readonly ConcurrentDictionary<object, T> BackingDictionary = new ConcurrentDictionary<object, T>();

		/// <inheritdoc/>
		public IEnumerator<T> GetEnumerator() => BackingDictionary.Values.GetEnumerator();

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator() => BackingDictionary.Values.GetEnumerator();

		/// <inheritdoc/>
		public void Add(T item) => BackingDictionary.TryAdd(item ?? FAKE_NULL, item);

		/// <inheritdoc/>
		public void Clear() => BackingDictionary.Clear();
		
		/// <inheritdoc/>
		public void CopyTo(T[] array, int arrayIndex) => BackingDictionary.Values.CopyTo(array, arrayIndex);

		/// <inheritdoc/>
		public bool Remove(T item) => BackingDictionary.TryRemove(item ?? FAKE_NULL, out T _);

		/// <inheritdoc/>
		public bool Contains(T item) => BackingDictionary.ContainsKey(item ?? FAKE_NULL);
		
		/// <summary>
		/// Replaces the given <paramref name="item"/> with the given <paramref name="replacement"/>. Does nothing if the item is not a member of this <see cref="ConcurrentStorage{T}"/>.
		/// </summary>
		/// <param name="item"></param>
		/// <param name="replacement"></param>
		/// <returns>Whether or not the item was replaced.</returns>
		public bool Replace(T item, T replacement) {
			// Both are null, therefore identical - This does nothing. Return false, no replacement occurred.
			if (item is null && replacement is null) return false;
			
			object itemKey = item ?? FAKE_NULL;
			object replacementKey = replacement ?? FAKE_NULL;

			// The item and replacement are identical, so this does nothing. Return false, no replacement occurred.
			if (itemKey.Equals(replacementKey)) return false;

			if (BackingDictionary.ContainsKey(itemKey)) {
				BackingDictionary.TryRemove(itemKey, out T _);
				BackingDictionary.TryAdd(replacementKey, replacement);
				return true;
			}
			return false;
		}

		/// <inheritdoc/>
		public int Count => BackingDictionary.Count;

		/// <inheritdoc/>
		public bool IsReadOnly { get; } = false;

	}
}
