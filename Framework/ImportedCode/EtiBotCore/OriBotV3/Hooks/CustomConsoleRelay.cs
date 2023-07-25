using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OriBotV3;
using EtiLogger.Data.Structs;
using EtiLogger.Logging;
using EtiLogger.Logging.Util;
using System.Threading;
using System.IO;

namespace CustomConsole.Hooks {
	class CustomConsoleRelay : OutputRelay {

		public FileInfo CurrentLogFile = new FileInfo(@$".\output-log-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.log");

		public RichTextBox Target { get; }

		public BotWindow Window { get; }

		public SynchronizationContext Context { get; }

		public CustomConsoleRelay(RichTextBox rtb, BotWindow iface) {
			Target = rtb;
			Window = iface;
			Context = SynchronizationContext.Current;
		}

		private System.Drawing.Font GetFontFrom(System.Drawing.Font input, bool? bold = null, bool? italics = null, bool? underline = null, bool? strike = null) {
			string face = input.Name;
			bool makeBold = bold.GetValueOrDefault(input.Bold);
			bool makeItalics = italics.GetValueOrDefault(input.Italic);
			bool makeUnderline = underline.GetValueOrDefault(input.Underline);
			bool makeStrike = strike.GetValueOrDefault(input.Strikeout);

			System.Drawing.FontStyle style = System.Drawing.FontStyle.Regular;
			if (makeBold) style |= System.Drawing.FontStyle.Bold;
			if (makeItalics) style |= System.Drawing.FontStyle.Italic;
			if (makeUnderline) style |= System.Drawing.FontStyle.Underline;
			if (makeStrike) style |= System.Drawing.FontStyle.Strikeout;
			return new System.Drawing.Font(face, input.Size, style);
		}

		private void OnLogWrittenMain(object state) {
			try {
				(LogMessage message, LogLevel messageLevel, bool shouldWrite, Logger source) = (ValueTuple<LogMessage, LogLevel, bool, Logger>)state;

				using StreamWriter writer = CurrentLogFile.AppendText();
				writer.Write(message.ToString());
				writer.Flush();
				writer.Close();

				if (!shouldWrite) return;

				int orgStart = Target.SelectionStart;
				int orgLen = Target.SelectionLength;
				if (Target.TextLength > int.MaxValue - 10000) {
					Target.Clear();
					orgStart = 0;
					orgLen = 0;
				}

				Target.SelectionProtected = true;
				foreach (var cmp in message.Components) {
					Target.SelectionStart = Target.TextLength;
					Target.SelectionLength = 0;
					if (cmp.Color != null) Target.SelectionColor = cmp.Color.Value.ToSystemColor();
					if (cmp.BackgroundColor != null) Target.SelectionBackColor = cmp.BackgroundColor.Value.ToSystemColor();
					Target.SelectionFont = GetFontFrom(Target.Font, cmp.Bold, cmp.Italics, cmp.Underline, cmp.Strike);

					Target.AppendText(cmp.Text);
				}
				Target.SelectionFont = GetFontFrom(Target.Font);
				if (orgLen > 0) {
					Target.SelectionStart = orgStart;
					Target.SelectionLength = orgLen;
				}
				Target.SelectionProtected = false;
			} catch (Exception exc) {
				MessageBox.Show(exc.Message + "\n\n" + exc.StackTrace);
			}
		}

		public override void OnLogWritten(LogMessage message, LogLevel messageLevel, bool shouldWrite, Logger source) {
			if (SynchronizationContext.Current != Context) {
				Context.Post(OnLogWrittenMain, (message, messageLevel, shouldWrite, source));
			} else {
				OnLogWrittenMain((message, messageLevel, shouldWrite, source));
			}
		}

		public override void OnBeep(bool shouldBeep, Logger source) {
			if (!shouldBeep) return;
			Window.SoundPlayer.Play();
		}
	}
}
