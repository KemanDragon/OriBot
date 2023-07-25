using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiLogger.Logging;
using OldOriBot.Data;

namespace OldOriBot.Interaction {

	/// <summary>
	/// Very similar to a command, with the exception that they run for every message that has been sent.
	/// </summary>
	public abstract class PassiveHandler {

		/// <summary>
		/// A <see cref="Task{TResult}"/> that returns <see langword="false"/>, intended for use in handlers that do not do anything to messages.
		/// </summary>
		public static readonly Task<bool> HandlerDidNothingTask = Task.FromResult(false);

		/// <summary>
		/// The name of this passive handler.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// The description of this passive handler.
		/// </summary>
		public abstract string Description { get; }

		/// <summary>
		/// If <see langword="true"/>, this handler will run before commands run, and it won't care if the message is a command or not.
		/// </summary>
		public virtual bool RunOnCommands { get; }

		/// <summary>
		/// The <see cref="BotContext"/> this handler exists for.
		/// </summary>
		public BotContext Context { get; }

		/// <summary>
		/// A <see cref="Logger"/> just for this <see cref="PassiveHandler"/>
		/// </summary>
		public Logger HandlerLogger {
			get {
				if (_HandlerLogger == null) {
					_HandlerLogger = new Logger($"[Handler: {Name}] ");
				}
				return _HandlerLogger;
			}
		}
		private Logger _HandlerLogger = null;

		/// <summary>
		/// Construct a new <see cref="PassiveHandler"/> in the given context.
		/// </summary>
		/// <param name="ctx"></param>
		public PassiveHandler(BotContext ctx) {
			Context = ctx;
		}
		
		/// <summary>
		/// Run this <see cref="PassiveHandler"/> on the given message.
		/// </summary>
		/// <param name="executor">The member that triggered this handler.</param>
		/// <param name="executionContext">The <see cref="BotContext"/> this handler is running in.</param>
		/// <param name="message">The <see cref="Message"/> this handler is running on.</param>
		/// <returns><see langword="true"/> if the message was intercepted and no other code should handle it, <see langword="false"/> if not.</returns>
		public abstract Task<bool> ExecuteHandlerAsync(Member executor, BotContext executionContext, Message message);

	}
}
