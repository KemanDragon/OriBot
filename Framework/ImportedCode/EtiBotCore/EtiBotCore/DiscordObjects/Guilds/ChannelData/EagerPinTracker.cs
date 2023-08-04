#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Threading;
using EtiLogger.Data.Structs;
using EtiLogger.Logging;
using SignalCore;

namespace EtiBotCore.DiscordObjects.Guilds.ChannelData {

	/// <summary>
	/// A special event container just for message pins. This uses some witchcraft to try to track pins better.<para/>
	/// Avoid using this en masse, as it does a lot of expensive network requests on initialization.
	/// </summary>
	public class EagerPinTracker {

		private static readonly Dictionary<ChannelBase, EagerPinTracker> TrackerCache = new Dictionary<ChannelBase, EagerPinTracker>();

		/// <summary>
		/// The currently pinned messages.
		/// </summary>
		private readonly List<Message> PinnedMessages = new List<Message>(50);

		/// <summary>
		/// A file used to persistently track message pin times.
		/// </summary>
		private readonly PinTimeTrackerFile TrackerFile;

		/// <summary>
		/// The edited pin array on a message edit signal. The pin update signal looks for this to figure out which message changed.
		/// </summary>
		private List<Message> OldPinnedMessages = new List<Message>(50);

		/// <summary>
		/// When a pin or unpin was last handled in this channel from an edit signal.
		/// </summary>
		private long LastEditOrCreateSignalHandledAt = 0;

		/// <summary>
		/// When a channel pin update message was last received.
		/// </summary>
		private long LastPinSignalReceivedAt = 0;

		/// <summary>
		/// The current epoch.
		/// </summary>
		private long Epoch => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

		/// <summary>
		/// The channel associated with this tracker.
		/// </summary>
		private ChannelBase Channel { get; }

		/// <summary>
		/// Whether or not this has been invalidated.
		/// </summary>
		private bool Invalidated = false;

		/// <summary>
		/// A logger for this <see cref="EagerPinTracker"/>
		/// </summary>
		private Logger TrackerLog { get; }

		/// <summary>
		/// Returns a new or existing <see cref="EagerPinTracker"/> for the given channel.
		/// </summary>
		/// <param name="channel"></param>
		/// <returns></returns>
		public static EagerPinTracker GetTrackerFor(ChannelBase channel) {
			if (TrackerCache.TryGetValue(channel, out EagerPinTracker? retn)) {
				return retn!;
			}
			retn = new EagerPinTracker(channel);
			TrackerCache[channel] = retn;
			return retn;
		}

		/// <summary>
		/// Returns whether or not the given channel has an <see cref="EagerPinTracker"/> tied to it.
		/// </summary>
		/// <param name="channel"></param>
		/// <returns></returns>
		public static bool HasTracker(ChannelBase channel) => TrackerCache.ContainsKey(channel);

		/// <summary>
		/// Construct a new <see cref="EagerPinTracker"/>, which attempts to accurately track pins in a channel.
		/// </summary>
		/// <param name="channel"></param>
		private EagerPinTracker(ChannelBase channel) {
			if (channel is DMChannel dm) {
				Channel = dm;
				DiscordClient.Current!.Events.MessageEvents.OnMessageEdited += OnMessageEditedGuildOrDM;
				DiscordClient.Current!.Events.MessageEvents.OnMessageDeleted += OnMessageDeletedGuildOrDM;
				DiscordClient.Current!.Events.MessageEvents.OnMessageCreated += OnMessageCreatedGuildOrDM;
				DiscordClient.Current!.Events.MessageEvents.OnMessagesBulkDeleted += OnMessagesBulkDeleted;
				DiscordClient.Current!.Events.MessageEvents.OnDirectMessagePinStateChanged += OnPinsChangedDM;
			} else if (channel is TextChannel txt) {
				Channel = txt;
				DiscordClient.Current!.Events.MessageEvents.OnMessageEdited += OnMessageEditedGuildOrDM;
				DiscordClient.Current!.Events.MessageEvents.OnMessageDeleted += OnMessageDeletedGuildOrDM;
				DiscordClient.Current!.Events.MessageEvents.OnMessageCreated += OnMessageCreatedGuildOrDM;
				DiscordClient.Current!.Events.MessageEvents.OnMessagesBulkDeleted += OnMessagesBulkDeleted;
				DiscordClient.Current!.Events.GuildEvents.OnPinsUpdated += OnPinsChangedGuild;
			} else {
				Channel = channel;
				throw new ArgumentException("The given channel is not a text channel or DM!");
			}
			TrackerFile = new PinTimeTrackerFile(channel);
			TrackerLog = new Logger(new LogMessage.MessageComponent($"[PinTracker {Channel.ID}] ", Color.CYAN));
			EtiTaskExtensions.RunSync(RedownloadMessages);
		}

		/// <summary>
		/// Tell this <see cref="EagerPinTracker"/> that its channel was deleted. This will verify if <see cref="Channel"/>.Deleted is actually true, and if so, will disconnect all of its event handlers and send an unpin signal for all messages in the channel.
		/// </summary>
		/// <exception cref="InvalidOperationException">If this has already been called successfully.</exception>
		internal void TellChannelWasDeleted() {
			if (Invalidated) return;
			if (Channel.Deleted) {
				Invalidated = true;

				// Remove all event handlers
				TrackerLog.WriteLine($"The channel associated with this tracker ({Channel.ID}) was deleted! Firing unpinned event for all messages and disconnecting event handlers.");
				if (Channel is DMChannel) {
					DiscordClient.Current!.Events.MessageEvents.OnDirectMessagePinStateChanged -= OnPinsChangedDM;
				} else {
					DiscordClient.Current!.Events.GuildEvents.OnPinsUpdated -= OnPinsChangedGuild;
				}
				DiscordClient.Current!.Events.MessageEvents.OnMessageEdited -= OnMessageEditedGuildOrDM;
				DiscordClient.Current!.Events.MessageEvents.OnMessageDeleted -= OnMessageDeletedGuildOrDM;
				DiscordClient.Current!.Events.MessageEvents.OnMessagesBulkDeleted -= OnMessagesBulkDeleted;
				DiscordClient.Current!.Events.MessageEvents.OnMessageCreated -= OnMessageCreatedGuildOrDM;

				foreach (Message msg in PinnedMessages) {
					TrackerFile.Set(msg);
					OnMessageUnpinned?.Invoke(msg);
				}
			}
		}

		private async Task RedownloadMessages() {
			PinnedMessages.Clear();
			var messages = (await TextChannel.GetPinnedMessages.ExecuteAsync<Payloads.PayloadObjects.Message[]>(new APIRequestData { Params = { Channel.ID } })).Item1;
			if (messages != null) {
				foreach (var message in messages) {
					if (Channel is TextChannel text) message.GuildID = text.ServerID;
					Message msgObj = await Message.GetOrCreateAsync(message);
					PinnedMessages.Add(msgObj);
				}
			} else {
				throw new Exception($"Failed to initialize an {nameof(EagerPinTracker)} for this channel! Reason: Message array download returned null. Was the request discarded by Discord?");
			}
		}

		private async Task OnMessagesBulkDeleted(Snowflake[] messageIds, ChannelBase channel) {
			if (channel != Channel) return;
			foreach (Snowflake id in messageIds) {
				await OnMessageDeletedGuildOrDM(id, channel);
			}
		}

		/// <summary>
		/// Runs when a message is deleted either in a guild channel or a DM.
		/// </summary>
		/// <param name="messageId"></param>
		/// <param name="channel"></param>
		/// <returns></returns>
		private async Task OnMessageDeletedGuildOrDM(Snowflake messageId, ChannelBase channel) {
			if (channel != Channel) return;
			Message? pinned = PinnedMessages.Find(msg => msg.ID == messageId);
			if (pinned != null) {
				// No need to set timers because the events don't fire.
				PinnedMessages.Remove(pinned);
				TrackerFile.Remove(pinned);
				OldPinnedMessages = PinnedMessages;
				OnMessageUnpinned?.Invoke(pinned);
			}
		}
		
		private async Task PinsChanged() {
			LastPinSignalReceivedAt = Epoch;
			await Task.Delay(1000);
			if (Epoch - LastEditOrCreateSignalHandledAt > 2500) {
				// Received an edit signal OVER 5s ago.
				// Chances are, it just wasn't sent by Discord (unless their API is having a bad day).
				// Because of this, we should just expensively redownload. Log this too.
				TrackerLog.WriteWarning("A pin change signal was received, and the last edit signal was over 2.5 seconds ago! I'm going to redownload the pins.", EtiLogger.Logging.LogLevel.Debug);
				await RedownloadMessages();
				IEnumerable<Message> differences = PinnedMessages.Where(newPin => !OldPinnedMessages.Any(oldPin => newPin.ID == oldPin.ID));
				OldPinnedMessages = PinnedMessages;

				foreach (Message msg in differences) {
					if (PinnedMessages.Contains(msg)) {
						TrackerFile.Set(msg);
						OnMessagePinned?.Invoke(msg);
					} else {
						TrackerFile.Remove(msg);
						OnMessageUnpinned?.Invoke(msg);
					}
				}
			}
		}

		/// <summary>
		/// When the pin list in a DM channel changes.
		/// </summary>
		/// <param name="inChannel"></param>
		/// <returns></returns>
		private async Task OnPinsChangedDM(DMChannel inChannel) { if (inChannel == Channel) await PinsChanged(); }

		/// <summary>
		/// When the pin list in a guild channel changes.
		/// </summary>
		/// <param name="guild"></param>
		/// <param name="channel"></param>
		/// <param name="latestPinTime"></param>
		/// <returns></returns>
		private async Task OnPinsChangedGuild(Guild guild, TextChannel channel, DateTimeOffset? latestPinTime) { if (channel == Channel) await PinsChanged(); }

		/// <summary>
		/// When a message is edited in a guild channel or DM.
		/// </summary>
		/// <param name="oldMessage"></param>
		/// <param name="message"></param>
		/// <param name="pinned"></param>
		/// <returns></returns>
		private async Task OnMessageEditedGuildOrDM(Message oldMessage, Message message, bool? pinned) {
			if (message.Channel != Channel) return;
			if (pinned == null) return;
			TrackerLog.WriteLine($"A message edit signal was received for message {message.ID}.", LogLevel.Debug);
			LastEditOrCreateSignalHandledAt = Epoch;
			if (Epoch - LastPinSignalReceivedAt < 2500) {
				// If it's within 5000ms, then we should do this because we didn't do a redownload.
				if (!pinned.Value) {
					// was unpinned
					if (PinnedMessages.Remove(message)) 
						OnMessageUnpinned?.Invoke(message);
					TrackerFile.Remove(message);
				} else {
					if (!PinnedMessages.Contains(message)) {
						OnMessagePinned?.Invoke(message);
						PinnedMessages.Insert(0, message);
						TrackerFile.Set(message);
					}
				}
				OldPinnedMessages = PinnedMessages;
			}
		}

		/// <summary>
		/// When a message is created in a guild channel or DM. This is an exceptionally hacky trick and aims to try to grab the pinned message out of the pin notification.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="pinned"></param>
		/// <returns></returns>
		private async Task OnMessageCreatedGuildOrDM(Message message, bool? pinned) {
			if (message.Channel != Channel) return;
			if (message.Type != MessageType.ChannelPinnedMessage) return;
			if (message.Reference == null) return;
			if (message.Reference.ChannelID == Channel.ID) {
				TrackerLog.WriteLine($"A message create signal was received, and was a pin notification referencing message {message.Reference.MessageID}.", LogLevel.Debug);
				LastEditOrCreateSignalHandledAt = Epoch;
				Message.InstantiatedMessages.TryGetValue(message.Reference.MessageID!.Value, out Message? target);
				if (target == null) {
					if (Channel is TextChannel text) {
						target = await text.GetMessageAsync(message.Reference.MessageID!.Value);
					} else if (Channel is DMChannel dm) {
						target = await dm.GetMessageAsync(message.Reference.MessageID!.Value);
					}
				}
				if (target == null) {
					TrackerLog.WriteWarning("Failed to download the referenced message!");
					return;
				}
				if (Epoch - LastPinSignalReceivedAt < 5000) {
					// If it's within 5000ms, then we should do this because we didn't do a redownload.
					if (!target.Pinned) {
						// was unpinned
						if (PinnedMessages.Remove(target))
							OnMessageUnpinned?.Invoke(target);
						TrackerFile.Remove(target);
					} else {
						if (!PinnedMessages.Contains(target)) {
							PinnedMessages.Remove(target);
							OnMessagePinned?.Invoke(target);
							TrackerFile.Set(target);
						}
					}
					OldPinnedMessages = PinnedMessages;
				}
			}
		}

		/// <summary>
		/// Pins this message through the pin tracker, which causes it to not rely on events to determine its changed state.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="reason"></param>
		/// <returns></returns>
		public async Task PinMessageAsync(Message message, string? reason = null) {
			if (message.Channel != Channel) throw new ArgumentException("This message does not belong to this channel.");
			LastEditOrCreateSignalHandledAt = Epoch;
			message.BeginChanges();
			message.Pinned = true;
			HttpResponseMessage? response = await message.ApplyChanges(reason);
			if (response?.StatusCode == System.Net.HttpStatusCode.NoContent) {
				if (!PinnedMessages.Contains(message)) {
					PinnedMessages.Insert(0, message);
					OnMessagePinned?.Invoke(message);
					TrackerFile.Set(message);
				}
			}
		}

		/// <summary>
		/// Unpins this message through the pin tracker, which causes it to not rely on events to determine its changed state.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="reason"></param>
		/// <returns></returns>
		public async Task UnpinMessageAsync(Message message, string? reason = null) {
			if (message.Channel != Channel) throw new ArgumentException("This message does not belong to this channel.");
			LastEditOrCreateSignalHandledAt = Epoch;
			message.BeginChanges();
			message.Pinned = false;
			HttpResponseMessage? response = await message.ApplyChanges(reason);
			if (response?.StatusCode == System.Net.HttpStatusCode.NoContent) {
				if (PinnedMessages.Remove(message))
					OnMessageUnpinned?.Invoke(message);
				TrackerFile.Remove(message);
			}
		}

		/// <summary>
		/// Get the messages pinned in this channel where the first message is the most recent pin, and the last message is the latest pin.<para/>
		/// Unfortunately, this sort is not guaranteed, but it is maintained as accurately as possible.
		/// </summary>
		/// <returns></returns>
		public Message[] GetPinnedMessages() {
			Message[] msgs = PinnedMessages.ToArray();
			Array.Sort(msgs, (a, b) => {
				DateTimeOffset? t0 = TrackerFile.Get(a);
				DateTimeOffset? t1 = TrackerFile.Get(b);
				if (t0 != null && t1 != null) {
					// a is t0
					// b is t1
					// if t0 is in the future relative to t1, t0 needs to come first
					return (int)(t1.Value.ToUnixTimeMilliseconds() - t0.Value.ToUnixTimeMilliseconds());
				}
				return 0;
			});
			return msgs;
		}

		#region Delegates

		/// <summary>
		/// A delegate used to relay that a message's pin state has changed.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public delegate Task MessagePinStateChanged(Message message);

		#endregion

		#region Events

		/// <summary>
		/// An event that fires when a message is pinned. In this event, the message must be younger than two weeks old.
		/// </summary>
		public event MessagePinStateChanged OnMessagePinned;

		/// <summary>
		/// An event that fires when a message is unpinned for any reason at all, including deletion. In this event, the message must be younger than two weeks old.
		/// </summary>
		public event MessagePinStateChanged OnMessageUnpinned;

		#endregion

		private class PinTimeTrackerFile {

			private readonly FileInfo TargetPinStorageFile;

			private readonly Dictionary<Snowflake, DateTimeOffset> Times = new Dictionary<Snowflake, DateTimeOffset>();

			public PinTimeTrackerFile(ChannelBase forChannel) {
				string domain;
				string fname = forChannel.ID + ".pins";
				if (forChannel is DMChannel) {
					domain = "@me";
				} else if (forChannel is TextChannel tx) {
					domain = tx.ServerID.ToString();
				} else {
					throw new ArgumentException("Unexpected channel type.");
				}

				DirectoryInfo folder;
				if (Directory.Exists("V:\\")) {
					folder = Directory.CreateDirectory(Path.Combine(@"V:\EtiBotCore\pinTrackers", domain));
				} else {
					folder = Directory.CreateDirectory(Path.Combine(@"C:\EtiBotCore\pinTrackers", domain));
				}
				
				TargetPinStorageFile = new FileInfo(Path.Combine(folder.FullName, fname));
				if (!TargetPinStorageFile.Exists) TargetPinStorageFile.Create().Close();

				foreach (string line in File.ReadAllLines(TargetPinStorageFile.FullName)) {
					if (line.Contains("=")) {
						string[] data = line.Split('=');
						if (data.Length != 2) continue;

						if (Snowflake.TryParse(data[0], out Snowflake messageId) && long.TryParse(data[1], out long epoch)) {
							Times[messageId] = DateTimeOffset.FromUnixTimeMilliseconds(epoch);
						}
					}
				}
			}

			/// <summary>
			/// Save the pin tracker file.
			/// </summary>
			private void Save() {
				string[] lines = new string[Times.Count];
				int i = 0;
				foreach (KeyValuePair<Snowflake, DateTimeOffset> timeData in Times) {
					lines[i] = $"{timeData.Key}={timeData.Value.ToUnixTimeMilliseconds()}";
					i++;
				}
				File.WriteAllLines(TargetPinStorageFile.FullName, lines);
			}

			/// <summary>
			/// Sets the time that the given message was pinned. A <see langword="null"/> <paramref name="time"/> signifies to use <see cref="DateTimeOffset.UtcNow"/>.
			/// </summary>
			/// <param name="messageId"></param>
			/// <param name="time"></param>
			public void Set(Snowflake messageId, DateTimeOffset? time = null) {
				Times[messageId] = time.GetValueOrDefault(DateTimeOffset.UtcNow);
				Save();
			}

			/// <inheritdoc cref="Set(Snowflake, DateTimeOffset?)"/>
			public void Set(Message msg, DateTimeOffset? time = null) => Set(msg.ID, time);

			/// <summary>
			/// Removes the given message from the registry.
			/// </summary>
			/// <param name="messageId"></param>
			public void Remove(Snowflake messageId) {
				Times.Remove(messageId);
				Save();
			}

			/// <inheritdoc cref="Remove(Snowflake)"/>
			public void Remove(Message msg) => Remove(msg.ID);

			/// <summary>
			/// Gets the time that the given message was pinned, or <see langword="null"/> if that message is not registered.
			/// </summary>
			/// <param name="messageId"></param>
			/// <returns></returns>
			public DateTimeOffset? Get(Snowflake messageId) {
				if (Times.TryGetValue(messageId, out DateTimeOffset time)) {
					return time;
				}
				return null;
			}

			/// <inheritdoc cref="Get(Snowflake)"/>
			public DateTimeOffset? Get(Message msg) => Get(msg.ID);

		}

	}
}
