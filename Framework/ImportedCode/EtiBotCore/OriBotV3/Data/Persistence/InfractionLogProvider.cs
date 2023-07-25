using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Universal;
using OldOriBot.CoreImplementation;
using OldOriBot.CoreImplementation.Commands;
using OldOriBot.Interaction;
using OldOriBot.Utility;
using OldOriBot.Utility.Extensions;

namespace OldOriBot.Data.Persistence {

	/// <summary>
	/// A utility class for <see cref="CommandLog"/>. It is joined to <see cref="DataPersistence"/>.
	/// </summary>
	public class InfractionLogProvider {

		/// <summary>
		/// A header for log entries v2 and onward. This will not be possible to confuse for a snowflake until July 17, 2056.
		/// </summary>
		public const ulong NEW_LOG_HEADER_LE = 0x5952544E45474F4C;

		/// <summary>
		/// When the log system was created.
		/// </summary>
		public static readonly DateTimeOffset LogSystemCreatedAt = new DateTimeOffset(2021, 4, 23, 0, 0, 0, TimeSpan.Zero);

		protected static readonly IReadOnlyDictionary<LogType, string> EMOJI_BINDINGS = new Dictionary<LogType, string> {
			[LogType.Invalid] = "?",
			[LogType.Note] = EmojiLookup.GetEmoji("memo"),
			[LogType.MinorWarning] = EmojiLookup.GetEmoji("ticket"),
			[LogType.Warning] = EmojiLookup.GetEmoji("warning"),
			[LogType.MajorWarning] = EmojiLookup.GetEmoji("stop_sign"),
			[LogType.Mute] = EmojiLookup.GetEmoji("muted_speaker"),
			[LogType.Unmute] = EmojiLookup.GetEmoji("speaker_high_volume"),
			[LogType.ChangeMute] = EmojiLookup.GetEmoji("wrench"),
			[LogType.AuthorizeAlt] = EmojiLookup.GetEmoji("people_holding_hands"),
			[LogType.RevokeAlt] = EmojiLookup.GetEmoji("person"),
			[LogType.Alert] = EmojiLookup.GetEmoji("triangular_flag_on_post"),
			[LogType.Ban] = EmojiLookup.GetEmoji("hammer"),
			[LogType.Pardon] = EmojiLookup.GetEmoji("door"),
			[LogType.ChangeEntryTime] = EmojiLookup.GetEmoji("ten_thirty"),
			[LogType.RemoveEntry] = EmojiLookup.GetEmoji("see_no_evil_monkey"),
			[LogType.RestoreEntry] = EmojiLookup.GetEmoji("eye"),
		};

		private static readonly Dictionary<Snowflake, InfractionLogProvider> LOGGERS = new Dictionary<Snowflake, InfractionLogProvider>();

		/// <summary>
		/// The root folder for data persistence. Points to <see cref="DataPersistence.PersistenceRootFolder"/>.
		/// </summary>
		public static DirectoryInfo PersistenceRootFolder => DataPersistence.PersistenceRootFolder;

		/// <summary>
		/// The <see cref="BotContext"/> that created this <see cref="InfractionLogProvider"/>.
		/// </summary>
		public BotContext Context { get; }

		/// <summary>
		/// The folder that this <see cref="InfractionLogProvider"/> puts its data into.
		/// </summary>
		public DirectoryInfo TargetFolder { get; }

		/// <summary>
		/// All log entries mapped by user ID to log.
		/// </summary>
		private readonly Dictionary<Snowflake, InfractionLog> Logs = new Dictionary<Snowflake, InfractionLog>();

		private InfractionLogProvider(BotContext context) {
			LOGGERS[context.ID] = this;
			TargetFolder = PersistenceRootFolder.CreateSubdirectory(Path.Combine("InfractionLogging", context.DataPersistenceName));
			if (!TargetFolder.Exists) {
				TargetFolder.Create();
			}
			Context = context;
		}

		/// <summary>
		/// Returns an existing instance or creates a new instance of <see cref="InfractionLogProvider"/> for the given <see cref="BotContext"/>.
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		public static InfractionLogProvider GetProvider(BotContext ctx) {
			if (LOGGERS.TryGetValue(ctx.ID, out InfractionLogProvider logger)) {
				return logger;
			}
			return new InfractionLogProvider(ctx);
		}

		/// <summary>
		/// Returns the infraction log for the given user.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		public InfractionLog For(Snowflake user) {
			if (Logs.TryGetValue(user, out InfractionLog log)) {
				return log;
			}
			FileInfo userFile = new FileInfo(Path.Combine(TargetFolder.FullName, user.ToString() + ".dat"));
			InfractionLog newLog = new InfractionLog(this, user);
			if (userFile.Exists) {
				// Read it from disk.
				Logs[user] = newLog;
				using BinaryReader reader = new BinaryReader(userFile.OpenRead());
				newLog.Read(reader);
			} else {
				// Create a new one and write it to disk.
				Logs[user] = newLog;
				using BinaryWriter writer = new BinaryWriter(userFile.Create());
				newLog.Write(writer);
			}
			return newLog;
		}

		/// <inheritdoc cref="For(Snowflake)"/>
		public InfractionLog For(User user) => For(user.ID);

		/// <summary>
		/// Returns all <see cref="InfractionLog"/> entries for this <see cref="InfractionLogProvider"/>.
		/// </summary>
		/// <returns></returns>
		public List<InfractionLog> GetAllLogs() {
			if (!HasEnumeratedAllFiles) {
				List<InfractionLog> logs = new List<InfractionLog>();
				List<ulong> ids = new List<ulong>();
				FileInfo[] all = TargetFolder.GetFiles("*.dat");
				foreach (FileInfo file in all) {
					if (ulong.TryParse(file.Name.Substring(0, file.Name.Length - file.Extension.Length), out ulong id)) {
						if (ids.Contains(id)) continue;
						logs.Add(For(id));
						ids.Add(id);
					}
				}
				HasEnumeratedAllFiles = true;
				return logs;
			} else {
				return Logs.Values.ToList();
			}
		}
		private bool HasEnumeratedAllFiles = false;

		protected void Save(InfractionLog log) {
			FileInfo userFile = new FileInfo(Path.Combine(TargetFolder.FullName, log.UserID.ToString() + ".dat"));
			userFile.MoveToBackup();
			using BinaryWriter writer = new BinaryWriter(userFile.Create());
			log.Write(writer);
		}

		/// <summary>
		/// Safely attempts to acquire the symbol for the given <see cref="LogType"/>, or returns the symbol for <see cref="LogType.Invalid"/> otherwise.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string GetEntrySymbol(LogType type) {
			return EMOJI_BINDINGS.GetValueOrDefault(type, EMOJI_BINDINGS[LogType.Invalid]);
		}

		#region Log Writing Aliases

		/// <summary>
		/// Appends arbitrary information to the given user from the given moderator. This information can be basically anything.
		/// </summary>
		/// <param name="moderator">The moderator responsible for adding this note.</param>
		/// <param name="to">The member that is the subject of this action.</param>
		/// <param name="info">The information being logged.</param>
		public void AppendInfo(Member moderator, Snowflake to, string info, DateTimeOffset? time = null) {
			For(to).AddEntry(moderator.ID, time ?? DateTimeOffset.UtcNow, LogType.Note, info, false);
		}

		/// <summary>
		/// Appends a minor warning to the given user from the given moderator.
		/// </summary>
		/// <param name="moderator">The moderator responsible for adding this note.</param>
		/// <param name="to">The member that is the subject of this action.</param>
		/// <param name="warnReason">Why this warning is being logged.</param>
		public void AppendMinorWarn(Member moderator, Snowflake to, string warnReason, DateTimeOffset? time = null) {
			For(to).AddEntry(moderator.ID, time ?? DateTimeOffset.UtcNow, LogType.MinorWarning, warnReason, false);
		}

		/// <summary>
		/// Appends a standard warning to the given user from the given moderator.
		/// </summary>
		/// <param name="moderator">The moderator responsible for adding this note.</param>
		/// <param name="to">The member that is the subject of this action.</param>
		/// <param name="warnReason">Why this warning is being logged.</param>
		public void AppendWarn(Member moderator, Snowflake to, string warnReason, DateTimeOffset? time = null) {
			For(to).AddEntry(moderator.ID, time ?? DateTimeOffset.UtcNow, LogType.Warning, warnReason, false);
		}

		/// <summary>
		/// Appends a major / harsh warning to the given user from the given moderator.
		/// </summary>
		/// <param name="moderator">The moderator responsible for adding this note.</param>
		/// <param name="to">The member that is the subject of this action.</param>
		/// <param name="warnReason">Why this warning is being logged.</param>
		public void AppendMajorWarn(Member moderator, Snowflake to, string warnReason, DateTimeOffset? time = null) {
			For(to).AddEntry(moderator.ID, time ?? DateTimeOffset.UtcNow, LogType.MajorWarning, warnReason, false);
		}

		/// <summary>
		/// Appends a mute to the given user from the given moderator.
		/// </summary>
		/// <param name="moderator">The moderator responsible for adding this note.</param>
		/// <param name="to">The member that is the subject of this action.</param>
		/// <param name="muteReason">Why this user is being muted.</param>
		public void AppendMute(Member moderator, Snowflake to, string muteReason, bool fromBot, DateTimeOffset? time = null) {
			For(to).AddEntry(moderator.ID, time ?? DateTimeOffset.UtcNow, LogType.Mute, muteReason, fromBot);
		}

		/// <summary>
		/// Appends a mute tweak to the given user from the given moderator.
		/// </summary>
		/// <param name="moderator">The moderator responsible for adding this note.</param>
		/// <param name="to">The member that is the subject of this action.</param>
		/// <param name="reason">Why this user is being muted.</param>
		/// <param name="oldDuration">The original duration of the mute.</param>
		/// <param name="newDuration">The new duration of the mute.</param>
		/// <param name="fromBot"></param>
		/// <param name="time"></param>
		public void AppendMuteTweak(Member moderator, Snowflake to, string reason, TimeSpan oldDuration, TimeSpan newDuration, bool fromBot, DateTimeOffset? time = null) {
			reason += $" // Changed mute time from {oldDuration} to {newDuration}";
			For(to).AddEntry(moderator.ID, time ?? DateTimeOffset.UtcNow, LogType.ChangeMute, reason, fromBot);
		}

		/// <summary>
		/// Appends an unmute to the given user from the given moderator.
		/// </summary>
		/// <param name="moderator">The moderator responsible for adding this note.</param>
		/// <param name="to">The member that is the subject of this action.</param>
		/// <param name="unmuteReason">Why this user is being unmuted.</param>
		public void AppendUnmute(Member moderator, Snowflake to, string unmuteReason, bool fromBot, DateTimeOffset? time = null) {
			For(to).AddEntry(moderator.ID, time ?? DateTimeOffset.UtcNow, LogType.Unmute, unmuteReason, fromBot, null, fromBot); // Set hidden to true of it was done by the bot.
		}

		/// <summary>
		/// Appends a note for an authorized alternate account of a user.
		/// </summary>
		/// <param name="moderator">The moderator responsible for adding this note.</param>
		/// <param name="main">The user's main account.</param>
		/// <param name="alt">The alt account being associated with the given <paramref name="main"/>.</param>
		public void AppendAuthorizeAlt(Member moderator, Snowflake main, Snowflake alt, string reason, DateTimeOffset? time = null) {
			For(main).AddEntry(moderator.ID, time ?? DateTimeOffset.UtcNow, LogType.AuthorizeAlt, reason, false, alt);
		}

		/// <summary>
		/// Appends a note for an alternate account of a user being unauthorized.
		/// </summary>
		/// <param name="moderator">The moderator responsible for adding this note.</param>
		/// <param name="main">The user's main account.</param>
		/// <param name="alt">The alt account being associated with the given <paramref name="main"/>.</param>
		public void AppendRevokeAlt(Member moderator, Snowflake main, Snowflake alt, string reason, DateTimeOffset? time = null) {
			For(main).AddEntry(moderator.ID, time ?? DateTimeOffset.UtcNow, LogType.RevokeAlt, reason, false, alt);
		}

		/// <summary>
		/// Appends a ban.
		/// </summary>
		/// <param name="moderator"></param>
		/// <param name="to"></param>
		/// <param name="reason"></param>
		/// <param name="time"></param>
		public void AppendBan(Snowflake moderator, Snowflake to, string reason, DateTimeOffset? time = null) {
			For(to).AddEntry(moderator, time ?? DateTimeOffset.UtcNow, LogType.Ban, reason, false);
		}

		/// <summary>
		/// Appends an unban / pardon.
		/// </summary>
		/// <param name="moderator"></param>
		/// <param name="to"></param>
		/// <param name="reason"></param>
		/// <param name="time"></param>
		public void AppendPardon(Snowflake moderator, Snowflake to, string reason, DateTimeOffset? time = null) {
			For(to).AddEntry(moderator, time ?? DateTimeOffset.UtcNow, LogType.Pardon, reason, false);
		}

		/// <summary>
		/// Appends an alert, which is like a note with more meaning.
		/// </summary>
		/// <param name="moderator"></param>
		/// <param name="to"></param>
		/// <param name="reason"></param>
		/// <param name="time"></param>
		public void AppendAlert(Member moderator, Snowflake to, string reason, DateTimeOffset? time = null) {
			For(to).AddEntry(moderator.ID, time ?? DateTimeOffset.UtcNow, LogType.Alert, reason, false);
		}

		#endregion

		/// <summary>
		/// Represents an actual log entry for a single specific user.
		/// </summary>
		public class InfractionLog : IBinaryReadWrite, IEmbeddable {

			private const ushort CURRENT_LOG_VERSION = 2;

			#region Properties

			/// <summary>
			/// The <see cref="InfractionLogProvider"/> that made this entry.
			/// </summary>
			private InfractionLogProvider Creator { get; }

			/// <summary>
			/// The logfile version that this log is.
			/// </summary>
			private int ThisLogVersion { get; set; }

			/// <summary>
			/// The ID of the user that this <see cref="InfractionLog"/> represents.
			/// </summary>
			public Snowflake UserID { get; private set; }

			/// <summary>
			/// Only exists for alt accounts, this is the ID of the main account.
			/// </summary>
			public Snowflake? MainID { get; private set; }

			/// <summary>
			/// Whether or not this <see cref="InfractionLog"/> represents an alt account.
			/// </summary>
			public bool IsAlt => MainID?.IsValid ?? false;

			/// <summary>
			/// Whether or not this alt is authorized. Completely useless if <see cref="IsAlt"/> is false.
			/// </summary>
			public bool IsAuthorized { get; private set; } = false;

			/// <summary>
			/// Whether or not this user's log is complete. Users who joined the server before this system's implementation have this as false by default.
			/// </summary>
			public bool IsComplete { get; set; } = false;

			/// <summary>
			/// The logged actions taken upon this user. Note that this may be invalid if <see cref="IsAlt"/> is true, though generally alt <see cref="InfractionLog"/> instances try to mirror that of their main.
			/// </summary>
			private List<InfractionLogEntry> Entries { get; set; } = new List<InfractionLogEntry>();

			/// <summary>
			/// All accounts that have been authorized for this user as alts. This will only exist for a user's main. If this is an alt, then <see cref="MainID"/> should be used.
			/// </summary>
			private List<Snowflake> KnownAltIDs { get; set; } = new List<Snowflake>();

			#endregion

			/// <summary>
			/// Used to create a fresh entry.
			/// </summary>
			/// <param name="creator"></param>
			/// <param name="userId"></param>
			public InfractionLog(InfractionLogProvider creator, Snowflake userId) {
				Creator = creator;
				UserID = userId;
				ThisLogVersion = 1; // Create as 1, see Write below for more on this.
			}

			#region Associations and Entries

			/// <summary>
			/// For <see cref="InfractionLog"/> instances related to alt accounts, this will return the <see cref="InfractionLog"/> associated with their main.<para/>
			/// If one does not exist, this will create a new one. If this is not an alt (see <see cref="IsAlt"/>) this will throw <see cref="InvalidOperationException"/>.<para/>
			/// Always index the main account to store log entries on.
			/// </summary>
			/// <returns></returns>
			/// <exception cref="InvalidOperationException">If this is called on an <see cref="InfractionLog"/> not associated with an alt account.</exception>
			public InfractionLog GetMainLog() {
				if (!IsAlt) throw new InvalidOperationException($"Cannot get the {nameof(InfractionLog)} for a main account when this log does not have a main associated with it!");
				User user = User.GetOrDownloadUserAsync(MainID.Value).Result;
				if (user == null) throw new InvalidOperationException("Cannot find a user with this ID.");
				return Creator.For(user);
			}

			private void Save() => Creator.Save(this);

			/// <summary>
			/// Adds a new log entry. If <see cref="IsAlt"/> is <see langword="true"/>, then this will apply to their main and will not modify this object. This also saves the log.
			/// </summary>
			/// <param name="moderator"></param>
			/// <param name="time"></param>
			/// <param name="type"></param>
			/// <param name="reason"></param>
			/// <param name="auto"></param>
			/// <param name="newAltId"></param>
			public void AddEntry(Snowflake moderator, DateTimeOffset time, LogType type, string reason, bool auto, Snowflake? newAltId = null, bool hidden = false) {
				if (IsAlt) {
					GetMainLog().AddEntry(moderator, time, type, reason, auto, newAltId); // The main will continue downward and will save properly.
					return;
				}

				if (type == LogType.AuthorizeAlt || type == LogType.RevokeAlt) {
					if (!(newAltId?.IsValid ?? true)) {
						throw new ArgumentOutOfRangeException(nameof(newAltId), "If the log type is related to alt accounts, the alt account ID must be specified and valid!");
					}
				}

				//InfractionLogEntry entry = new InfractionLogEntry(this, moderator, time, type, reason, auto, newAltId);
				InfractionLogEntry entry = new InfractionLogEntry(moderator, time, type, reason, auto, newAltId);
				if (hidden) entry.SetDeleted(true);
				Entries.Add(entry);

				if (type == LogType.AuthorizeAlt) {
					if (!KnownAltIDs.Contains(newAltId.Value)) {
						KnownAltIDs.Add(newAltId.Value);
					}

					InfractionLog altLog = Creator.For(newAltId.Value);
					altLog.RegisterMain(UserID);
					altLog.IsAuthorized = true;
					if (altLog.Entries.Count > 0) {
						// Maybe their alt already has entries. If it does, we need to copy them to their main which will mirror itself to the alt.
						// The goal is to NOT delete existing history, granted it exists.
						foreach (InfractionLogEntry altEntry in altLog.Entries) {
							if (!Entries.Contains(altEntry)) {
								Entries.Add(altEntry);
							}
						}

						Entries.Sort();
					}
				} else if (type == LogType.RevokeAlt) {
					InfractionLog altLog = Creator.For(newAltId.Value);
					altLog.RegisterMain(UserID);
					altLog.IsAuthorized = false;
				}

				foreach (Snowflake knownAlt in KnownAltIDs) {
					InfractionLog altLog = Creator.For(knownAlt);
					altLog.RegisterMain(UserID);
					altLog.CopyEntriesFromMain(this);
					altLog.Save();
				}

				Save();
			}

			/// <summary>
			/// Returns the entry at the given index.
			/// </summary>
			/// <param name="index"></param>
			public InfractionLogEntry GetEntry(int index) => Entries[index];

			/// <summary>
			/// Returns the first entry that satisfies the given predicate. Otherwise, returns null.
			/// </summary>
			/// <param name="predicate"></param>
			/// <returns></returns>
			public InfractionLogEntry FindEntry(Predicate<InfractionLogEntry> predicate) => Entries.Find(predicate);

			/// <summary>
			/// Returns whether or not an entry exists in this <see cref="InfractionLog"/> that satisfies the given predicate.
			/// </summary>
			/// <param name="where"></param>
			/// <returns></returns>
			public bool HasEntry(Predicate<InfractionLogEntry> where) => Entries.Find(where) != null;

			/// <summary>
			/// Returns whether or not an entry exists at the given index.
			/// </summary>
			/// <param name="index"></param>
			/// <returns></returns>
			public bool HasEntry(int index) => Entries.Count > index;

			/// <summary>
			/// Returns the index of the given entry.
			/// </summary>
			/// <param name="entry"></param>
			/// <returns></returns>
			public int IndexOf(InfractionLogEntry entry) => Entries.IndexOf(entry);

			/// <summary>
			/// Updates this <see cref="Entries"/> to mirror that of the main account's <see cref="Entries"/> Throws <see cref="InvalidOperationException"/> if this is not an alt.
			/// </summary>
			private void CopyEntriesFromMain() => CopyEntriesFromMain(GetMainLog());

			private void CopyEntriesFromMain(InfractionLog main) {
				if (!IsAlt) throw new InvalidOperationException();
				Entries = main.Entries.ToArray().ToList(); //ToArrayToList clones it.
				Entries.Sort();
			}

			/// <summary>
			/// Registers the given <see cref="Snowflake"/> as the ID of this user's main account.
			/// </summary>
			/// <param name="mainId"></param>
			protected void RegisterMain(Snowflake mainId) {
				MainID = mainId;
			}

			/// <summary>
			/// Sets the deleted flag on the given entry, then saves this log.
			/// </summary>
			/// <param name="entry"></param>
			/// <param name="delete"></param>
			public void SetDeleted(int entry, bool delete) => SetDeleted(GetEntry(entry), delete);

			/// <summary>
			/// Sets the time flag on the given entry, then saves this log.
			/// </summary>
			/// <param name="entry"></param>
			/// <param name="time"></param>
			public void SetTime(int entry, DateTimeOffset time) => SetTime(GetEntry(entry), time);

			/// <summary>
			/// Sets the deleted flag on the given entry, then saves this log.
			/// </summary>
			/// <param name="entry"></param>
			/// <param name="delete"></param>
			public void SetDeleted(InfractionLogEntry entry, bool delete) {
				entry.SetDeleted(delete);
				Save();
			}

			/// <summary>
			/// Sets the time flag on the given entry, then saves this log.
			/// </summary>
			/// <param name="entry"></param>
			/// <param name="time"></param>
			public void SetTime(InfractionLogEntry entry, DateTimeOffset time) {
				entry.SetTime(time);
				Save();
			}

			#endregion

			#region Extra Garbage

			private int GetLongestModUsernameLength() {
				BotContext context = Creator.Context;
				Role modRole = context.Server.GetRole(603306540438388756);
				Member[] mods = context.Server.FindMembersWithRole(modRole);
				int longest = 0;
				foreach (Member mod in mods) {
					int nameLen = mod.FullName.Length;
					if (nameLen > longest) {
						longest = nameLen;
					}
				}
				return longest;
			}

			#endregion

			#region Interface Implementations

			public void Write(BinaryWriter writer) {
				if (ThisLogVersion == 1) {
					Member logFor = Creator.Context.Server.GetMemberAsync(UserID).GetAwaiter().GetResult();
					if (logFor != null) {
						IsComplete = logFor.JoinedAt > LogSystemCreatedAt;
					}
				}

				ThisLogVersion = CURRENT_LOG_VERSION;

				writer.Write(NEW_LOG_HEADER_LE); // log v2 onward
				writer.Write(CURRENT_LOG_VERSION);
				writer.Write(IsComplete);

				writer.Write(UserID);
				writer.Write(MainID ?? default);

				if (!IsAlt) {
					writer.WriteEntries(Entries);
					writer.Write((ushort)KnownAltIDs.Count);
					foreach (Snowflake sf in KnownAltIDs) writer.Write(sf);
				} else {
					writer.Write(IsAuthorized);
				}
			}

			public void Read(BinaryReader reader) {
				// A bit of a hack, but we need to get ahold of the first 8 bytes, which could be a snowflake or the new v2 header.
				ulong value = reader.ReadUInt64();

				ushort version;
				if (value == NEW_LOG_HEADER_LE) {
					version = reader.ReadUInt16();
					IsComplete = reader.ReadBoolean();
					UserID = reader.ReadUInt64();
				} else {
					version = 1;
					UserID = value;
				}
				Snowflake id = reader.ReadUInt64();
				if (id.IsValid) MainID = id;
				ThisLogVersion = version;

				if (!IsAlt) {
					Entries = reader.ReadMetaEntries<InfractionLogEntry>(version).ToList();
					// ^ Metadata populates their version.

					if (version == 1) {
						Member logFor = Creator.Context.Server.GetMemberAsync(UserID).GetAwaiter().GetResult();
						if (logFor != null) {
							IsComplete = logFor.JoinedAt > LogSystemCreatedAt;
						}
					}

					ushort length = reader.ReadUInt16();
					KnownAltIDs = new List<Snowflake>();
					for (int idx = 0; idx < length; idx++) {
						KnownAltIDs.Add(reader.ReadUInt64());
					}
				} else {
					IsAuthorized = reader.ReadBoolean();
					CopyEntriesFromMain();
				}
			}

			public Embed ToEmbed() => ToEmbed(false);

			public Embed ToEmbed(bool showHidden) {
				EmbedBuilder builder = new EmbedBuilder {
					Title = "User Log"
				};

				//int nameLen = GetLongestModUsernameLength();
				int nameLen = 0;

				StringBuilder desc = new StringBuilder("**This is your reminder to pay mind to the index number (it starts from 0, and may skip up).**\n\n");
				StringBuilder descNoReason = new StringBuilder("**List text is too long. This will not contain reasons. You must ask for a specific entry to view its reason.**\n\n");
				Entries.Sort((left, right) => {
					return left.Time.CompareTo(right.Time);
				});

				for (int idx = 0; idx < Entries.Count; idx++) {
					InfractionLogEntry entry = Entries[idx];
					if (entry.Hidden && !showHidden) continue;

					User mod = User.GetOrDownloadUserAsync(entry.ModeratorID).Result;
					if (mod.FullName.Length > nameLen) {
						nameLen = mod.FullName.Length;
					}
				}
				for (int idx = 0; idx < Entries.Count; idx++) {
					InfractionLogEntry entry = Entries[idx];
					if (entry.Hidden && !showHidden) continue;

					string entryText = entry.ToString(this, idx, nameLen, true);
					desc.AppendLine(entryText + entry.Information); // the true param above just removes Information, so I will manually add it here to save calls.
					desc.AppendLine();
					descNoReason.AppendLine(entryText);
					descNoReason.AppendLine();
				}
				if (desc.Length > 2048) {
					builder.Description = descNoReason.ToString();
				} else {
					builder.Description = desc.ToString();
				}

				User user = User.GetOrDownloadUserAsync(UserID).Result;
				Member inServer = user.InServerAsync(Creator.Context.Server).Result;
				if (KnownAltIDs.Count == 0 && !IsAlt) {
					builder.AddField("About", $"**User ID:** {UserID}\n**User:** {inServer?.FullNickname ?? user.FullName}");
					if (!IsComplete) builder.AddField("Log Status Notice", "This log entry may not be complete. It is advised that you manually append any missing log entries (by searching #log-chat, remember to specify their associated time) and then use `>> log markcomplete <id> true` to mark this log as up to date.");
				} else {
					if (IsAlt) {
						string about = $"**User ID:** {UserID}\n**User:** {inServer?.FullNickname ?? user.FullName}\n**Main Account:** <@!{MainID}> ({MainID})";
						builder.AddField("About", about);
					} else {
						string about = $"**User ID:** {UserID}\n**User:** {inServer?.FullNickname ?? user.FullName}\n**Registered Alts:** ";
						foreach (Snowflake altID in KnownAltIDs) {
							about += $"<@!{altID}> ({altID})";
							if (!Creator.For(altID).IsAuthorized) {
								about += " **(Not Authorized)**";
							}
							if (altID != KnownAltIDs.Last()) {
								about += ", ";
							}
						}
						builder.AddField("About", about);
						if (!IsComplete) builder.AddField("Log Status Notice", "This log entry may not be complete. It is advised that you manually append any missing log entries (by searching #log-chat, remember to specify their associated time) and then use `>> log markcomplete <id> true` to mark this log as up to date.");
					}
				}
				if (!showHidden) {
					builder.AddField("Entries Hidden", "This log is hiding hidden entries. Add `true` as a second parameter to `log view` to show these.");
				}

				return builder.Build();
			}

			#endregion

			/// <summary>
			/// Represents an entry in an <see cref="InfractionLog"/>
			/// </summary>
			public class InfractionLogEntry : IMetadataReceiver, IEquatable<InfractionLogEntry>, IComparable<InfractionLogEntry> {

				/// <summary>
				/// The ID of the moderator that added this entry.
				/// </summary>
				public Snowflake ModeratorID { get; private set; }
				
				/// <summary>
				/// When this entry was created.
				/// </summary>
				public DateTimeOffset Time { get; private set; }

				/// <summary>
				/// The type of log entry that this is.
				/// </summary>
				public LogType Type { get; private set; }

				/// <summary>
				/// The moderator-provided information for this log entry.
				/// </summary>
				public string Information { get; private set; }

				/// <summary>
				/// Whether or not this log entry was created by an automatic bot routine.<para/>
				/// This may change whether or not the entry is shown.
				/// </summary>
				public bool Automatic { get; private set; }

				/// <summary>
				/// If this log entry is related to alt accounts, this is the ID of the alt. This will be <see langword="null"/> otherwise.
				/// </summary>
				public Snowflake? AltID { get; private set; }

				/// <summary>
				/// Whether or not this entry was deleted with [log remove].
				/// </summary>
				public bool Deleted { get; private set; }

				/// <summary>
				/// Whether or not this entry should be hidden.
				/// </summary>
				public bool Hidden => Deleted || IsLogTypeAlwaysHidden(Type);

				/// <summary>
				/// The version of this entry, which mirrors the version of its container.
				/// </summary>
				public ushort Version { get; private set; }

				/// <summary>
				/// Returns <see langword="true"/> if the given log entry type is always hidden and cannot be made visible by default.
				/// </summary>
				/// <param name="type"></param>
				/// <returns></returns>
				public static bool IsLogTypeAlwaysHidden(LogType type) {
					return type == LogType.ChangeEntryTime || type == LogType.RemoveEntry || type == LogType.RestoreEntry;
				}

				public static bool IsLogTypeImmutable(LogType type) {
					return type == LogType.ChangeEntryTime || type == LogType.RemoveEntry || type == LogType.RestoreEntry;
				}

				internal void SetDeleted(bool delete) {
					Deleted = delete;
				}

				internal void SetTime(DateTimeOffset time) {
					Time = time;
				}

				/// <summary>
				/// Strictly only for instantiating this object from a stream.
				/// </summary>
				public InfractionLogEntry() { }

				/// <summary>
				/// Construct a new log entry with the given information.
				/// </summary>
				/// <param name="mod">The moderator that is responsible for the creation ofthis entry.</param>
				/// <param name="time">The time that this entry was added.</param>
				/// <param name="type">The type of entry that this is.</param>
				/// <param name="info">The information associated with this entry.</param>
				/// <param name="wasAuto">True if this action was performed by the bot itself.</param>
				/// <param name="altID">If this pertains to alt accounts, this is the ID of the alt.</param>
				public InfractionLogEntry(Snowflake mod, DateTimeOffset time, LogType type, string info, bool wasAuto, Snowflake? altID = null) {
					ModeratorID = mod;
					Time = time;
					Type = type;
					Information = info;
					Automatic = wasAuto;
					AltID = altID;
				}

				public void ReceiveMetadata(params object[] data) {
					if (data.Length == 1) {
						ushort version = (ushort)data[0];
						Version = version;
					} else {
						Version = 1;
					}
				}

				public void Write(BinaryWriter writer) {
					writer.Write(Time.ToUnixTimeMilliseconds());
					writer.Write(ModeratorID);

					int packedInfo = (byte)Type << 2; // Now we move only by two.
					packedInfo |= (Automatic ? 1 : 0) << 1;
					packedInfo |= Deleted ? 1 : 0;

					writer.Write((byte)packedInfo);
					writer.Write(AltID ?? default);
					writer.WriteStringSL(Information);
				}

				public void Read(BinaryReader reader) {
					Time = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
					ModeratorID = reader.ReadUInt64();
					byte packedInfo = reader.ReadByte();
					if (Version == 1) {
						Type = (LogType)((packedInfo & 0b1111_0000) >> 4);
					} else {
						Type = (LogType)((packedInfo & 0b111111_00) >> 2);
					}
					Automatic = (packedInfo & 2) == 2;
					Deleted = (packedInfo & 1) == 1;

					ulong altId = reader.ReadUInt64();
					if (altId == 0) {
						AltID = null;
					} else {
						AltID = altId;
					}
					Information = reader.ReadStringSL();
				}

				/// <summary>
				/// Intended for a single-line specific entry drop.
				/// </summary>
				/// <param name="parent">A manually-defined override for <see cref="Parent"/> intended for use on entries part of an alt's infraction log. Set to null to use <see cref="Parent"/>.</param>
				/// <returns></returns>
				public string ToString(InfractionLog parent) {
					string time = Time.AsDiscordTimestamp(); //Time.InEUFormat();
					User modUser = User.GetOrDownloadUserAsync(ModeratorID).Result;
					string modName = modUser?.FullName ?? ModeratorID.ToString();
					string ret = $"**`{GetEntrySymbol(Type)} {modName.ReverseGraves()} issued {Type,NAME_LENGTH_LOGTYPE}`\nOn:** {time}\n**Reason:** {Information}\n";
					if (parent.MainID != null) {
						if (Type == LogType.AuthorizeAlt || Type == LogType.RevokeAlt) {
							Snowflake main = parent.MainID.Value;
							ret += $" [Main: <@!{main}> ({main})] ";
						}
					} else if (AltID != null) {
						if (Type == LogType.AuthorizeAlt || Type == LogType.RevokeAlt) {
							Snowflake alt = AltID.Value;
							ret += $" [Alt: <@!{alt}> ({alt})] ";
						}
					}
					return ret;
				}

				/// <summary>
				/// Turns this entry into a single-line string. Does not respect <see cref="Hidden"/>, and always returns a formatted string. This returns a string intended to go into a Discord message.
				/// </summary>
				/// <param name="parent">A manually-defined override for <see cref="Parent"/> intended for use on entries part of an alt's infraction log. Set to null to use <see cref="Parent"/>.</param>
				/// <param name="spaces">Intended to be supplied by the parent <see cref="InfractionLog"/>, this is the length of the longest mod's username so that text can be formatted uniformly..</param>
				/// <returns></returns>
				public string ToString(InfractionLog parent, int index, int longestModNameLength, bool noReason = false) {
					string time = Time.AsDiscordTimestamp(); //Time.InEUFormat();
					User modUser = User.GetOrDownloadUserAsync(ModeratorID).Result;
					string modName = modUser?.FullName ?? ModeratorID.ToString();
					if (longestModNameLength != 0) modName = modName.PadRight(longestModNameLength);
					string ret = $"**`{index}: {GetEntrySymbol(Type)} {modName.ReverseGraves()} issued {Type,NAME_LENGTH_LOGTYPE}`\nOn:** {time}\n";
					if (!noReason) {
						ret += Information;
					}
					if (parent.MainID != null) {
						if (Type == LogType.AuthorizeAlt || Type == LogType.RevokeAlt) {
							Snowflake main = parent.MainID.Value;
							ret += $" [Main: <@!{main}> ({main})] ";
						}
					} else if (AltID != null) {
						if (Type == LogType.AuthorizeAlt || Type == LogType.RevokeAlt) {
							Snowflake alt = AltID.Value;
							ret += $" [Alt: <@!{alt}> ({alt})] ";
						}
					}
					return ret;
				}

				public override bool Equals(object obj) {
					if (ReferenceEquals(this, obj)) return true;
					if (obj is InfractionLogEntry entry) return Equals(entry);
					return false;
				}

				public bool Equals(InfractionLogEntry other) {
					if (other is null) return false;
					if (ReferenceEquals(this, other)) return true;
					return ModeratorID == other.ModeratorID && Type == other.Type && Time == other.Time;
				}

				public override int GetHashCode() {
					return HashCode.Combine(ModeratorID, Type, Time);
				}

				public int CompareTo([AllowNull] InfractionLogEntry other) {
					if (other is null) return -1;
					return Time.CompareTo(other.Time);
				}
			}

			/// <summary>
			/// The longest <see cref="LogType"/> entry name's length.
			/// </summary>
			private const int NAME_LENGTH_LOGTYPE = 15;

		}

		/// <summary>
		/// Represents various types of log entries, which represent actions taken.
		/// </summary>
		public enum LogType {

			// REMEMBER TO UPDATE THE LENGTH IN TOSTRING IF YOU ADD ANY NEW ENTRIES
			// THIS IS SO THAT THE DISPLAYED NAME LENGTH IS UNIFORM
			// NAME_LENGTH_LOGTYPE

			/// <summary>
			/// An invalid log entry type.
			/// </summary>
			Invalid = 0,

			/// <summary>
			/// This log entry is a note.
			/// </summary>
			Note = 1,

			/// <summary>
			/// This log entry is a minor warning.
			/// </summary>
			MinorWarning = 2,

			/// <summary>
			/// This log entry is a standard warning.
			/// </summary>
			Warning = 3,

			/// <summary>
			/// This log entry is a major warning.
			/// </summary>
			MajorWarning = 4,

			/// <summary>
			/// This log entry is a mute.
			/// </summary>
			Mute = 5,

			/// <summary>
			/// This log entry is tweaking a mute.
			/// </summary>
			ChangeMute = 6,

			/// <summary>
			/// This log entry is an unmute, be it manual or automatic.
			/// </summary>
			Unmute = 7,

			/// <summary>
			/// This log entry is authorizing an alt account.
			/// </summary>
			AuthorizeAlt = 8,

			/// <summary>
			/// This log entry is revoking authorization of an alt account.
			/// </summary>
			RevokeAlt = 9,

			/// <summary>
			/// This log entry is an alert, like a note with more weight.
			/// </summary>
			Alert = 10,

			/// <summary>
			/// This log entry corresponds to banning the member.
			/// </summary>
			Ban = 11,

			/// <summary>
			/// This log entry corresponds to unbanning ("pardoning") the member.
			/// </summary>
			Pardon = 12,

			/// <summary>
			/// This log entry corresponds to a moderator-only reason for changing the log entry's time.
			/// </summary>
			ChangeEntryTime = 13,

			/// <summary>
			/// This log entry corresponds to a moderator-only reason for hiding a log entry.
			/// </summary>
			RemoveEntry = 14,

			/// <summary>
			/// This log entry corresponds to a moderator-only reason for restoring a log entry.
			/// </summary>
			RestoreEntry = 15,

		}

	}
}
