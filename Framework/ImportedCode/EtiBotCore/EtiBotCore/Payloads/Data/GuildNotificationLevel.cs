using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// What messages warrant sending a notification to members.
	/// </summary>
	public enum GuildNotificationLevel {

		/// <summary>
		/// All sent messages will result in a notification.
		/// </summary>
		AllMessages = 0,

		/// <summary>
		/// Only when a user is pinged will they have a notification.
		/// </summary>
		OnlyMentions = 1

	}
}
