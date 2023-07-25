using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;

namespace OldOriBot.Utility.Formatting {

	/// <summary>
	/// For making pseudo-progress-bars in text chat.
	/// </summary>
	public class ChattyProgressBar {

		public const char BLOCK_EMPTY = ' ';
		public const char BLOCK_HALF = '▌';
		public const char BLOCK_FULL = '█';
		public const char TRI = '►';
		public int Width { get; }

		public bool ShowPercentage { get; set; } = true;

		public ChattyProgressBar(int width = 20) {
			Width = width;
		}
		
		/// <summary>
		/// Initial draw call for the message.
		/// </summary>
		/// <param name="percent"></param>
		/// <returns></returns>
		public Task<Message> Draw(TextChannel channel, double percent) {
			return channel.SendMessageAsync(GetProgressBarText(percent), null, AllowedMentions.AllowNothing);
		}

		/// <summary>
		/// Updates an existing message to display a progress bar.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="percent"></param>
		/// <returns></returns>
		public Task Update(Message message, double percent) {
			message.BeginChanges();
			message.Content = GetProgressBarText(percent);
			return message.ApplyChanges("Progress bar update.");
		}

		public string GetProgressBarText(double percent) {

			string percentage;
			if (ShowPercentage) {
				percentage = Math.Round(percent * 100).ToString();
				while (percentage.Length < 3) {
					percentage = " " + percentage;
				}
				percentage += "% ";
			} else {
				percentage = TRI + " ";
			}
			int numWritten = 0;
			double numBlocks = Width * percent;
			int numWholeBlocks = (int)Math.Floor(numBlocks);
			for (int i = 0; i < numWholeBlocks; i++) {
				percentage += BLOCK_FULL;
				numBlocks--;
				numWritten++;
			}
			if (numBlocks >= 0.5) {
				// Still a half left over
				percentage += BLOCK_HALF;
				numWritten++;
			}
			for (int i = 0; i < Width - numWritten; i++) {
				percentage += BLOCK_EMPTY;
			}
			return "`" + percentage + "`";
		}

	}
}
