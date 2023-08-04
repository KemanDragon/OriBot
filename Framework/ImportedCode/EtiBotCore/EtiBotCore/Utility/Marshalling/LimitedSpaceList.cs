using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Utility.Marshalling {

	/// <summary>
	/// A list that has a maximum size.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class LimitedSpaceList<T> : IEnumerable<T> {

		private readonly List<T> InternalList = new List<T>();

		/// <summary>
		/// The maximum number of elements this can have.
		/// </summary>
		public int MaxCapacity { get; }

		/// <inheritdoc cref="List{T}.Count"/>
		public int Count => InternalList.Count;

		/// <summary>
		/// Construct a new <see cref="LimitedSpaceList{T}"/> with the given capacity.
		/// </summary>
		/// <param name="size"></param>
		public LimitedSpaceList(int size) {
			MaxCapacity = size;
		}

		internal LimitedSpaceList(LimitedSpaceList<T> other) {
			InternalList = other.InternalList;
			MaxCapacity = other.MaxCapacity;
		}

		/// <summary>
		/// Gets or sets the object at the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public T this[int index] {
			get {
				if (index < 0 || index > MaxCapacity) throw new ArgumentOutOfRangeException(nameof(index));
				return InternalList[index];
			}
			set {
				if (index < 0 || index > MaxCapacity) throw new ArgumentOutOfRangeException(nameof(index));
				InternalList[index] = value;
			}
		}

		/// <summary>
		/// Attempts to add the given object to the end of the list. Throws <see cref="InvalidOperationException"/> if the list is full.
		/// </summary>
		/// <exception cref="InvalidOperationException">If the list is full.</exception>
		public void Add(T item) {
			if (Count == MaxCapacity) throw new InvalidOperationException("This list is full!");
			InternalList.Add(item);
		}

		/// <inheritdoc cref="List{T}.Remove(T)"/>
		public void Remove(T item) => InternalList.Remove(item);

		/// <inheritdoc cref="List{T}.RemoveAt(int)"/>
		public void RemoveAt(int idx) => InternalList.RemoveAt(idx);

		/// <inheritdoc cref="List{T}.RemoveAll(Predicate{T})"/>
		public void RemoveAll(Predicate<T> match) => InternalList.RemoveAll(match);

		/// <inheritdoc cref="List{T}.RemoveRange(int, int)"/>
		public void RemoveRange(int index, int count) => InternalList.RemoveRange(index, count);

		/// <inheritdoc/>
		public IEnumerator<T> GetEnumerator() => InternalList.GetEnumerator();

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator() => InternalList.GetEnumerator();
	}
}
