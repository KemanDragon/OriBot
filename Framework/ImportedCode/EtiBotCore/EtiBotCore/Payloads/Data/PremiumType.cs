using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// The type of premium (nitro) on a User's account.
	/// </summary>
	public enum PremiumType {

		/// <summary>
		/// This user does not have a Nitro subscription.
		/// </summary>
		None = 0,

		/// <summary>
		/// The user has Nitro Classic.
		/// </summary>
		NitroClassic = 1,

		/// <summary>
		/// This user has full Nitro.
		/// </summary>
		Nitro = 2

	}
}
