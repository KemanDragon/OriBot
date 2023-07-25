using EtiBotCore.Data.JsonConversion;
using EtiBotCore.Payloads.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// Represents a permission setting that overrides role permissions on a channel.
	/// </summary>
	internal class PermissionOverwrite {

		/// <summary>
		/// The role or user ID that this overwrite applies to.
		/// </summary>
		[JsonProperty("id")]
		public ulong ID { get; set; }

		/// <summary>
		/// The thing that this permission overwrite applies to.
		/// </summary>
		[JsonProperty("type"), JsonConverter(typeof(EnumConverter))]
		public OverwriteTarget Type { get; set; }

		/// <summary>
		/// The numeric flags for the permissions that are explicitly allowed.
		/// </summary>
		[JsonProperty("allow")]
		protected string Allow { get; set; } = "0";

		/// <summary>
		/// The numeric flags for the permissions that are explicitly denied.
		/// </summary>
		[JsonProperty("deny")]
		protected string Deny { get; set; } = "0";

		/// <inheritdoc cref="Allow"/>
		[JsonIgnore]
		public Permissions AllowPermissions {
			get {
				return (Permissions)ulong.Parse(Allow);
			}
			set {
				Allow = ((ulong)value).ToString();
			}
		}

		/// <inheritdoc cref="Deny"/>
		[JsonIgnore]
		public Permissions DenyPermissions {
			get {
				return (Permissions)ulong.Parse(Deny);
			}
			set {
				Deny = ((ulong)value).ToString();
			}
		}

		/// <summary>
		/// Describes what this overwrite applies to.
		/// </summary>
		public enum OverwriteTarget {

			/// <summary>
			/// This overwrite applies to a role.
			/// </summary>
			Role = 0,

			/// <summary>
			/// This overwrite applies to a member.
			/// </summary>
			Member = 1

		}

	}
}
