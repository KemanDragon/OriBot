using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OldOriBot.Utility.Enumerables {

	/// <summary>
	/// A custom implementation of an array that has a limited amount of space.<para/>
	/// Adding new objects to it will place the new object at index 0, bumping everything else up by 1 index. If an object's index exceeds the array's capacity, it is removed.
	/// </summary>
	public class LimitedSpaceArray<T> : IEnumerable<T> {

		private T[] ObjectsInternal { get; }

		/// <summary>
		/// A list of every object in this array.
		/// </summary>
		public IReadOnlyList<T> Objects {
			get {
				return ObjectsInternal.ToList().AsReadOnly();
			}
		}

		/// <summary>
		/// The maximum amount of objects this <see cref="LimitedSpaceArray{T}"/> can contain.
		/// </summary>
		public int Capacity { get; }

		/// <summary>
		/// The number of non-null objects this <see cref="LimitedSpaceArray{T}"/> contains.
		/// </summary>
		public int Count {
			get {
				int count = 0;
				foreach (T obj in ObjectsInternal) {
					if (obj != null) {
						count++;
					} else {
						return count;
					}
				}
				return count;
			}
		}

		/// <summary>
		/// The object type that this <see cref="LimitedSpaceArray{T}"/> stores.
		/// </summary>
		public Type ArrayType => typeof(T);

		/// <summary>
		/// If <see cref="ArrayType"/> extends <see cref="IDisposable"/>, and this is <see cref="true"/>, objects bumped off the end of the array will have their <see cref="IDisposable.Dispose()"/> method called.
		/// </summary>
		public bool DisposeOfBumpedObjects { get; }

		/// <summary>
		/// Construct a limited space array with the specified capacity.<para/>
		/// If <paramref name="disposeBumpedObjects"/> is <see cref="true"/>, and <see cref="T"/> implements <see cref="IDisposable"/>, then objects that are bumped off the end of the array will have their <see cref="IDisposable.Dispose()"/> method called.
		/// </summary>
		/// <param name="capacity">The maximum amount of elements in this array. Adding new elements that result in the object count exceeding this will cause the oldest element to be discarded.</param>
		/// <param name="disposeBumpedObjects">If <see cref="T"/> implements <see cref="IDisposable"/>, then objects that are bumped off the end of the array will have their <see cref="IDisposable.Dispose()"/> method called.</param>
		public LimitedSpaceArray(int capacity, bool disposeBumpedObjects = false) {
			ObjectsInternal = new T[capacity];
			Capacity = capacity;
			DisposeOfBumpedObjects = disposeBumpedObjects;
		}

		/// <summary>
		/// Adds an object to the start of this array. If adding this object causes the amount of objects in the array to exceed its limit, the oldest object (the object at the highest index) will be discarded.
		/// </summary>
		/// <param name="obj">The object to add.</param>
		public void Add(T obj) {
			// Update: If the type extends IDisposable then we need to dispose of the last object if it's getting bumped off.
			if (DisposeOfBumpedObjects) {
				T lastObject = ObjectsInternal.Last();
				if (lastObject != null && lastObject is IDisposable disposableObject) disposableObject.Dispose();
			}

			for (int idx = ObjectsInternal.Length - 1; idx >= 0; idx--) {
				//Console.WriteLine(idx);
				if (idx < ObjectsInternal.Length - 1) {
					ObjectsInternal[idx + 1] = ObjectsInternal[idx];
				}
				if (idx == 0) {
					//Console.WriteLine("Added a thing");
					ObjectsInternal[idx] = obj;
				}
			}
		}

		/// <summary>
		/// Removes <paramref name="obj"/> from this array, and then pulls all objects that were ahead of it backwards by 1 index, setting the last index of the array to <see cref="default"/>
		/// </summary>
		/// <param name="obj"></param>
		/// <exception cref="NullReferenceException">If the object does not exist in the array.</exception>
		public void Remove(T obj) {
			int i = -1;
			for (int idx = 0; idx < ObjectsInternal.Length; idx++) {
				if (ObjectsInternal[idx].Equals(obj)) {
					i = idx;
				}
			}
			if (i == -1) throw new NullReferenceException("Object could not be found in the array.");

			if (DisposeOfBumpedObjects) {
				if (obj is IDisposable disposableObject) disposableObject.Dispose();
			}

			for (int idx = i; idx < ObjectsInternal.Length - 1; idx++) {
				ObjectsInternal[idx] = ObjectsInternal[idx + 1];
			}
			ObjectsInternal[^1] = default;
		}

		/// <summary>
		/// Returns true if this <see cref="LimitedSpaceArray{T}"/> contains the specified object, and false if it does not.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public bool Contains(T obj) {
			return ObjectsInternal.Contains(obj);
		}

		/// <summary>
		/// Clears the contents of this <see cref="LimitedSpaceArray{T}"/>.
		/// </summary>
		/// <returns></returns>
		public void Clear() {
			for (int index = 0; index < ObjectsInternal.Length; index++) {
				T obj = ObjectsInternal[index];
				if (DisposeOfBumpedObjects && obj is IDisposable disposableObject) disposableObject.Dispose();
				ObjectsInternal[index] = default;
			}
		}

		public IEnumerator<T> GetEnumerator() {
			return Objects.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return Objects.GetEnumerator();
		}
		
		public T this[int index] {
			get {
				return ObjectsInternal[index];
			}
			set {
				ObjectsInternal[index] = value;
			}
		}
	}
}
