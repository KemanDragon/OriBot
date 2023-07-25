using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Utility.Extension;
using EtiLogger.Logging;
using EtiLogger.Logging.Util;

namespace Test_Implementation.OutputArea {

	/// <summary>
	/// Writes log entries to the server.
	/// </summary>
	public class ServerRelay : OutputRelay {

		public TextChannel Channel { get; set; }

		public override void OnLogWritten(LogMessage message, LogLevel messageLevel, bool shouldWrite, Logger source) {
			ConsoleRelay.OnLogWritten(message, messageLevel, shouldWrite, source);
			if (!shouldWrite) {
				return;
			}

			if (Channel != null) {
				string text = message.ToString();
				_ = Channel.SendMessageAsync(text, mentionLimits: AllowedMentions.AllowNothing);
			}
		}

		public override void OnBeep(bool shouldBeep, Logger source) {
			
		}
	}
}
