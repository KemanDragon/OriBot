using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// The behavior of the explicit content filter.
	/// </summary>
	public enum ExplicitContentFilterLevel {

		/// <summary>
		/// The explicit content filter is disabled.
		/// </summary>
		Disabled = 0,

		/// <summary>
		/// Only members without roles will be subjected to the content filter.
		/// </summary>
		MembersWithoutRoles = 1,

		/// <summary>
		/// All members are subject to the content filter.
		/// </summary>
		AllMembers = 2

	}
}
