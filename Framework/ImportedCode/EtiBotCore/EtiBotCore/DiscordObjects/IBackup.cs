using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Exceptions;

namespace EtiBotCore.DiscordObjects {

	/// <summary>
	/// Represents a <see cref="DiscordObject"/> that can have a backup made.
	/// </summary>
	public interface IBackup {

		/// <summary>
		/// Duplicates this <see cref="DiscordObject"/> into a new instance via a selective deep copy.
		/// </summary>
		/// <returns></returns>
		DiscordObject CreateBackup();

		/// <summary>
		/// Restores this <see cref="DiscordObject"/> to the state defined by <see cref="DiscordObject.Original"/>
		/// </summary>
		void Restore();
	}
}
