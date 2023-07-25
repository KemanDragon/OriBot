using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.Exceptions;
using EtiBotCore.Exceptions.Marshalling;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Threading;

namespace EtiBotCore.Data.Container {

	/// <summary>
	/// A generic container of <see cref="DiscordObject"/>s.
	/// </summary>
	public class DiscordObjectContainer : IEnumerable<DiscordObject> {

		internal ThreadedDictionary<Snowflake, DiscordObject> InternalList = new ThreadedDictionary<Snowflake, DiscordObject>();

		/// <summary>
		/// The <see cref="DiscordObject"/> that instantiated this <see cref="DiscordObjectContainer"/>
		/// </summary>
		protected readonly DiscordObject Creator;

		/// <summary>
		/// The name of the property that this <see cref="DiscordObjectContainer"/> exists in.
		/// </summary>
		protected readonly string Property;

		/// <summary>
		/// Whether or not to remove an object if it is set to null.
		/// </summary>
		protected readonly bool RemoveIfSetToNull;

		/// <summary>
		/// Must be manually unset. If only one item was added or removed to this, this is the object that was added or removed.
		/// </summary>
		internal DiscordObject? SingularChange { get; set; } = null;

		/// <summary>
		/// Coupled with <see cref="SingularChange"/>, this keeps track of if a change has occurred yet.
		/// </summary>
		internal bool HasChangedYet { get; set; } = false;

		/// <summary>
		/// <see langword="false"/> if the change was something added, and <see langword="true"/> if the change was something removed.
		/// </summary>
		internal bool WasChangeRemoval { get; set; } = false;

		/// <summary>
		/// If the object is removed from this list, delete it.
		/// </summary>
		public bool DeleteIfRemoved { get; internal set; } = true;

		/// <inheritdoc cref="List{T}.Count"/>
		public int Count => InternalList.Count;

		/// <summary>
		/// Returns whether or not <see cref="DiscordObject"/> is sortable.
		/// </summary>
		private bool IsSortable { get; } = typeof(DiscordObject).GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IComparable<>));

		/// <summary>
		/// Whether or not this container is read-only.
		/// </summary>
		public bool IsReadOnly { get; }

		/// <summary>
		/// Any extra requirements to check in terms of permissions. Returns an exception to throw if something isn't right.
		/// </summary>
		internal Func<Exception?>? ExtraRequirementDelegate { get; set; } = null;

		/// <summary>
		/// Returns the object in this container with the given ID, or <see langword="null"/> if it could not be found.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public DiscordObject? this[Snowflake id] {
			//get => InternalList.Where(obj => obj?.ID == id).FirstOrDefault();
			get {
				InternalList.TryGetValue(id, out DiscordObject? o);
				return o;
			}
		}

		/// <summary>
		/// Adds <paramref name="obj"/> to this container.
		/// </summary>
		/// <inheritdoc cref="VerifyAllRequirements"/>
		public void Add(DiscordObject obj) {
			VerifyAllRequirements();
			var before = Clone();

			//InternalList.Add(obj);
			InternalList[obj.ID] = obj;
			Creator.RegisterChange(before, Property);
			if (!HasChangedYet) {
				SingularChange = obj;
				HasChangedYet = true;
				WasChangeRemoval = false;
			} else {
				SingularChange = null;
			}
		}

		/// <summary>
		/// Removes <paramref name="obj"/> from this container, or does nothing if it's not here.
		/// </summary>
		/// <inheritdoc cref="VerifyAllRequirements"/>
		public void Remove(DiscordObject obj) {
			VerifyAllRequirements();
			if (!InternalList.ContainsKey(obj.ID)) return;
			var before = Clone();

			InternalList.Remove(obj.ID, out DiscordObject? _);
			if (DeleteIfRemoved) obj.Deleted = true;
			Creator.RegisterChange(before, Property);
			if (!HasChangedYet) {
				SingularChange = obj;
				HasChangedYet = true;
				WasChangeRemoval = true;
			} else {
				SingularChange = null;
			}
		}

		/// <summary>
		/// Removes the object from this container with the given snowflake.
		/// </summary>
		/// <param name="objWithId"></param>
		/// <inheritdoc cref="VerifyAllRequirements"/>
		public void Remove(Snowflake objWithId) {
			VerifyAllRequirements();
			
			if (InternalList.TryGetValue(objWithId, out DiscordObject? obj)) {
				var before = Clone();

				InternalList.Remove(objWithId, out DiscordObject? _);
				if (DeleteIfRemoved) obj!.Deleted = true;
				Creator.RegisterChange(before, Property);
				if (!HasChangedYet) {
					SingularChange = obj;
					HasChangedYet = true;
					WasChangeRemoval = true;
				} else {
					SingularChange = null;
				}
			}
		}

		internal void AddInternally(DiscordObject obj) {
			//InternalList.Add(obj);
			InternalList[obj.ID] = obj;
		}

		internal void RemoveInternally(DiscordObject obj) {
			//InternalList.Remove(obj);
			InternalList.Remove(obj.ID, out DiscordObject? _);
		}

		internal void RemoveInternally(Snowflake id) {
			/*
			DiscordObject obj = InternalList.Find(obj => {
				return obj?.ID == id;
			})!;
			if (obj != null) {
				InternalList.Remove(obj);
			}
			*/
			InternalList.Remove(id, out DiscordObject? _);
		}

		/// <summary>
		/// Returns whether or not this container contains the given object.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public bool Contains(DiscordObject obj) {
			return InternalList.ContainsKey(obj.ID);
		}

		/// <summary>
		/// Returns whether or not this container contains an object with the given ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool Contains(Snowflake id) {
			/*
			return InternalList.Find(obj => {
				return obj?.ID == id;
			}) != null;
			*/
			return InternalList.ContainsKey(id);
		}

		/// <summary>
		/// Returns whether or not this container contains an object that satisfies the given predicate.
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public bool Contains(Predicate<Snowflake> predicate) {
			return InternalList.Values.FirstOrDefault(obj => {
				return predicate(obj.ID);
			}) != null;
		}

		/// <summary>
		/// Used in update calls, sets the contents of this container to the given object unless the creator object is being edited.<para/>
		/// Ignores the readonly state of this object (see <see cref="IsReadOnly"/>), as this is used for data storage updates.
		/// </summary>
		/// <param name="objects"></param>
		public void SetTo(IEnumerable<DiscordObject> objects) {
			if (!Creator.IgnoresNetworkUpdates) return; // Not locked = being edited
										 //InternalList = new List<DiscordObject>(objects);
			InternalList.Clear();
			foreach (DiscordObject obj in objects) {
				InternalList[obj.ID] = obj;
			}
		}

		/// <summary>
		/// Returns the highest <see cref="DiscordObject"/>, or, whichever has the highest ID.
		/// <strong>Note:</strong> This sorts every time it is called.
		/// </summary>
		public DiscordObject? GetHighestElement() {
			if (!IsSortable) return null;
			DiscordObject?[] objects = InternalList.Values.ToArray();
			Array.Sort(objects);
			return objects[^1];
		}

		/// <summary>
		/// Converts this container to an array of IDs for all objects in this container.
		/// </summary>
		/// <returns></returns>
		public Snowflake[] ToIDArray(Predicate<DiscordObject>? where = null) {
			if (where == null) return InternalList.Keys.ToArray();

			List<Snowflake> ids = new List<Snowflake>();
			DiscordObject[] objs = InternalList.Values.ToArray();
			for (int idx = 0; idx < objs.Length; idx++) {
				if (where?.Invoke(objs[idx]) ?? true) {
					ids.Add(objs[idx]?.ID ?? Snowflake.Invalid);
				}
			}
			return ids.ToArray();
		}

		/// <summary>
		/// Construct a new container with the given object.
		/// </summary>
		/// <param name="source">The <see cref="DiscordObject"/> that contains this container.</param>
		/// <param name="isReadonly">If this container cannot be written to.</param>
		/// <param name="removeIfSetToNull">If <see langword="true"/>, setting any index to <see langword="null"/> will remove it rather than actually setting it to <see langword="null"/></param>
		/// <param name="propertyName">The name of the property in the source object that is set to this container. This is automatically populated by default.</param>
		public DiscordObjectContainer(DiscordObject source, bool removeIfSetToNull = true, bool isReadonly = false, [CallerMemberName] string? propertyName = null) {
			if (propertyName == null) throw new ArgumentNullException(nameof(propertyName), "The property name cannot be null! If it is being excluded, ensure it is in a member (e.g. a property) with a name that [CallerMemberName] can pick up.");
			Creator = source;
			RemoveIfSetToNull = removeIfSetToNull;
			Property = propertyName;
			IsReadOnly = isReadonly;
		}

		/// <summary>
		/// Used to throw necessary exceptions.
		/// </summary>
		/// <exception cref="InvalidOperationException">If this container is read only.</exception>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="ObjectDeletedException">If the object with this container has been deleted and cannot be edited.</exception>
		/// <exception cref="ObjectUnavailableException">If the object that has this container is not available for any reason.</exception>
		/// <exception cref="InsufficientPermissionException">If this container cannot be edited.</exception>
		protected void VerifyAllRequirements() {
			if (IsReadOnly) throw new InvalidOperationException("This container is read-only.");
			if (Creator.IgnoresNetworkUpdates) throw new PropertyLockedException(Property);
			if (Creator.Deleted) throw new ObjectDeletedException(Creator);
			if (Creator is Guild guild && guild.Unavailable) throw new ObjectUnavailableException(Creator);
			Exception? error = ExtraRequirementDelegate?.Invoke();
			if (error != null) throw error;
		}

		/// <summary>
		/// Sets <see cref="HasChangedYet"/>, <see cref="SingularChange"/>, and <see cref="WasChangeRemoval"/> to their <see langword="default"/>s.<para/>
		/// This is used to tell this container that its changes have been acknowledged by the parent's SendChangesToDiscord method.
		/// </summary>
		internal void Reset() {
			HasChangedYet = default;
			SingularChange = default;
			WasChangeRemoval = default;
		}

		/// <inheritdoc/>
		public IEnumerator<DiscordObject> GetEnumerator() => InternalList.Values.GetEnumerator();

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator() => InternalList.Values.GetEnumerator();

		/// <summary>
		/// Returns a shallow copy of this <see cref="DiscordObjectContainer{T}"/>, with the exception that the list is not copied by reference (though its stored objects are)
		/// </summary>
		/// <returns></returns>
		internal DiscordObjectContainer Clone() {
			DiscordObjectContainer ctr = (DiscordObjectContainer)MemberwiseClone();
			ctr.InternalList = InternalList.LazyCopy();
			return ctr;
		}

		/// <summary>
		/// Converts this <see cref="DiscordObjectContainer"/> to a <see cref="List{T}"/>
		/// </summary>
		/// <returns></returns>
		public List<DiscordObject> ToList() {
			return InternalList.Values.ToList();
		}
	}

	/// <summary>
	/// A class similar to <see cref="List{T}"/> that stores a number of <typeparamref name="T"/>, but it has code to handle when something is added or removed. Specifically, it will verify whether or not the object is locked.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	
	public class DiscordObjectContainer<T> : DiscordObjectContainer, IEnumerable, IEnumerable<T> where T : DiscordObject {

		/// <summary>
		/// Returns the object in this container with the given ID, or <see langword="null"/> if it could not be found.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public new T? this[Snowflake id] {
			//get => (T)InternalList.Where(obj => obj?.ID == id).FirstOrDefault();
			get {
				InternalList.TryGetValue(id, out DiscordObject? obj);
				return (T)obj!;
			}
		}

		/// <summary>
		/// Adds <paramref name="obj"/> to this container.
		/// </summary>
		public void Add(T obj) => base.Add(obj);

		/// <summary>
		/// Removes <paramref name="obj"/> from this container, or does nothing if it's not here.
		/// </summary>
		public void Remove(T obj) => base.Remove(obj);

		/// <summary>
		/// Removes all objects from this container.
		/// </summary>
		public void Clear(Predicate<T>? where = null) {
			VerifyAllRequirements();
			Reset(); // for change tracking
			var before = Clone();

			int numChanged = 0;
			T? change = default;
			T[] objects = InternalList.Values.Cast<T>().ToArray();
			foreach (T obj in objects) {
				if (where?.Invoke(obj) ?? true) {
					//InternalList.Remove(obj);
					InternalList.Remove(obj.ID, out DiscordObject? _);
					if (DeleteIfRemoved) obj.Deleted = true;
					change = obj;
					numChanged++;
					HasChangedYet = true;
					WasChangeRemoval = true;
				}
			}
			if (numChanged == 1) {
				SingularChange = change!;
			}
			if (numChanged != 0) {
				Creator.RegisterChange(before, Property);
			}
		}

		internal void AddInternally(T obj) {
			//InternalList.Add(obj);
			base.AddInternally(obj);
		}

		internal void RemoveInternally(T obj) {
			//InternalList.Remove(obj);
			base.RemoveInternally(obj);
		}

		/// <summary>
		/// Akin to <see cref="Clear"/>, but this does not signal a change.
		/// </summary>
		internal void ClearInternally() {
			InternalList.Clear();
		}

		/// <summary>
		/// Returns whether or not this container contains the given object.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public bool Contains(T obj) {
			//return InternalList.Contains(obj);
			return InternalList.ContainsKey(obj.ID);
		}

		/// <summary>
		/// Returns whether or not this container contains an object that satisfies the given predicate.
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public bool Contains(Predicate<T> predicate) {
			/*
			return InternalList.Find(obj => {
				return predicate((T)obj);
			}) != null;
			*/
			return InternalList.Values.FirstOrDefault(obj => {
				return predicate((T)obj);
			}) != null;
		}

		/// <summary>
		/// Used in update calls, sets the contents of this container to the given object unless the creator object is being edited.<para/>
		/// Ignores the readonly state of this object (see <see cref="DiscordObjectContainer.IsReadOnly"/>), as this is used for data storage updates.
		/// </summary>
		/// <param name="objects"></param>
		public void SetTo(IEnumerable<T> objects) {
			if (!Creator.IgnoresNetworkUpdates) return; // Not locked = being edited
										 //InternalList = new List<DiscordObject>(objects);
			InternalList.Clear();
			foreach (T obj in objects) {
				InternalList[obj.ID] = obj;
			}
		}

		/// <summary>
		/// Converts this container to an array of IDs for all objects in this container.
		/// </summary>
		/// <returns></returns>
		public Snowflake[] ToIDArray(Predicate<T>? where = null) {
			List<Snowflake> ids = new List<Snowflake>();
			DiscordObject[] objs = InternalList.Values.ToArray();
			for (int idx = 0; idx < objs.Length; idx++) {
				if (where?.Invoke((T)objs[idx]) ?? true) {
					ids.Add(objs[idx]?.ID ?? Snowflake.Invalid);
				}
			}
			return ids.ToArray();
		}

		/// <summary>
		/// Converts this container to an array of the given object type for all objects in this container.
		/// </summary>
		/// <returns></returns>
		public T[] ToArray(Predicate<T>? where = null) {
			List<T> objects = new List<T>();
			DiscordObject[] objs = InternalList.Values.ToArray();
			for (int idx = 0; idx < objs.Length; idx++) {
				if (where?.Invoke((T)objs[idx]) ?? true) {
					objects.Add((T)objs[idx]);
				}
			}
			return objects.ToArray();
		}

		/// <summary>
		/// Construct a new container with the given object.
		/// </summary>
		/// <param name="source">The <see cref="DiscordObject"/> that contains this container.</param>
		/// <param name="isReadonly">If this container cannot be written to.</param>
		/// <param name="removeIfSetToNull">If <see langword="true"/>, setting any index to <see langword="null"/> will remove it rather than actually setting it to <see langword="null"/></param>
		/// <param name="propertyName">The name of the property in the source object that is set to this container. This is automatically populated by default.</param>
		public DiscordObjectContainer(DiscordObject source, bool removeIfSetToNull = true, bool isReadonly = false, [CallerMemberName] string? propertyName = null)
			: base(source, removeIfSetToNull, isReadonly, propertyName) { }

		/// <inheritdoc/>
		public new IEnumerator<T> GetEnumerator() => InternalList.Values.Cast<T>().GetEnumerator();

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator() => InternalList.Values.GetEnumerator();

		/// <summary>
		/// Returns a shallow copy of this <see cref="DiscordObjectContainer{T}"/>, with the exception that the list is not copied by reference (though its stored objects are)
		/// </summary>
		/// <returns></returns>
		internal new DiscordObjectContainer<T> Clone() {
			DiscordObjectContainer<T> ctr = (DiscordObjectContainer<T>)MemberwiseClone();
			ctr.InternalList = InternalList.LazyCopy();
			return ctr;
		}

		/// <summary>
		/// Converts this <see cref="DiscordObjectContainer"/> to a <see cref="List{T}"/>
		/// </summary>
		/// <returns></returns>
		public new List<T> ToList() {
			return InternalList.Values.Cast<T>().ToList();
		}
	}
}
