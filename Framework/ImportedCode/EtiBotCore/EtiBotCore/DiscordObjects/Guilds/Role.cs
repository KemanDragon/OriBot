using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Exceptions.Marshalling;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Events;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Marshalling;
using EtiBotCore.Utility.Threading;
using EtiLogger.Data.Structs;
using EtiLogger.Logging;

namespace EtiBotCore.DiscordObjects.Guilds {

	/// <summary>
	/// A role in a server.
	/// </summary>
	
	public class Role : DiscordObject, IComparable<Role> {

		/// <summary>
		/// A binding from Snowflake to Role for existing roles. There is no server-dependant list here because Roles' IDs are not server-dependant.
		/// </summary>
		internal static readonly ThreadedDictionary<Snowflake, Role> RoleRegistry = new ThreadedDictionary<Snowflake, Role>();

		/// <summary>
		/// A string that mentions this role by ID.
		/// </summary>
		public string Mention => $"<@&{ID}>";

		/// <summary>
		/// The server that this <see cref="Role"/> exists in.
		/// </summary>
		public Guild Server { get; }

		/// <summary>
		/// The name of this role.
		/// </summary>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the permissions needed to do this.</exception>
		/// <exception cref="ObjectDeletedException">If this object has been deleted and cannot be edited.</exception>
		public string Name {
			get => _Name;
			set {
				EnforcePermissions(Server, Payloads.Data.Permissions.ManageRoles);
				SetProperty(ref _Name, value);
			}
		}
		private string _Name = string.Empty;

		/// <summary>
		/// The color of this role, or <see langword="null"/> for no color.
		/// </summary>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the permissions needed to do this.</exception>
		/// <exception cref="ObjectDeletedException">If this object has been deleted and cannot be edited.</exception>
		public Color? Color {
			get => _Color;
			set {
				EnforcePermissions(Server, Payloads.Data.Permissions.ManageRoles);
				SetProperty(ref _Color, value);
			}
		}
		private Color? _Color = null;

		/// <summary>
		/// Whether or not this role is shown uniquely in the member list.
		/// </summary>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the permissions needed to do this.</exception>
		/// <exception cref="ObjectDeletedException">If this object has been deleted and cannot be edited.</exception>
		public bool Hoisted {
			get => _Hoisted;
			set {
				EnforcePermissions(Server, Payloads.Data.Permissions.ManageRoles);
				SetProperty(ref _Hoisted, value);
			}
		}
		private bool _Hoisted = false;

		/// <summary>
		/// Whether or not people can mention this role.
		/// </summary>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the permissions needed to do this.</exception>
		/// <exception cref="ObjectDeletedException">If this object has been deleted and cannot be edited.</exception>
		public bool Mentionable {
			get => _Mentionable;
			set {
				EnforcePermissions(Server, Payloads.Data.Permissions.ManageRoles);
				SetProperty(ref _Mentionable, value);
			}
		}
		private bool _Mentionable = false;

		/// <summary>
		/// Whether or not this role is managed by an integration.
		/// </summary>
		public bool Integrated { get; private set; }

		/// <summary>
		/// The permissions this role has. This object can be accessed like an array: <c>role.Permissions[Permissions.SendMessages] = PermissionState.Allow</c><para/>
		/// </summary>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the permissions needed to do this.</exception>
		/// <exception cref="ObjectDeletedException">If this object has been deleted and cannot be edited.</exception>
		public PermissionInformation Permissions {
			get {
				if (_Permissions == null) {
					_Permissions = new PermissionInformation(this);
				}
				return _Permissions;
			}
		}
		private PermissionInformation? _Permissions = null;

		/// <summary>
		/// The role's position in the list.
		/// </summary>
		/// <exception cref="PropertyLockedException">If this property is not able to be changed at this point in time.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the permissions needed to do this.</exception>
		/// <exception cref="ObjectDeletedException">If this object has been deleted and cannot be edited.</exception>
		public int Position {
			get => _Position;
			set {
				EnforcePermissions(Server, Payloads.Data.Permissions.ManageRoles);
				SetProperty(ref _Position, value);
			}
		}
		private int _Position = 0;

		internal void NotifyChange(Role before) {
			//Changes[nameof(Permissions)] = this;
			RegisterChange(before, nameof(Permissions));
		}


		/// <summary>
		/// Throws <see cref="InvalidOperationException"/> as a guild must be provided to properly create a role.
		/// </summary>
		/// <param name="id"></param>
		internal Role(Snowflake id) : base(id) {
			throw new InvalidOperationException("A role must be paired with a server.");
		}

		/// <summary>
		/// Creates a new role with the given ID in the given server.
		/// </summary>
		/// <param name="plRole"></param>
		/// <param name="server"></param>
		private Role(Payloads.PayloadObjects.Role plRole, Guild server) : base(plRole.ID) {
			Server = server;
			RoleRegistry[plRole.ID] = this;
			Update(plRole, false);
		}

		/// <summary>
		/// Creates a new role in the given server or gets an existing one. Returns <see langword="true"/> if the role had to be freshly created.
		/// </summary>
		/// <param name="plRole"></param>
		/// <param name="server"></param>
		/// <returns></returns>
		internal static Role GetOrCreate(Payloads.PayloadObjects.Role plRole, Guild server) {
			Role role = RoleRegistry.GetOrDefault(plRole.ID, new Role(plRole, server));
			return role;
		}

		internal Role(Snowflake id, Guild server) : base(id) {
			Server = server;
			RoleRegistry[id] = this;
		}

		/// <summary>
		/// Creates a new role in the given server or gets an existing one. Returns <see langword="true"/> if the role had to be freshly created.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="server"></param>
		/// <returns></returns>
		internal static async Task<Role> GetOrDownloadAsync(Snowflake id, Guild server) {
			bool hadToCreate = !RoleRegistry.ContainsKey(id);
			if (hadToCreate) {
				if (server.Roles.Count == 0) {
					await server.RedownloadAllRolesAsync();
					Role? role = server.Roles[id];
					if (role == null) throw new Exception($"Role ID {id} was not found in server {server.ID}!");
					return role;
				}
			}
			return RoleRegistry[id];
		}

		/// <inheritdoc/>
		protected internal override Task Update(PayloadDataObject obj, bool skipNonNullFields) {
			if (obj is Payloads.PayloadObjects.Role role) {
				Integrated = role.Managed;

				_Name = role.Name;
				_Mentionable = role.Mentionable;
				_Hoisted = role.Hoisted;
				_Position = role.Position;

				Permissions.DataReset();
				if (role.Permissions != Payloads.Data.Permissions.None) {
					//Permissions.SetAllowed(role.Permissions);
					Permissions.SetTo(role.Permissions, Payloads.Data.PermissionState.Allow, true);
				}
			}
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		protected override async Task<HttpResponseMessage?> SendChangesToDiscord(IReadOnlyDictionary<string, object> changes, string? reasons) {
			APIRequestData request = new APIRequestData {
				Params = { Server.ID, ID },
				Reason = reasons
			};
			if (changes.ContainsKey(nameof(Name))) request.SetJsonField("name", Name);
			if (changes.ContainsKey(nameof(Color))) request.SetJsonField("color", Color?.Value);
			if (changes.ContainsKey(nameof(Hoisted))) request.SetJsonField("hoist", Hoisted);
			if (changes.ContainsKey(nameof(Mentionable))) request.SetJsonField("mentionable", Mentionable);
			if (changes.ContainsKey(nameof(Permissions))) request.SetJsonField("permissions", ((int)Permissions.GetAllowed()).ToString());

			if (changes.ContainsKey(nameof(Position))) {
				await Guild.ModifyGuildRolePosition.ExecuteAsync(new APIRequestData { Params = { Server.ID }, Reason = reasons }.SetJsonField("id", ID).SetJsonField("position", Position));
			}

			return await Guild.ModifyGuildRole.ExecuteAsync(request);
		}

		/// <summary>
		/// Returns a sort order dependant on whether or not this role is higher in position than the given role. If used in <see cref="Array.Sort(Array)"/>, this will order the roles from lowest position first to highest position last.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo([AllowNull] Role other) {
			if (other is null) return 1;
			return Position - other.Position;
		}

		/// <summary>
		/// Returns whether or not the left-hand role is higher up than the right-hand role.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator >(Role? left, Role? right) {
			if (left is null || right is null) throw new NullReferenceException();
			return left.Position > right.Position;
		}

		/// <summary>
		/// Returns whether or not the left-hand role is lower down than the right-hand role.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator <(Role? left, Role? right) {
			if (left is null || right is null) throw new NullReferenceException();
			return left.Position > right.Position;
		}

		/// <summary>
		/// Returns whether or not the left-hand role is higher up or equal in position relative to the right-hand role.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator >=(Role? left, Role? right) {
			if (left is null || right is null) throw new NullReferenceException();
			return left.Position >= right.Position;
		}

		/// <summary>
		/// Returns whether or not the left-hand role is lower down or equal in position relative to the right-hand role.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator <=(Role? left, Role? right) {
			if (left is null || right is null) throw new NullReferenceException();
			return left.Position >= right.Position;
		}

		/// <inheritdoc/>
		public override DiscordObject MemberwiseClone() {
			Role r = (Role)base.MemberwiseClone();
			r._Permissions = Permissions.Clone();
			return r;
		}
	}
}
