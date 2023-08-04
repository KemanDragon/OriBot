using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiLogger.Data.Structs;
using EtiLogger.Logging;
using OldOriBot.Data.MemberInformation;
using OldOriBot.Interaction.CommandData;
using OldOriBot.PermissionData;
using OldOriBot.Utility.Arguments;

namespace OldOriBot.Interaction {

	/// <summary>
	/// A command that is run across several chat messages.
	/// </summary>
	public abstract class ProgressiveCommand : Command, ILoggable {

		/// <summary>
		/// The prompts that show with each step.
		/// </summary>
		public abstract string[] Prompts { get; }

		/// <summary>
		/// An <see cref="ArgumentMapProvider"/> that contains the arguments this command can take. This is used to display its syntax in the help menu as well.<para/>
		/// Note: Subcommands should not be listed as arguments. Use <see cref="Subcommands"/> for this purpose instead.
		/// </summary>
		public abstract ArgumentMapProvider[] Syntaxes { get; }

		/// <summary>
		/// Throws <see cref="NotImplementedException"/> as <see cref="ProgressiveCommand"/>s do not have a singular syntax.
		/// </summary>
		public override ArgumentMapProvider Syntax => throw new NotImplementedException("Progressive Commands do not have a singular syntax.");

		/// <summary>
		/// Throws <see cref="NotImplementedException"/> as progressive commands cannot have subcommands.
		/// </summary>
		public override bool IsExclusiveBase => throw new NotImplementedException("Progressive Commands do not have subcommands.");

		/// <summary>
		/// Throws <see cref="NotImplementedException"/> as progressive commands cannot have subcommands.
		/// </summary>
		public override Command[] Subcommands => throw new NotImplementedException("Progressive Commands do not have subcommands.");

		/// <summary>
		/// A <see cref="Logger"/> just for this <see cref="Command"/>
		/// </summary>
		public override Logger CommandLogger {
			get {
				if (_CommandLogger == null) {
					_CommandLogger = new Logger($"[Progressive Command: {Name}] ");
				}
				return _CommandLogger;
			}
		}
		private Logger _CommandLogger = null;

		/// <summary>
		/// Returns <see langword="true"/> if this command overrides <see cref="CanRunCommand"/>, which may alter how it detects whether or not it can be used.
		/// </summary>
		public override bool HasCustomUsageBehavior {
			get {
				if (!TestedForCustomUsage) {
					_HasCustomUsageBehavior = GetType().GetMethod("CanRunCommand").DeclaringType != typeof(ProgressiveCommand);
					TestedForCustomUsage = true;
				}
				return _HasCustomUsageBehavior;
			}
		}
		private bool _HasCustomUsageBehavior = false;
		private bool TestedForCustomUsage = false;

		protected ProgressiveCommand(BotContext ctx) : base(ctx) { }

		/// <summary>
		/// Starts the execution of this <see cref="ProgressiveCommand"/>, returning the <see cref="Tracker"/> that can be used to guide the user through the command.
		/// </summary>
		/// <remarks>
		/// This method should return a custom implementation of Tracker made specifically for the command implementation. It is a good idea in general to have the command simply prepare a tracker rather than require arguments.
		/// </remarks>
		/// <param name="executor"></param>
		/// <param name="executionContext"></param>
		/// <param name="originalMessage"></param>
		/// <returns></returns>
		public abstract Task<Tracker> BeginExecutionAsync(Member executor, BotContext executionContext, Message originalMessage);

		public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			throw new NotImplementedException("Use BeginExecutionAsync on ProgressiveCommand");
		}

		public override LogMessage ToMessage() {
			LogMessage message = new LogMessage();
			message.AddComponent("Progressive Command: ", Color.WHITE, null, true);
			message.AddComponent(Name, Color.DARK_GREEN);
			return message;
		}

		/// <summary>
		/// Tracks a user's process on progressive commands.
		/// </summary>
		public abstract class Tracker {

			public const string RED_X_EMOJI = "❌";
			public const string RED_X = ":x:";

			/// <summary>
			/// The current step the user is on. These are 0-indexed (Step=0 is the first step).
			/// </summary>
			public int Step { get; private set; } = 0;

			/// <summary>
			/// The channel this was initialized in.
			/// </summary>
			public ChannelBase Channel { get; }

			/// <summary>
			/// The <see cref="ProgressiveCommand"/> that created this <see cref="Tracker"/>.
			/// </summary>
			public ProgressiveCommand Source { get; }

			/// <summary>
			/// Whether or not this tracker has been terminated.
			/// </summary>
			public bool Terminated { get; private set; }

			/// <summary>
			/// Creates a new tracker that exists for the given <see cref="ProgressiveCommand"/> and allows things to be executed in the given <see cref="ChannelBase"/>
			/// </summary>
			/// <param name="source"></param>
			/// <param name="channel"></param>
			public Tracker(ProgressiveCommand source, ChannelBase channel) {
				Channel = channel;
				Source = source;
			}

			/// <summary>
			/// This callback is executed when the input of the last chat message was malformed, and the user needs to try again.
			/// </summary>
			/// <param name="message"></param>
			/// <returns></returns>
			protected abstract Task ReshowCurrentStep(Message latestResponse);

			/// <summary>
			/// This callback is executed when the current step is successfully executed and the next step is ready to run.
			/// </summary>
			/// <returns></returns>
			protected abstract Task DisplayNextStep(Message latestResponse);

			/// <summary>
			/// This callback is executed when the progressive command is terminated. It should not be called manually.
			/// </summary>
			protected abstract Task Terminate(Message latestResponse);

			/// <summary>
			/// An internal termination method that sets <see cref="Terminated"/> to true and then calls <see cref="Terminate(Message)"/>
			/// </summary>
			/// <param name="latestResponse"></param>
			/// <returns></returns>
			private Task TerminateInternal(Message latestResponse) {
				Terminated = true;
				return Terminate(latestResponse);
			}

			/// <summary>
			/// This callback is executed when the current step is being attempted. Returns what should happen once execution completes.
			/// </summary>
			/// <returns></returns>
			protected abstract Task<TrackerActionResult> TryExecuteCurrentStep(Message latestResponse);

			/// <summary>
			/// Called from <see cref="CommandMarshaller"/>, this is run when a message is sent that might work for this progressive command.
			/// </summary>
			/// <param name="latestResponse"></param>
			/// <returns></returns>
			public async Task ExecuteCurrentTask(Message latestResponse) {
				string info = latestResponse.Content;
				if (info == RED_X || info == RED_X_EMOJI) {
					await TerminateInternal(latestResponse);
				}
				TrackerActionResult action = await TryExecuteCurrentStep(latestResponse);
				if (action == TrackerActionResult.Continue) {
					Step++;
					await DisplayNextStep(latestResponse);
				} else if (action == TrackerActionResult.ErrorAndRetry) {
					await ReshowCurrentStep(latestResponse);
				} else if (action == TrackerActionResult.Terminate) {
					await TerminateInternal(latestResponse);
				}
			}

			/// <summary>
			/// Describes what should happen after the current step executes.
			/// </summary>
			public enum TrackerActionResult {
				/// <summary>
				/// The tracker should send an error message and retry the current step (<see cref="ReshowCurrentStep(Message)"/> will be fired)
				/// </summary>
				ErrorAndRetry = 0,

				/// <summary>
				/// The tracker should terminate, execution is complete.
				/// </summary>
				Terminate = 1,

				/// <summary>
				/// The tracker should move on to the next step. This will cause the internal <see cref="Step"/> value to increment.
				/// </summary>
				Continue = 2
			}
		}
	}
}
