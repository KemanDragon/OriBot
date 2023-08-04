using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// A type of permission overwrite target in a channel's permission settings, either role or user.
	/// </summary>
	public enum PermissionOverwriteTargetType {

		/// <summary>
		/// This permission overwrite targets a role.
		/// </summary>
		Role = 0,

		/// <summary>
		/// This permission overwrite targets a user.
		/// </summary>
		User = 1

	}
}
