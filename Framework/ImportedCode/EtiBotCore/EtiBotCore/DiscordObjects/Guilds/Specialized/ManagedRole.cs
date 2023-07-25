using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.Exceptions.Marshalling;
using EtiLogger.Data.Structs;

namespace EtiBotCore.DiscordObjects.Guilds.Specialized {

	/// <summary>
	/// A system that allows the existence of a role to be enforced.
	/// </summary>
	public class ManagedRole : IEquatable<ManagedRole>, IEquatable<Role> {

		/// <summary>
		/// Whether or not this <see cref="ManagedRole"/> has been initialized.
		/// </summary>
		public bool IsInitialized => Role != null;

		/// <summary>
		/// The name used when finding this role on startup, or when creating the role.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// The color of this role if it needs to be created, or <see langword="null"/> to use no color.
		/// </summary>
		public Color? Color { get; }

		/// <summary>
		/// The server this role is a part of.
		/// </summary>
		public Guild Server { get; }

		/// <summary>
		/// The role that this represents. This will be <see langword="null"/> if <see cref="IsInitialized"/> is <see langword="false"/>.
		/// </summary>
		public Role? Role { get; private set; }

		/// <summary>
		/// Create a new <see cref="ManagedRole"/> with the given name and default color.
		/// </summary>
		/// <param name="inServer">The server this role is a part of.</param>
		/// <param name="name">The name of this role to search for, or to use when creating.</param>
		/// <param name="color">The color of this role, or <see langword="null"/> for no color.</param>
		public ManagedRole(Guild inServer, string name, Color? color = null) {
			Server = inServer;
			Name = name;
			Color = color;
		}

		/// <summary>
		/// Searches for the role in the server, or creates a new one. Also connects the event handlers.
		/// </summary>
		/// <returns></returns>
		public async Task Initialize(int msToDelayAfterCreation = 0) {
			DiscordClient.Current!.Events.GuildEvents.OnRoleDeleted += OnRoleDeleted;
			DiscordClient.Current!.Events.GuildEvents.OnRoleUpdated += OnRoleUpdated;

			foreach (Role r in Server.Roles) {
				if (r.Name == Name) {
					Role = r;
					return;
				}
			}

			await CreateThisRoleAsync();
			if (msToDelayAfterCreation > 0) {
				await Task.Delay(msToDelayAfterCreation);
			}
		}

		private async Task OnRoleUpdated(Guild guild, Role roleBefore, Role role) {
			if (guild != Server) return;
			if (role != Role) return;
			if (role.Name != Name) {
				role.BeginChanges();
				role.Name = Name;
				await role.ApplyChanges($"This is a managed role. Its name MUST be {Name} in order for the bot to recognize it. If you wish to change its name, ping Eti.");
			}
		}

		private async Task OnRoleDeleted(Guild guild, Role role, Snowflake roleID) {
			if (roleID == Role!.ID && guild == Server)
				await CreateThisRoleAsync();

		}

		private async Task CreateThisRoleAsync() {
			Role = await Server.CreateNewRoleAsync(Name, null, Color?.Value, reason: $"This is a managed role. A role named {Name} MUST exist, and as such, it has been deleted after manual recreation. If you wish to remove it, ping Eti.");
		}

		#region Equality

		/// <summary>
		/// Compares <see cref="Name"/> and <see cref="Server"/> to that of the <paramref name="other"/> <see cref="ManagedRole"/>.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals([AllowNull] ManagedRole other) {
			if (ReferenceEquals(this, other)) return true;
			if (other is null) return false;
			return Name.Equals(other.Name) && Server == other.Server;
		}

		/// <summary>
		/// Compares this <see cref="Role"/> to the given <see cref="Guilds.Role"/>. Note that this will throw <see cref="InvalidOperationException"/> if this <see cref="ManagedRole"/> is not initialized (<see cref="Role"/> is <see langword="null"/>)
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">If <see cref="Role"/> is <see langword="null"/>.</exception>
		public bool Equals([AllowNull] Role other) {
			if (ReferenceEquals(this, other)) return true;
			if (Role is null) throw new InvalidOperationException("Cannot compare ManagedRole.Equals(Role) if the ManagedRole has not been initialized.");
			if (other is null) return false;
			return Role == other;
		}

		/// <summary>
		/// Compares this <see cref="ManagedRole"/> to the given object. Will always return <see langword="false"/> for anything other than another <see cref="ManagedRole"/> or a <see cref="Guilds.Role"/>.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object? obj) {
			if (ReferenceEquals(this, obj)) return true;
			if (obj is ManagedRole mg) return Equals(mg);
			if (obj is Role role) return Equals(role);
			return false;
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return HashCode.Combine(Name, Server);
		}

		/// <summary>
		/// Compares the name and server of the two <see cref="ManagedRole"/> instances.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(ManagedRole left, ManagedRole right) {
			if (left is null && right is null) return true;
			if (left is null || right is null) return false;
			return left.Equals(right);
		}

		/// <inheritdoc cref="operator ==(ManagedRole, ManagedRole)"/>
		public static bool operator !=(ManagedRole left, ManagedRole right) => !(left == right);

		/// <summary>
		/// Compares the given role to the given <see cref="Role"/>.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(Role left, ManagedRole right) {
			if (left is null && right is null) return true;
			if (left is null || right is null) return false;
			return right.Equals(left);
		}

		/// <inheritdoc cref="operator ==(Role, ManagedRole)"/>
		public static bool operator !=(Role left, ManagedRole right) => !(left == right);

		/// <inheritdoc cref="operator ==(Role, ManagedRole)"/>
		public static bool operator ==(ManagedRole left, Role right) => right == left;

		/// <inheritdoc cref="operator ==(Role, ManagedRole)"/>
		public static bool operator !=(ManagedRole left, Role right) => !(left == right);

		#endregion
	}
}
