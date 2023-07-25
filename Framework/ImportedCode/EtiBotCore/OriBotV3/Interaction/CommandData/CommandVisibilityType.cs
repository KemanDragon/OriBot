using System;
using System.Collections.Generic;
using System.Text;

namespace OldOriBot.Interaction.CommandData {

	/// <summary>
	/// Represents the manner in which a command is (or is not) visible in the help menu.
	/// </summary>
	public enum CommandVisibilityType {

		/// <summary>
		/// This command is always visible in the help menu.
		/// </summary>
		Visible = 0,

		/// <summary>
		/// This command is only visible in the help menu if the user running the help command can actually use it.
		/// </summary>
		OnlyIfUsable = 1,

		/// <summary>
		/// This command will never be shown in the help menu.
		/// </summary>
		Never = 2

	}
}
