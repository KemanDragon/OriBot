#pragma warning disable CS8601
#pragma warning disable CS8603
#pragma warning disable CS8604

using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace EtiBotCore.Utility.Threading {

	/// <summary>
	/// A thread-safe <see cref="IDictionary{TKey, TValue}"/>.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	public class ThreadedDictionary<TKey, TValue> : IDictionary, IDictionary<TKey, TValue> where TKey : notnull {

		private readonly Dictionary<TKey, TValue> InternalDictionary = new Dictionary<TKey, TValue>();

		/// <inheritdoc/>
		public void Add(TKey key, TValue value) {
			lock (SyncRoot) {
				InternalDictionary.Add(key, value);
			}
		}

		/// <inheritdoc/>
		public bool ContainsKey(TKey key) {
			lock (SyncRoot) {
				return InternalDictionary.ContainsKey(key);
			}
		}

		/// <inheritdoc/>
		public bool Remove(TKey key) {
			lock (SyncRoot) {
				return InternalDictionary.Remove(key);
			}
		}

		/// <inheritdoc/>
		public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
			lock (SyncRoot) {
				return InternalDictionary.TryGetValue(key, out value);
			}
		}

		/// <inheritdoc/>
		public TValue this[TKey key] {
			get {
				lock (SyncRoot) {
					return InternalDictionary[key];
				}
			}
			set {
				lock (SyncRoot) {
					InternalDictionary[key] = value;
				}
			}
		}

		/// <summary>
		/// Attempts to return the value associated with <paramref name="key"/>, or <paramref name="def"/> if the key does not exist.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="def"></param>
		/// <returns></returns>
		internal TValue GetOrDefault(TKey key, TValue def) {
			lock (SyncRoot) {
				if (InternalDictionary.TryGetValue(key, out TValue value)) return value;
				return def;
			}
		}

		/// <summary>
		/// Attempts to return the value associated with <paramref name="key"/>, or <see langword="default"/> if the key does not exist.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		internal TValue GetValueOrDefault(TKey key) {
			lock (SyncRoot) {
				if (InternalDictionary.TryGetValue(key, out TValue value)) return value;
				return default;
			}
		}

		/// <inheritdoc/>
		public ICollection<TKey> Keys {
			get {
				lock (SyncRoot) {
					return InternalDictionary.Keys;
				}
			}
		}

		/// <inheritdoc/>
		public ICollection<TValue> Values {
			get {
				lock (SyncRoot) {
					return InternalDictionary.Values;
				}
			}
		}

		/// <inheritdoc/>
		public void Add(KeyValuePair<TKey, TValue> item) {
			lock (SyncRoot) {
				InternalDictionary.Add(item.Key, item.Value);
			}
		}

		/// <inheritdoc/>
		public void Clear() {
			lock (SyncRoot) {
				InternalDictionary.Clear();
			}
		}

		/// <inheritdoc/>
		public bool Contains(KeyValuePair<TKey, TValue> item) {
			lock (SyncRoot) {
				return ((IDictionary<TKey, TValue>)InternalDictionary).Contains(item);
			}
		}

		/// <inheritdoc/>
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
			lock (SyncRoot) {
				((IDictionary<TKey, TValue>)InternalDictionary).CopyTo(array, arrayIndex);
			}
		}

		/// <inheritdoc/>
		public bool Remove(KeyValuePair<TKey, TValue> item) {
			lock (SyncRoot) {
				return ((IDictionary<TKey, TValue>)InternalDictionary).Remove(item);
			}
		}

		/// <inheritdoc/>
		public int Count {
			get {
				lock (SyncRoot) {
					return InternalDictionary.Count;
				}
			}
		}

		/// <inheritdoc/>
		public bool IsReadOnly => ((IDictionary<TKey, TValue>)InternalDictionary).IsReadOnly;

		/// <inheritdoc/>
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => InternalDictionary.GetEnumerator();

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator() => InternalDictionary.GetEnumerator();

		/// <inheritdoc/>
		public void Add(object key, object? value) => Add((TKey)key, (TValue)value);

		/// <inheritdoc/>
		public bool Contains(object key) => Contains((TKey)key);

		/// <inheritdoc/>
		IDictionaryEnumerator IDictionary.GetEnumerator() => ((IDictionary)InternalDictionary).GetEnumerator();

		/// <inheritdoc/>
		public void Remove(object key) => Remove((TKey)key);

		/// <inheritdoc/>
		public bool IsFixedSize => ((IDictionary)InternalDictionary).IsFixedSize;

		/// <inheritdoc/>
		public object? this[object key] {
			get => this[(TKey)key];
			set => this[(TKey)key] = (TValue)value;
		}

		/// <inheritdoc/>
		ICollection IDictionary.Keys => (ICollection)((IDictionary<TKey, TValue>)this).Keys;

		/// <inheritdoc/>
		ICollection IDictionary.Values => (ICollection)((IDictionary<TKey, TValue>)this).Values;

		/// <inheritdoc/>
		public void CopyTo(Array array, int index) {
			lock (SyncRoot) {
				((IDictionary)InternalDictionary).CopyTo(array, index);
			}
		}

		/// <inheritdoc/>
		public bool IsSynchronized => true;

		/// <inheritdoc/>
		public object SyncRoot { get; } = new object();
	}
}
