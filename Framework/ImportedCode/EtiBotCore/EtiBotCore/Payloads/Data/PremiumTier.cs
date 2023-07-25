using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// Nitro boost tiers for servers.
	/// </summary>
	public enum PremiumTier {

		/// <summary>
		/// This server is not boosted.
		/// </summary>
		None = 0,

		/// <summary>
		/// This server has achieved tier 1 rewards.
		/// </summary>
		Tier1 = 1,

		/// <summary>
		/// This server has achieved tier 2 rewards.
		/// </summary>
		Tier2 = 2,

		/// <summary>
		/// This server has achieved tier 3 rewards.
		/// </summary>
		Tier3 = 3

	}
}
