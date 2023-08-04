using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.Exceptions.Marshalling;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EtiBotCore.DiscordObjects.Universal.Data {

	/// <summary>
	/// A container for permissions.
	/// </summary>
	
	public class PermissionContainer {

		//private List<PermissionInformation> Permissions { get; } = new List<PermissionInformation>();
		private readonly SynchronizedCollection<PermissionInformation> Permissions = new SynchronizedCollection<PermissionInformation>();

		/// <summary>
		/// A reference to the object that created this <see cref="PermissionContainer"/>.
		/// </summary>
		public GuildChannelBase Creator { get; }

		/// <summary>
		/// Whether or not <see cref="Creator"/> is in a state that prevents changes.
		/// </summary>
		public bool Locked => Creator.IgnoresNetworkUpdates;

		/// <summary>
		/// The name of the property that instantiated this <see cref="PermissionContainer"/>
		/// </summary>
		public string PropertyName { get; }

		/// <inheritdoc cref="RegisterPermissionFor(Snowflake, Payloads.Data.Permissions, Payloads.Data.Permissions)"/>
		public void RegisterPermissionFor(Role role, Permissions toAllow, Permissions toDeny) => RegisterPermissionFor(role.ID, toAllow, toDeny);

		/// <inheritdoc cref="RegisterPermissionFor(Snowflake, Payloads.Data.Permissions, Payloads.Data.Permissions)"/>
		public void RegisterPermissionFor(User user, Permissions toAllow, Permissions toDeny) => RegisterPermissionFor(user.ID, toAllow, toDeny);

		/// <summary>
		/// Registers permissions for the given <see cref="User"/> or <see cref="Role"/> on this channel with the given permissions to allow and deny. Anything that is neither allowed nor denied will be set to inherited. Likewise, anything that is wrongly registered where it both allows <strong>and</strong> denies permissions will be set to inherited as well.<para/>
		/// If an instance of <see cref="PermissionInformation"/> already exists for this <see cref="User"/> or <see cref="Role"/>, then this will update the existing object. Otherwise, this will create a new object.<para/><para/>
		/// If you wish to set a specific permission, it is often best to call <c>GetPermission(roleOrUser)</c> and to directly call set on the returned object rather than calling this method every time. This throws a <see cref="PropertyLockedException"/> if the object storing these permissions is locked.
		/// </summary>
		/// <param name="id">The user to add to this channel.</param>
		/// <param name="toAllow">The permissions to allow.</param>
		/// <param name="toDeny">The permissions to deny.</param>
		/// <exception cref="PropertyLockedException">If the object containing this <see cref="PermissionContainer"/> is locked.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the ability to modify this object.</exception>
		private void RegisterPermissionFor(Snowflake id, Permissions toAllow, Permissions toDeny) {
			if (Locked) throw new PropertyLockedException(PropertyName);
			DiscordObject.EnforcePermissions(Creator, Payloads.Data.Permissions.ManageChannels);

			PermissionInformation info = GetPermission(id) ?? new PermissionInformation(id, this);
			info.SetAllowed(toAllow);
			info.SetDenied(toDeny);
			info.SetInherited(toAllow & toDeny);
			SetPermission(id, info);
		}

		/// <inheritdoc cref="GetOrRegister(Snowflake)"/>
		public PermissionInformation GetOrRegister(Role role) => GetOrRegister(role.ID);

		/// <inheritdoc cref="GetOrRegister(Snowflake)"/>
		public PermissionInformation GetOrRegister(User user) => GetOrRegister(user.ID);

		/// <summary>
		/// Identical to calling <see cref="GetPermission(Snowflake)"/>, but this will register a new <see cref="PermissionInformation"/> for the given object and return it should one not have existed previously.
		/// </summary>
		/// <param name="id"></param>
		/// <exception cref="PropertyLockedException">If the object containing this <see cref="PermissionContainer"/> is locked.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the ability to modify this object.</exception>
		private PermissionInformation GetOrRegister(Snowflake id) {
			PermissionInformation? existing = GetPermission(id);
			if (existing != null) return existing;
			if (Locked) throw new PropertyLockedException(PropertyName);
			PermissionInformation newPerm = new PermissionInformation(id, this);
			SetPermission(id, newPerm);
			return newPerm;
		}


		/// <inheritdoc cref="GetPermission(Snowflake)"/>
		public PermissionInformation? GetPermission(Role role) => GetPermission(role.ID);

		/// <inheritdoc cref="GetPermission(Snowflake)"/>
		/// <remarks>
		/// This only returns for user-specific permissions, or, the permissions for this object explicitly added this specific person to the list.
		/// To get the effective permissions of a given user (based on their roles), use <see cref="Member.GetPermissionsInChannel(GuildChannelBase)"/>.
		/// </remarks>
		public PermissionInformation? GetPermission(User user) => GetPermission(user.ID);

		/// <summary>
		/// Forcefully gets or registers a <see cref="PermissionInformation"/> for the given ID, for the purpose of updating from a payload.<para/>
		/// This will return <see langword="null"/> if the object is unlocked (being edited), unless forceUnlock is true.
		/// This does not set the changed flag.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="forceUnlock">Forcefully treat the object as if it's unlocked.</param>
		/// <returns></returns>
		internal PermissionInformation? GetOrRegisterForDataPopulation(Snowflake id, bool forceUnlock = false) {
			PermissionInformation? existing = GetPermission(id);
			if (existing != null) return existing;
			if (!Locked && !forceUnlock) return null;
			PermissionInformation newPerm = new PermissionInformation(id, this);
			SetPermission(id, newPerm, true);
			return newPerm;
		}

		/// <summary>
		/// Acquires the permissions associated with the given <see cref="Role"/> or <see cref="User"/> in this channel, or <see langword="null"/> if no permissions are registered for the role or user.
		/// </summary>
		/// <param name="id">The ID of the role or user.</param>
		/// <returns></returns>
		private PermissionInformation? GetPermission(Snowflake id) => Permissions.Where(permission => permission.ID == id).FirstOrDefault();

		/// <summary>
		/// Removes the permissions of the given <see cref="User"/> from this channel, if present.
		/// </summary>
		/// <param name="user"></param>
		/// <exception cref="PropertyLockedException">If the object containing this <see cref="PermissionContainer"/> is locked.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the ability to modify this object.</exception>
		public void RemovePermissions(User user) => SetPermission(user.ID, null);

		/// <summary>
		/// Removes the permissions of this given <see cref="Role"/> from this channel, if present.
		/// </summary>
		/// <param name="role"></param>
		/// <exception cref="PropertyLockedException">If the object containing this <see cref="PermissionContainer"/> is locked.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the ability to modify this object.</exception>
		public void RemovePermissions(Role role) => SetPermission(role.ID, null);

		private void SetPermission(ulong id, PermissionInformation? value, bool isInternalUpdate = false) {
			if (!isInternalUpdate) DiscordObject.EnforcePermissions(Creator, Payloads.Data.Permissions.ManageChannels);
			PermissionContainer before = Clone();

			PermissionInformation? existing = GetPermission(id);
			if (value != null) {
				if (existing != null) {
					//Permissions[Permissions.IndexOf(existing)] = value;
					//Permissions.Replace(existing, value);
					Permissions.Remove(existing);
				}
				//} else {
				Permissions.Add(value);
				//}
				if (!isInternalUpdate) NotifyChange(before);
			} else if (existing != null) {
				//Permissions.RemoveAt(Permissions.IndexOf(existing));
				Permissions.Remove(existing);
				if (!isInternalUpdate) NotifyChange(before);
			}
		}

		/// <summary>
		/// Converts this container to a JSON string.
		/// </summary>
		/// <returns></returns>
		public List<JObject> ToJson() {
			List<JObject> objs = new List<JObject>();
			foreach (PermissionInformation info in Permissions) {
				objs.Add(JObject.FromObject(info));
			}
			return objs;
		}

		/// <summary>
		/// Tells the object storing this <see cref="PermissionContainer"/> that it has changed.
		/// </summary>
		internal void NotifyChange(PermissionContainer before) {
			//Creator.Changes[PropertyName] = this;
			Creator.RegisterChange(before, PropertyName);
		}

		/// <summary>
		/// Construct a new container for the given channel with the given property name storing this object.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="propertyName"></param>
		public PermissionContainer(GuildChannelBase source, [CallerMemberName] string? propertyName = null) {
			PropertyName = propertyName ?? "ERR_NULL_PROP_NAME";
			Creator = source;
		}

		/*
		internal void SetFrom(PermissionContainer other) {
			if (other.Creator.ID != Creator.ID || other.PropertyName != PropertyName) {
				throw new ArgumentException();
			}

			Permissions.Clear();
			foreach (PermissionInformation permInfo in other.Permissions) {
				PermissionInformation permsFor = new PermissionInformation(permInfo, this);
				Permissions.Add(permsFor);
			}
		}
		*/

		/// <summary>
		/// Copies this container and all permissions inside.
		/// </summary>
		/// <returns></returns>
		public PermissionContainer Clone() {
			PermissionContainer ctr = (PermissionContainer)MemberwiseClone();
			ctr.Permissions.Clear();
			foreach (PermissionInformation perm in Permissions) {
				ctr.Permissions.Add(perm.Clone(this));
			}
			return ctr;
		}
	}
}
