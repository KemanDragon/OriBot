using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiLogger.Data.Structs;
using EtiLogger.Logging;
using OldOriBot.Data;
using OldOriBot.Data.MemberInformation;
using OldOriBot.Interaction.CommandData;
using OldOriBot.PermissionData;
using OldOriBot.Utility.Arguments;

namespace OldOriBot.Interaction {

	/// <summary>
	/// Represents an executable command. It is advised to add subcommands as inner classes to their parent command. All commands should directly inherit from this class, even subcommands.
	/// </summary>
	public abstract class Command : ILoggable {

		/// <summary>
		/// The name of this command. Should be all lowercase and not contain spaces.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Alternative names for this command. Should be all lowercase and not contain spaces.
		/// </summary>
		public virtual string[] Aliases { get; } = new string[0];

		/// <summary>
		/// A description of what this command does.
		/// </summary>
		public abstract string Description { get; }

		/// <summary>
		/// An <see cref="ArgumentMapProvider"/> that contains the arguments this command can take. This is used to display its syntax in the help menu as well.<para/>
		/// Note: Subcommands should not be listed as arguments. Use <see cref="Subcommands"/> for this purpose instead.
		/// </summary>
		public abstract ArgumentMapProvider Syntax { get; }

		/// <summary>
		/// Subcommands to this command, which are indexed via their name.
		/// </summary>
		public virtual Command[] Subcommands { get; }

		/// <summary>
		/// Determines when this command is displayed in the help menu.
		/// </summary>
		public virtual CommandVisibilityType Visibility { get; } = CommandVisibilityType.OnlyIfUsable;

		/// <summary>
		/// The permission level required to use this command.
		/// </summary>
		public virtual PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.StandardUser;

		/// <summary>
		/// If <see langword="true"/>, this command cannot be run in the console even if a context is set.
		/// </summary>
		public virtual bool NoConsole { get; } = false;

		/// <summary>
		/// If <see langword="true"/>, this command requires a <see cref="BotContext"/> to run. This is only useful when a command is runnable by the console, so if <see cref="NoConsole"/> is <see langword="true"/>, this is useless.
		/// </summary>
		public virtual bool RequiresContext { get; } = false;

		/// <summary>
		/// If <see langword="true"/>, this command can only be executed in a DM.
		/// </summary>
		public virtual bool IsDMOnly { get; } = false;

		/// <summary>
		/// The <see cref="BotContext"/> this command was instantiated for.<para/>
		/// This will be <see langword="null"/> on commands created globally.<para/>
		/// <strong>Generally speaking, the <c>executionContext</c> parameter of <see cref="ExecuteCommandAsync(Member, BotContext, Message, string[], string, bool)"/> should be used whenever possible.</strong>
		/// </summary>
		public BotContext Context { get; }

		/// <summary>
		/// If this is a subcommand, then this is a reference to the parent <see cref="Command"/> containing it.
		/// </summary>
		public Command Parent { get; }

		/// <summary>
		/// Whether or not this <see cref="Command"/> is exclusively a base command, or, it has no functionality itself and instead is a container for subcommands.<para/>
		/// If this is <see langword="true"/>, then <see cref="ExecuteCommandAsync(Member, BotContext, Message, string[], string, bool)"/> will never be called, and if this command is run verbatim, the help menu entry will be posted on it instead.
		/// </summary>
		public virtual bool IsExclusiveBase { get; }

		/// <summary>
		/// Given a context and channel ID, this returns the ID of a channel where this command should be run. If <paramref name="channelUsedIn"/> is returned, the command can be used.<para/>
		/// For <see cref="PermissionLevel.Operator"/> and above, this will always return the input <paramref name="channelUsedIn"/> because they can use commands anywhere.
		/// </summary>
		/// <param name="executionContext"></param>
		/// <param name="channelUsedIn"></param>
		/// <returns></returns>
		public virtual Snowflake? GetUseInChannel(BotContext executionContext, Member member, Snowflake? channelUsedIn) {
			if (member.GetPermissionLevel() >= PermissionLevel.Operator) return channelUsedIn;
			if (executionContext.BotChannelID != null && executionContext.OnlyAllowCommandsInBotChannel) {
				return executionContext.BotChannelID.Value;
			}
			return channelUsedIn;
		}

		/// <summary>
		/// A <see cref="Logger"/> just for this <see cref="Command"/>
		/// </summary>
		public virtual Logger CommandLogger {
			get {
				if (_CommandLogger == null) {
					_CommandLogger = new Logger($"[Command: {FullName}] ");
				}
				return _CommandLogger;
			}
		}
		private Logger _CommandLogger = null;

		/// <summary>
		/// The full name of this subcommand following its whole hierarchy.
		/// </summary>
		public string FullName {
			get {
				if (_FullName == null) {
					if (Parent != null) {
						_FullName = Parent.FullName + " " + Name;
					} else {
						_FullName = Name;
					}
				}
				return _FullName;
			}
		}
		private string _FullName = null;

		/// <summary>
		/// Returns <see langword="true"/> if this command overrides <see cref="CanRunCommand"/>, which may alter how it detects whether or not it can be used.
		/// </summary>
		public virtual bool HasCustomUsageBehavior {
			get {
				if (!TestedForCustomUsage) {
					_HasCustomUsageBehavior = GetType().GetMethod("CanRunCommand").DeclaringType != typeof(Command);
					TestedForCustomUsage = true;
				}
				return _HasCustomUsageBehavior;
			}
		}
		private bool _HasCustomUsageBehavior = false;
		private bool TestedForCustomUsage = false;

		public Command(BotContext container) : this(container, null) { }

		public Command(BotContext container, Command parent) {
			Context = container;
			Parent = parent;
			VerifyName();
		}

		/// <summary>
		/// Verifies the <see cref="Name"/> is correct, and throws <see cref="FormatException"/> if it is not.
		/// </summary>
		private void VerifyName() {
			Match match = Regex.Match(Name, "([a-z]+)");
			if (!match.Success || match.Value != Name) {
				throw new FormatException($"Invalid command name {Name} (of {GetType().FullName}) -- Expected range of a-z.");
			}
		}

		/// <summary>
		/// Determines whether or not the given member can run this command.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public virtual CommandUsagePacket CanRunCommand(Member member) {
			if (member.GetPermissionLevel() >= RequiredPermissionLevel) return CommandUsagePacket.Success;
			return CommandUsagePacket.ForInsufficientPermissions(this, member);
		}

		/// <summary>
		/// If <see langword="true"/>, the help menu will display help for this command even if they cannot use it.
		/// </summary>
		public virtual bool CanSeeHelpForAnyway { get; } = false;

		/// <summary>
		/// Returns whether or not the input string command name refers to this command.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public virtual bool NameRunsCommand(string name) {
			name = name.ToLower();
			if (name == Name.ToLower()) {
				return true;
			}
			foreach (string alt in Aliases) {
				if (name == alt.ToLower()) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Execute this command.
		/// </summary>
		/// <param name="executor">The <see cref="Member"/> executing this command. This will be the bot's member in the <paramref name="executionContext"/> if the command was executed by the console and the context was set!</param>
		/// <param name="executionContext">The <see cref="BotContext"/> this command exists in. This will be <see langword="null"/> if the command was executed by the console!</param>
		/// <param name="originalMessage">The message this command was run by. This will be <see langword="null"/> if the command was executed by the console!</param>
		/// <param name="argArray">The array of arguments as split by shell32.dll</param>
		/// <param name="rawArgs">The raw arguments of this command.</param>
		/// <param name="isConsole"><see langword="true"/> if this command is being run from the console, and <see langword="false"/> if this is being run from a Discord chat.</param>
		/// <returns></returns>
		public abstract Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole);
		
		/// <summary>
		/// Returns a formatted string for this command's syntax and subcommands. This is not formatted for a Discord chat.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			string main = "Command: " + Name + " " + Syntax.ToString() + "\nSubcommands: ";
			if (Subcommands == null || Subcommands.Length == 0) {
				main += "None";
			} else {
				for (int idx = 0; idx < Subcommands.Length; idx++) {
					Command sub = Subcommands[idx];
					main += sub.Name;
					if (idx < Subcommands.Length - 1) {
						main += ", ";
					}
				}
			}
			return main;
		}

		public virtual LogMessage ToMessage() {
			LogMessage message = new LogMessage();
			message.AddComponent("Command: ", Color.WHITE, null, true);
			message.AddComponent(Name, Color.DARK_GREEN);
			message.AddComponent("\t");
			message.AddComponent("Subcommands: ", Color.WHITE, null, true);
			string subs = "";
			if (Subcommands == null || Subcommands.Length == 0) {
				subs = "None";
			} else {
				for (int idx = 0; idx < Subcommands.Length; idx++) {
					Command sub = Subcommands[idx];
					subs += sub.Name;
					if (idx < Subcommands.Length - 1) {
						subs += ", ";
					}
				}
			}
			message.AddComponent(subs, Color.DARK_GREEN);
			return message;
		}
	}
}
