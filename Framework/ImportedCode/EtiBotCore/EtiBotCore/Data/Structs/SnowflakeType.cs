using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Data.Structs {

	/// <summary>
	/// A type of object that a <see cref="Snowflake"/> represents.
	/// </summary>
	public enum SnowflakeType {

		/// <summary>
		/// This <see cref="Snowflake"/> could be anything, as it was given as a raw ID.
		/// </summary>
		Ambiguous = 0,

		/// <summary>
		/// This <see cref="Snowflake"/> is a user's ID.
		/// </summary>
		User = 1,

		/// <summary>
		/// This <see cref="Snowflake"/> is a role's ID.
		/// </summary>
		Role = 2,

		/// <summary>
		/// This <see cref="Snowflake"/> is a server's ID.
		/// </summary>
		Guild = 3,

		/// <summary>
		/// This <see cref="Snowflake"/> is a channel's ID.
		/// </summary>
		Channel = 4

	}
}
