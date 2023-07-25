using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// Represents a role.
	/// </summary>
	internal class Role : PayloadDataObject {

		/// <summary>
		/// The ID of this role.
		/// </summary>
		[JsonProperty("id")]
		public ulong ID { get; set; }

		/// <summary>
		/// The name of this role.
		/// </summary>
		[JsonProperty("name"), JsonRequired]
		public string Name { get; set; } = "new role";

		/// <summary>
		/// The color of this role stored in an integer as <c>0RGB</c>
		/// </summary>
		[JsonProperty("color")]
		public int? Color { get; set; }

		/// <summary>
		/// Whether or not this role is displayed uniquely in the member list.
		/// </summary>
		[JsonProperty("hoist")]
		public bool Hoisted { get; set; }

		/// <summary>
		/// The position of this role in the list.
		/// </summary>
		[JsonProperty("position")]
		public int Position { get; set; }

		/// <summary>
		/// The permissions of this role that are specifically set to allow.
		/// </summary>
		[JsonProperty("permissions")]
		protected string PermissionsString { get; set; } = "0";

		/// <inheritdoc cref="PermissionsString"/>
		[JsonIgnore]
		public Permissions Permissions {
			get => (Permissions)ulong.Parse(PermissionsString);
			set => PermissionsString = ((ulong)value).ToString();
		}

		/// <summary>
		/// Whether or not the role is managed by an integration.
		/// </summary>
		[JsonProperty("managed")]
		public bool Managed { get; set; }

		/// <summary>
		/// Whether or not the role can be pinged.
		/// </summary>
		[JsonProperty("mentionable")]
		public bool Mentionable { get; set; }

		public Role() { }

		public Role(string strId) {
			ID = Snowflake.Parse(strId);
		}

	}
}
