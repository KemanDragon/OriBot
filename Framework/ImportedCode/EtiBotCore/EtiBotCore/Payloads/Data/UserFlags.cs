using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// Flags associated with a Discord user.
	/// </summary>
	[Flags]
	public enum UserFlags {
		/// <summary>
		/// This user has no flags.
		/// </summary>
		None = 0,

		/// <summary>
		/// This user is an employee of Discord.
		/// </summary>
		DiscordEmployee = 1 << 0,

		/// <summary>
		/// This user owns a partnered server.
		/// </summary>
		PartneredServerOwner = 1 << 1,

		/// <summary>
		/// This user was present at a HypeSquad event.
		/// </summary>
		HypeSquadEvents = 1 << 2,

		/// <summary>
		/// This user is a Level 1 Bug Hunter
		/// </summary>
		BugHunterL1 = 1 << 3,

		/// <summary>
		/// This user is in the Bravery House of HypeSquad.
		/// </summary>
		HypesquadHouseBravery = 1 << 6,

		/// <summary>
		/// This user is in the Brilliance House of HypeSquad.
		/// </summary>
		HypesquadHouseBrilliance = 1 << 7,

		/// <summary>
		/// This user is in the Balance House of HypeSquad.
		/// </summary>
		HypesquadHouseBalance = 1 << 8,

		/// <summary>
		/// This user purchased Discord Nitro when it first launched and had only one available tier for $5.
		/// </summary>
		EarlySupporter = 1 << 9,

		/// <summary>
		/// This is a developer team, which is registered as a type of user.
		/// </summary>
		TeamUser = 1 << 10,

		/// <summary>
		/// This is Discord's system.
		/// </summary>
		System = 1 << 12,

		/// <summary>
		/// This user is a Level 2 Bug Hunter
		/// </summary>
		BugHunterL2 = 1 << 14,

		/// <summary>
		/// This user is a bot, and they are a verified bot too.
		/// </summary>
		VerifiedBot = 1 << 16,

		/// <summary>
		/// This user was one of the people who were among the first to register a verified bot.
		/// </summary>
		EarlyVerifiedBotDev = 1 << 17
	}
}
