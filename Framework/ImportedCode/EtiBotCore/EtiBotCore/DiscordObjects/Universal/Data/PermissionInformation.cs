using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using EtiBotCore.Data.JsonConversion;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.Exceptions.Marshalling;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Threading;
using EtiLogger.Logging;
using Newtonsoft.Json;

namespace EtiBotCore.DiscordObjects.Universal.Data {

	/// <summary>
	/// Represents permissions for a specific person or role. Think of this as one of the entries in the permissions configuration screen, one of the roles or people you can add.<para/>
	/// To read or write permissions, access this object like an array <see cref="this[Permissions]"/>
	/// </summary>
	
	public class PermissionInformation {

		private readonly ThreadedDictionary<Permissions, PermissionState> PermissionValues = new ThreadedDictionary<Permissions, PermissionState>();

		/// <summary>
		/// The name of the property that contains this <see cref="PermissionInformation"/>
		/// </summary>
		[JsonIgnore]
		public string PropertyName {
			get {
				if (_PropName != null) return _PropName;
				if (Parent != null) return Parent.PropertyName + $" [{ID}]";
				return $"UNKNOWN_OBJECT [{ID}]";
			}
		}
		[JsonIgnore] private string? _PropName { get; }

		/// <summary>
		/// The role or user ID that this overwrite applies to.
		/// </summary>
		[JsonProperty("id")] public Snowflake ID { get; }

		/// <summary>
		/// What this permission object applies to, which is either a role in the server's role list, or a role/member in a channel's permission object.
		/// </summary>
		[JsonConverter(typeof(EnumConverter)), JsonProperty("type")] public PermissionTarget Type { get; }

		/// <summary>
		/// Whether or not the <see cref="PermissionState.Inherit"/> value is allowed for permissions. This is <see langword="true"/> for permissions associated with channels, and <see langword="false"/> for literal role objects.<para/>
		/// If any permissions are set to <see cref="PermissionState.Inherit"/> and this <see cref="PermissionInformation"/> is associated with a role in the server's role list, then the inherited permissions will be <strong>ignored.</strong> This means that no changes will be made to the faulty flags.
		/// </summary>
		[JsonIgnore] public bool AllowsInherited { get; }

		/// <summary>
		/// The <see cref="PermissionContainer"/> containing this <see cref="PermissionInformation"/>.
		/// </summary>
		[JsonIgnore] public PermissionContainer? Parent { get; }

		/// <summary>
		/// A reference to the object that created this <see cref="PermissionInformation"/>, which is only set for cases where this is a standalone <see cref="PermissionInformation"/> not present in a <see cref="PermissionContainer"/> (e.g. on a <see cref="Role"/>)
		/// </summary>
		[JsonIgnore] public Role? Creator { get; }

		/// <summary>
		/// Whether or not <see cref="Creator"/> or <see cref="Parent"/> is in a state that prevents changes (whichever is applicable for this object).
		/// </summary>
		[JsonIgnore]
		public bool Locked {
			get {
				if (Creator != null) return Creator.IgnoresNetworkUpdates;
				if (Parent != null) return Parent.Locked;
				return false;
			}
		}

		/// <summary>
		/// Get or set a given permission. The input permission must be a <strong>single permission (not a combined flag of multiple permissions)</strong>.<para/>
		/// Attempting to pass in a merged flag will raise an <see cref="ArgumentException"/>. Passing in <see cref="Permissions.None"/> will always return <see cref="PermissionState.Inherit"/>.<para/>
		/// Setting something to <see cref="PermissionState.Inherit"/> when <see cref="AllowsInherited"/> is <see langword="false"/> will not change the value.
		/// </summary>
		/// <param name="perms"></param>
		/// <returns></returns>
		/// <exception cref="PropertyLockedException">If this is part of a <see cref="DiscordObject"/> and the object is locked.</exception>
		/// <exception cref="ArgumentException">If the input permissions enum is undefined.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the permissions necessary to alter permissions.</exception>
		public PermissionState this[Permissions perms] {
			get {
				if (perms == Permissions.None) return PermissionState.Inherit;
				if (!Enum.IsDefined(typeof(Permissions), perms)) throw new ArgumentException("The input permissions contain more than one permission reference or are an invalid value.");
				return PermissionValues.GetOrDefault(perms, PermissionState.Inherit);
			}
			set {
				CheckRequirements();
				if (Locked) throw new PropertyLockedException();
				if (perms == Permissions.None) return;
				if (value == PermissionState.Inherit && !AllowsInherited) return;
				if (!Enum.IsDefined(typeof(Permissions), perms)) throw new ArgumentException("The input permissions contain more than one permission reference or are an invalid value.");
				PermissionContainer? before = null;
				if (Parent != null) before = Parent.Clone();

				if (PermissionValues.ContainsKey(perms) && PermissionValues[perms] == value) return;
				PermissionValues[perms] = value;
				if (Parent != null) Parent.NotifyChange(before!);
			}
		}

		private PermissionInformation(PermissionInformation other, PermissionContainer? newParent = null) {
			_PropName = other._PropName;
			ID = other.ID;
			Type = other.Type;
			AllowsInherited = other.AllowsInherited;
			Creator = other.Creator;
			Parent = newParent ?? other.Parent;
			SetTo(other.GetAllowed(), PermissionState.Allow, true);
			SetTo(other.GetDenied(), PermissionState.Deny, true);
			if (AllowsInherited) SetTo(other.GetInherited(), PermissionState.Inherit, true);
		}

		/// <summary>
		/// Construct a new <see cref="PermissionInformation"/> for a role in the server's role list. This constructor is not the right one if this permissions is associated with a channel.
		/// </summary>
		/// <param name="source">The role in the server's role list.</param>
		/// <param name="name">The name of the property that created this.</param>
		internal PermissionInformation(Role source, [CallerMemberName] string? name = null) {
			_PropName = name;
			ID = source.ID;
			Type = PermissionTarget.ServerRole;
			AllowsInherited = false;
			Creator = source;
			Parent = null;
		}

		/// <summary>
		/// Construct a new <see cref="PermissionInformation"/> for a channel.
		/// </summary>
		/// <param name="associatedID">The ID of whatever this affects.</param>
		/// <param name="parent">The <see cref="PermissionContainer"/> storing this.</param>
		internal PermissionInformation(ulong associatedID, PermissionContainer parent) {
			ID = associatedID;
			Type = PermissionTarget.ChannelRoleOrMember;
			AllowsInherited = true;
			Creator = null;
			Parent = parent;
		}

		/// <summary>
		/// Depending on the state of <see cref="AllowsInherited"/>, this will either set all permissions to <see cref="PermissionState.Inherit"/> or <see cref="PermissionState.Deny"/>, resetting the permissions for this <see cref="Role"/> or Role/<see cref="User"/> in a <see cref="GuildChannelBase"/>.
		/// </summary>
		public void Reset() {
			CheckRequirements();
			PermissionContainer? before = null;
			if (Parent != null) before = Parent.Clone();
			PermissionState target = AllowsInherited ? PermissionState.Inherit : PermissionState.Deny;
			IList list = Enum.GetValues(typeof(Permissions));
			for (int i = 0; i < list.Count; i++) {
				Permissions value = (Permissions)list[i]!;
				this[value] = target;
			}
			if (Parent != null) Parent.NotifyChange(before!);
		}

		/// <summary>
		/// Depending on the state of <see cref="AllowsInherited"/>, this will either set all permissions to <see cref="PermissionState.Inherit"/> or <see cref="PermissionState.Deny"/><para/>
		/// Unlike <see cref="Reset"/>, this does not check the state / lock of the object and always sets the data unquestionably. This also does not notify the parent container about a change.
		/// </summary>
		internal void DataReset() {
			PermissionState target = AllowsInherited ? PermissionState.Inherit : PermissionState.Deny;
			IList list = Enum.GetValues(typeof(Permissions));
			for (int i = 0; i < list.Count; i++) {
				Permissions value = (Permissions)list[i]!;
				PermissionValues[value] = target;
			}
		}

		/// <summary>
		/// Sets the allowed permissions to the given flags.
		/// </summary>
		/// <param name="allowed"></param>
		/// <exception cref="PropertyLockedException">If the object storing this is locked.</exception>
		public void SetAllowed(Permissions allowed) {
			CheckRequirements();
			if (Locked) throw new PropertyLockedException(_PropName);
			SetTo(allowed, PermissionState.Allow);
		}

		/// <summary>
		/// Sets the inherited permissions to the given flags.
		/// </summary>
		/// <param name="inherited"></param>
		/// <exception cref="PropertyLockedException">If the object storing this is locked.</exception>
		public void SetInherited(Permissions inherited) {
			CheckRequirements();
			if (Locked) throw new PropertyLockedException(_PropName);
			if (!AllowsInherited) return;
			SetTo(inherited, PermissionState.Inherit);
		}

		/// <summary>
		/// Sets the denied permissions to the given flags.
		/// </summary>
		/// <param name="denied"></param>
		/// <exception cref="PropertyLockedException">If the object storing this is locked.</exception>
		public void SetDenied(Permissions denied) {
			CheckRequirements();
			if (Locked) throw new PropertyLockedException(_PropName);
			SetTo(denied, PermissionState.Deny);
		}

		/// <summary>
		/// Forcefully updates the given permissions flags to the given state. Ignores the item's locked state, and does not notify the parent of changes (if applicable).
		/// </summary>
		/// <param name="perms"></param>
		/// <param name="state"></param>
		/// <param name="isInUpdate"></param>
		/// <exception cref="PropertyLockedException">If the object storing this is locked.</exception>
		internal void SetTo(Permissions perms, PermissionState state, bool isInUpdate = false) {
			if (perms == Permissions.None) return;
			IList list = Enum.GetValues(typeof(Permissions));
			PermissionContainer? parentBefore = null;
			Role? roleBefore = null;
			if (!isInUpdate && Parent != null) parentBefore = Parent.Clone();
			if (!isInUpdate && Creator != null) roleBefore = (Role)Creator.MemberwiseClone();
			for (int i = 0; i < list.Count; i++) {
				Permissions value = (Permissions)list[i]!;
				if (value == Permissions.None) continue;
				if (perms.HasFlag(value)) {
					PermissionValues[value] = state;
				}
			}
			if (!isInUpdate) {
				if (Parent != null) Parent.NotifyChange(parentBefore!);
				if (Creator != null) Creator.NotifyChange(roleBefore!);
			}
		}

		/// <summary>
		/// Returns all permissions that are allowed.
		/// </summary>
		/// <returns></returns>
		public Permissions GetAllowed() {
			Permissions perms = Permissions.None;
			foreach (KeyValuePair<Permissions, PermissionState> permBinding in PermissionValues) {
				Permissions perm = permBinding.Key;
				if (permBinding.Value == PermissionState.Allow) {
					perms |= perm;
				}
			}
			return perms;
		}

		/// <summary>
		/// Returns all inherited permissions. Returns <see cref="Permissions.None"/> if <see cref="AllowsInherited"/> is <see langword="false"/>, which applies to roles in the server list.
		/// </summary>
		/// <returns></returns>
		public Permissions GetInherited() {
			if (!AllowsInherited) return Permissions.None;
			Permissions perms = Permissions.None;
			foreach (KeyValuePair<Permissions, PermissionState> permBinding in PermissionValues) {
				Permissions perm = permBinding.Key;
				if (permBinding.Value == PermissionState.Inherit) {
					perms |= perm;
				}
			}
			return perms;
		}

		/// <summary>
		/// Returns all permissions that are denied (or just off in the case of roles).
		/// </summary>
		/// <returns></returns>
		public Permissions GetDenied() {
			Permissions perms = Permissions.None;
			foreach (KeyValuePair<Permissions, PermissionState> permBinding in PermissionValues) {
				Permissions perm = permBinding.Key;
				if (permBinding.Value == PermissionState.Deny) {
					perms |= perm;
				}
			}
			return perms;
		}
		
		/// <summary>
		/// Given a set of "base permissions" (or, permissions that are already allowed), this applies the permissions from this object and returns the result.
		/// </summary>
		/// <param name="basePermissions">The pre-existing permissions to modify.</param>
		/// <returns>The base permissions after being modified by this object.</returns>
		public Permissions ApplyTo(Permissions basePermissions) {
			basePermissions &= ~GetDenied();
			basePermissions |= GetAllowed();
			return basePermissions;
		}

		/// <summary>
		/// Verifies all permissions.
		/// </summary>
		/// <exception cref="ObjectUnavailableException">If Discord is suffering from an outage or the bot has disconnected.</exception>
		/// <exception cref="InsufficientPermissionException">If anything is not possible.</exception>
		private void CheckRequirements() {
			if (Creator != null) {
				// Make sure we can manage roles.
				DiscordObject.EnforcePermissions(Creator.Server, Permissions.ManageRoles);

				// Okay, cool. Position?
				int thisRolePos = Creator.Position;
				int myHighestRolePos = ((Role)Creator.Server.BotMember.Roles.GetHighestElement()!).Position;
				if (thisRolePos >= myHighestRolePos) {
					throw new InsufficientPermissionException("Attempted to edit a role higher than or equal to the bot's highest role.");
				}

				//Dope. Carry on.
			} else if (Parent != null) {
				// This is for a channel.
				DiscordObject.EnforcePermissions(Parent.Creator!.Server!, Permissions.ManageChannels);
			}
		}

#pragma warning disable IDE0052 // Remove unread private members
		[JsonProperty("allow")] private Permissions Allowed = Permissions.None;
		[JsonProperty("deny")] private Permissions Denied = Permissions.None;
#pragma warning restore IDE0052 // Remove unread private members

		/// <inheritdoc/>
		public bool ShouldSerializeAllowed() {
			Allowed = GetAllowed();
			return true;
		}

		/// <inheritdoc/>
		public bool ShouldSerializeDenied() {
			Denied = GetDenied();
			return true;
		}

		internal PermissionInformation Clone(PermissionContainer? parent = null) {
			return new PermissionInformation(this, parent);
		}

		/// <summary>
		/// Describes what this overwrite applies to.
		/// </summary>
		public enum PermissionTarget {

			/// <summary>
			/// This overwrite applies to a role or member in a channel's permissions.
			/// </summary>
			ChannelRoleOrMember = 0,

			/// <summary>
			/// This overwrite applies to a role in the server's role list.
			/// </summary>
			ServerRole = 1

		}
	}
}
