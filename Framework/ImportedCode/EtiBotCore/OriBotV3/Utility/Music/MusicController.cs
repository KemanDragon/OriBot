#pragma warning disable CS4014
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Utility.Threading;
using EtiLogger.Logging;
using OldOriBot.Data;
using OldOriBot.Data.Commands;
using OldOriBot.Data.Persistence;
using OldOriBot.Interaction;
using OldOriBot.Utility.Formatting;
using OldOriBot.Utility.Music.FileRepresentation;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OldOriBot.Utility.Music {
	public class MusicController {

		private static Dictionary<VoiceChannel, MusicController> ControllerCache { get; } = new Dictionary<VoiceChannel, MusicController>();

		/// <summary>
		/// The size of the list that stores the recently played songs to avoid picking the same stuff over and over.
		/// </summary>
		private const int CACHE_SIZE = 10;

		/// <summary>
		/// Whether or not the system is currently runnning on the bot's VM.
		/// </summary>
		public static readonly bool IsOnVM = Environment.MachineName.ToLower() != "xan";

		public Logger MusicLog { get; } = new Logger("§3[MusicController] ");

		/// <summary>
		/// The directories that Ori music is stored in on Xan's computer.
		/// </summary>
		public MusicPool Pool {
			get {
				if (_Pool == null) {
					if (IsOnVM) {
						_Pool = new MusicPool(
							CACHE_SIZE,
							// BIG NOTE TO SELF: If you re-order these, you need to edit the ConfigUpdated method as it references these by integer index.
							// The VoteNext thing in CommandMusic is also hardcoded to reference trials by its index.
							new MusicDirectory(new DirectoryInfo(@"C:\OriBotMusic\Original\01 - Original Edition"), "bf", "Ori and the Blind Forest: Original Soundtrack"),
							new MusicDirectory(new DirectoryInfo(@"C:\OriBotMusic\Original\02 - Definitive Edition"), "bfextra", "Ori and the Blind Forest: Additional Soundtrack", ".flac"),
							new MusicDirectory(new DirectoryInfo(@"C:\OriBotMusic\WotW\OST"), "wotw", "Ori and the Will of the Wisps: Original Soundtrack",
								enabled: Configuration.TryGetType("UseWotW", false)),
							new MusicDirectory(new DirectoryInfo(@"C:\OriBotMusic\WotW\Trailers"), "wotwtrailers", "Ori and the Will of the Wisps: E3 Trailers"),
							new MusicDirectory(new DirectoryInfo(@"C:\OriBotMusic\WotW\Spirit Trials"), "trials", "Ori and the Will of the Wisps: Spirit Trials", 
								enabled: Configuration.TryGetType("UseWotWDataMined", false)),
							new MusicDirectory(new DirectoryInfo(@"C:\OriBotMusic\WotW\Extended"), "wotwextended", "Ori and the Will of the Wisps: Extended Soundtrack"),
							new MusicDirectory(new DirectoryInfo(@"C:\OriBotMusic\SkySaga"), "skysaga", "SkySaga: Unofficial Ripped Soundtrack", enabled: false),
							new MusicDirectory(new DirectoryInfo(@"C:\OriBotMusic\Caravan Palace"), "caravan", "Caravan Palace", enabled: false),
							new MusicDirectory(new DirectoryInfo(@"C:\OriBotMusic\Uncaged"), "uncaged", "Monstercat: Uncaged (Album Mixes)", enabled: false)
						);
					} else {
						_Pool = new MusicPool(
							CACHE_SIZE,
							// BIG NOTE TO SELF: If you re-order these, you need to edit the ConfigUpdated method as it references these by integer index.
							// The VoteNext thing in CommandMusic is also hardcoded to reference trials by its index.
							new MusicDirectory(new DirectoryInfo(@"V:\OriBotMusic\Original\01 - Original Edition"), "bf", "Ori and the Blind Forest: Original Soundtrack"),
							new MusicDirectory(new DirectoryInfo(@"V:\OriBotMusic\Original\02 - Definitive Edition"), "bfextra", "Ori and the Blind Forest: Additional Soundtrack", ".flac"),
							new MusicDirectory(new DirectoryInfo(@"V:\OriBotMusic\WotW\OST"), "wotw", "Ori and the Will of the Wisps: Original Soundtrack",
								enabled: Configuration.TryGetType("UseWotW", false)),
							new MusicDirectory(new DirectoryInfo(@"V:\OriBotMusic\WotW\Trailers"), "wotwtrailers", "Ori and the Will of the Wisps: E3 Trailers"),
							new MusicDirectory(new DirectoryInfo(@"V:\OriBotMusic\WotW\Spirit Trials"), "trials", "Ori and the Will of the Wisps: Spirit Trials",
								enabled: Configuration.TryGetType("UseWotWDataMined", false)),
							new MusicDirectory(new DirectoryInfo(@"V:\OriBotMusic\WotW\Extended"), "wotwextended", "Ori and the Will of the Wisps: Extended Soundtrack"),
							new MusicDirectory(new DirectoryInfo(@"V:\OriBotMusic\SkySaga"), "skysaga", "SkySaga: Unofficial Ripped Soundtrack", enabled: false),
							new MusicDirectory(new DirectoryInfo(@"V:\OriBotMusic\Caravan Palace"), "caravan", "Caravan Palace", enabled: false),
							new MusicDirectory(new DirectoryInfo(@"V:\OriBotMusic\Uncaged"), "uncaged", "Monstercat: Uncaged (Album Mixes)", enabled: false)
						);
					}
				}
				return _Pool;
			}
		}
		private MusicPool _Pool = null;

		/// <summary>
		/// The valid categories of music.
		/// </summary>
		public IReadOnlyList<string> ValidCategoryQueries {
			get {
				if (ValidQueries == null) {
					List<string> queries = new List<string>();
					MusicPool pool = Pool;
					foreach (MusicDirectory dir in pool.MusicDirectories) {
						queries.AddRange(dir.Identities);
					}

					ValidQueries = new List<string>();
					foreach (string dir in queries) {
						if (!ValidQueries.Contains(dir)) {
							ValidQueries.Add(dir);
						}
					}
				}

				return ValidQueries.AsReadOnly();
			}
		}
		private List<string> ValidQueries = null;

		// <summary>
		// A reference to the <see cref="BotContext"/> this <see cref="MusicController"/> exists for.
		// </summary>
		// private BotContext Context { get; }

		/// <summary>
		/// A reference to the <see cref="DataPersistence"/> for music.
		/// </summary>
		public static DataPersistence Configuration => DataPersistence.GetGlobalPersistence("music.cfg");

		/// <summary>
		/// A reference to the music voice channel.
		/// </summary>
		protected VoiceChannel DefaultMusicChannel { get; }

		/// <summary>
		/// The voice channel here will be joined instead of <see cref="DefaultMusicChannel"/>. This cannot be changed while playing.<para/>
		/// This will return <see cref="DefaultMusicChannel"/> by default. If explicitly set to <see langword="null"/>, it will behave as if it were set to <see cref="DefaultMusicChannel"/> instead (and as such, this can never be <see langword="null"/>)
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		public VoiceChannel EffectiveMusicChannel {
			get => _EffectiveMusicChannel ?? DefaultMusicChannel;
			set {
				if (Playing) throw new InvalidOperationException("Currently connected -- cannot change music channel. Stop the music and then change it.");
				_EffectiveMusicChannel = value;
			}
		}
		private VoiceChannel _EffectiveMusicChannel = null;

		/// <summary>
		/// A reference to #spirit-radio-chat, the text channel associated with the voice channel.
		/// </summary>
		public TextChannel MusicTextChannel { get; }

		/// <summary>
		/// The system controlling ongoing votes.
		/// </summary>
		public MusicVotingHelper VoteController { get; }

		/// <summary>
		/// Whether or not it's possible to connect to voice based on config.
		/// </summary>
		public bool IsSystemEnabled => Configuration.TryGetType("Enabled", true);

		/// <summary>
		/// Whether or not the system should autoconnect based on config.
		/// </summary>
		public bool AutoConnect => Configuration.TryGetType("ShouldAutoConnect", false);

		/// <summary>
		/// If <see langword="true"/>, the system will disconnect if the bot core drops connection too.
		/// </summary>
		public bool DisconnectOnCoreDisconnected => Configuration.TryGetType("DisconnectWhenCoreDisconnects", false);

		/// <summary>
		/// The current audio file.
		/// </summary>
		public MusicFile NowPlaying { get; private set; } = null;

		/// <summary>
		/// If set, this forces the next song. When this song starts playing, this property is reset to null.
		/// </summary>
		public FileInfo ForceNext { get; set; } = null;

		/// <summary>
		/// The number of people currently in the same channel as the bot. This count never includes the bot as a member (So if there's 5 members + the bot, this will be 5, NOT 6).
		/// </summary>
		public int ListenerCount => EffectiveMusicChannel.ConnectedMembers.Where(mbr => !mbr.IsSelf).Count();

		/// <summary>
		/// If <see langword="true"/>, the current audio file will finish transmitting, then the music system will disconnect.
		/// </summary>
		public bool StopAfterSong { get; private set; } = false;

		/// <summary>
		/// If <see langword="true"/>, the music loop will continue.
		/// </summary>
		public bool Playing { get; private set; } = false;

		/// <summary>
		/// If <see langword="true"/>, the system is working on translating something to opus, and should ignore input.
		/// </summary>
		public bool Encoding { get; private set; } = false;

		/// <summary>
		/// Whether or not the system is currently starting right now.
		/// </summary>
		public bool Starting { get; private set; } = false;

		/// <summary>
		/// A provider of <see cref="CancellationToken"/>s that can be used to stop music transmission.
		/// </summary>
		private ReusableCancellationTokenSource MusicCanceller { get; } = new ReusableCancellationTokenSource();

		/// <summary>
		/// A message used for encoding progress.
		/// </summary>
		private Message StatusReportMessage { get; set; } = null;

		private ChattyProgressBar DurationBar { get; } = new ChattyProgressBar(36) {
			ShowPercentage = false
		};

		/// <summary>
		/// Initialize a <see cref="MusicController"/> with the given music voice channel and text channel to relay music in.
		/// </summary>
		/// <param name="musicChannel"></param>
		/// <param name="textChannelForMusic"></param>
		private MusicController(VoiceChannel musicChannel, TextChannel textChannelForMusic) {
			MusicLog.WriteLine(Environment.MachineName);
			DefaultMusicChannel = musicChannel;
			MusicTextChannel = textChannelForMusic;
			VoteController = new MusicVotingHelper(this);
			ChattyProgressBar pbar = new ChattyProgressBar(32);
			DiscordClient.Current!.Events.VoiceStateEvents.OnVoiceStateChanged += OnVoiceStateChanged;
		}

		private Task OnVoiceStateChanged(VoiceState old, VoiceState state, Snowflake? guildId, Snowflake channelId) {
			if (state.UserID == User.BotUser.ID) {
				if (state.IsConnectedToVoice && Playing) {
					if (state.ChannelID != EffectiveMusicChannel.ID) {
						Stop();
						return Task.CompletedTask;
					}
				}
			}
			if (ListenerCount == 0 && Playing) {
				Stop();
			} else if (ListenerCount > 0 && !Playing && AutoConnect) {
				if (!IsSystemEnabled) return Task.CompletedTask;
				PlayAudio();
			}
			return Task.CompletedTask;
		}

		/// <summary>
		/// Gets an existing <see cref="MusicController"/> for the given <paramref name="musicChannel"/>, or creates and registers a new one.
		/// </summary>
		/// <param name="musicChannel"></param>
		/// <param name="textChannel"></param>
		/// <returns></returns>
		public static MusicController GetOrCreate(VoiceChannel musicChannel, TextChannel textChannel) {
			if (ControllerCache.TryGetValue(musicChannel, out MusicController ctr)) {
				return ctr;
			}
			ControllerCache[musicChannel] = new MusicController(musicChannel, textChannel);
			return ControllerCache[musicChannel];
		}

		/// <summary>
		/// Stops all live <see cref="MusicController"/>s.
		/// </summary>
		public static async Task StopAll() {
			foreach (MusicController ctr in ControllerCache.Values) {
				if (ctr.Playing) await ctr.Stop();
			}
		}

		/// <summary>
		/// Moves the bot to a different voice channel, but resets the voice stream.
		/// </summary>
		/// <param name="newChannel"></param>
		public async Task MigrateTo(VoiceChannel newChannel) {
			if (Playing) {
				await Stop(false, false);
			}
			_EffectiveMusicChannel = newChannel;
			await Task.Delay(200);
			PlayAudio();
		}

		/// <summary>
		/// Stops this <see cref="MusicController"/> and disconnects the <see cref="VoiceNextConnection"/>
		/// </summary>
		/// <param name="onNext">If true, this will wait until the current song is done before stopping it.</param>
		public async Task Stop(bool onNext = false, bool resetChannel = true) {
			if (onNext) {
				StopAfterSong = true;
			} else {
				Playing = false;
				VoteController.ClearVotes();
				MusicCanceller.Cancel();
				await EffectiveMusicChannel.DisconnectAsync();
				if (resetChannel) {
					ResetControllerState();
				}
			}
		}

		internal void ResetControllerState() {
			Starting = false;
			_EffectiveMusicChannel = null;
		}

		/// <summary>
		/// Cancels the current ongoing playback task and starts the next.
		/// </summary>
		public Task Skip() {
			VoteController.ClearVotes();
			MusicCanceller.Cancel();
			return Task.Delay(500);
		}

		/// <summary>
		/// Disconnects the bot from the music channel and reconnects it.
		/// </summary>
		public async Task Reinitialize() {
			await Stop(false, false);
			await Task.Delay(200);
			PlayAudio();
		}

		/// <summary>
		/// Returns a formatted variation of <see cref="NowPlaying"/> that displays where to get the song.
		/// </summary>
		/// <returns></returns>
		public Embed GetFormattedNowPlaying(bool justLength = true, bool verboseDir = false) {
			EmbedBuilder builder = new EmbedBuilder {
				Title = "Now Playing"
			};
			if (NowPlaying != null) {
				builder.Description = $"**{NowPlaying.Metadata.Title}**";

				if (justLength) {
					//builder.AddField("Track Length", GetSimpleTime(DiscordClient.Current.MusicLength));
				} else {
				//	builder.AddField("Track Progress", GetSimpleTime(marshaller.CurrentMusicTime, marshaller.MusicLength) + "\n" + DurationBar.GetProgressBarText(marshaller.CurrentMusicTime.TotalMilliseconds / marshaller.MusicLength.TotalMilliseconds));
				}

				if (NowPlaying.ParentMusicDirectory != null) {
					builder.AddField("Source", NowPlaying.ParentMusicDirectory.Source);
					builder.AddField("Download", NowPlaying.ParentMusicDirectory.Download);
				} else {
					builder.AddField("Source", "Local File System // Undocumented");
					builder.AddField("Download", "N/A");
				}

				if (NowPlaying.Metadata.ExtraNotes != null) {
					builder.AddField("Extra Notes", NowPlaying.Metadata.ExtraNotes);
				}
				
				if (verboseDir) {
					builder.AddField("Source (Local File System)", NowPlaying.File.FullName);
				}
				return builder.Build();
			} else {
				builder.Description = "Nothing!";
				return builder.Build();
			}
		}

		private string GetSimpleTime(TimeSpan current, TimeSpan duration) {
			bool useHours = current.TotalHours > 0 || duration.TotalHours > 0;
			if (useHours) {
				return $"{current.Hours:D2}:{current.Minutes:D2}:{current.Seconds:D2} / {duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
			}
			return $"{current.Minutes:D2}:{current.Seconds:D2} / {duration.Minutes:D2}:{duration.Seconds:D2}";
		}

		/// <summary>
		/// Play the given <see cref="MusicFile"/>. This also starts the selection loop, which will continue until canceled with <see cref="Stop"/>
		/// </summary>
		/// <param name="music"></param>
		/// <returns></returns>
		public Task PlayAudio(MusicFile music) {
			return PlayAudio(music.File);
		}

		/// <summary>
		/// Play audio with the given <see cref="FileInfo"/> for a specific music file, or null to randomly select. This also starts the selection loop, which will continue until canceled with <see cref="Stop"/><para/>
		/// <strong>Do not await this loop unless you need to yield until music stops.</strong>
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public async Task PlayAudio(FileInfo file = null) {
			if (!IsSystemEnabled) {
				MusicLog.WriteLine("PlayAudio called, but the system is disabled, so the call has been aborted.", LogLevel.Trace);
				return;
			}
			if (Starting) {
				MusicLog.WriteCritical("Already doing a start cycle!");
				return;
			}

			await EffectiveMusicChannel.DisconnectAsync();

			Starting = true;
			ForceNext = file;
			if (ForceNext != null) MusicLog.WriteLine("ForceNext is: " + ForceNext.ToString(), LogLevel.Debug);
			MusicLog.WriteLine("Connecting...", LogLevel.Trace);
			await EffectiveMusicChannel.ConnectAsync();
			await Task.Delay(500);

			MusicLog.WriteLine("Preparing state...", LogLevel.Trace);
			Playing = true;
			StopAfterSong = false;
			Encoding = false;
			Starting = false;
			VoteController.ClearVotes();
			while (Playing) {
				if (!DiscordClient.Current.Connected && DisconnectOnCoreDisconnected) {
					MusicLog.WriteLine("Core is no longer connected! Terminating.", LogLevel.Trace);
					Playing = false;
					break;
				}
				if (!IsSystemEnabled) {
					MusicLog.WriteLine("System was disabled! Terminating.", LogLevel.Trace);
					Playing = false;
					break;
				}
				try {
					if (ForceNext != null) {
						NowPlaying = Pool.FindOrCreateNew(ForceNext);
					} else {
						NowPlaying = Pool.GetRandomFileFromPool();
					}
					if (NowPlaying == null) throw new FileNotFoundException("I can't find *any* music files!");
					ForceNext = null;

					await MusicTextChannel.SendMessageAsync("Remember to use `>> help music` if you haven't already!", GetFormattedNowPlaying(), AllowedMentions.AllowNothing);
					Encoding = true;
				//	List<byte[]> rawOpusPackets = TransmitHelper.Encode(NowPlaying.File);
				//	Encoding = false;
				//	await VoiceConnectionMarshaller.Current.TransmitAll(rawOpusPackets, MusicCanceller.CurrentToken);
					if (StopAfterSong) {
						EffectiveMusicChannel.DisconnectAsync();
						Playing = false;
						break;
					}
				} catch (OperationCanceledException) {
				} catch (Exception exc) {
					EffectiveMusicChannel.DisconnectAsync();
					Playing = false;
					await CommandMarshaller.PostExceptionEmbedInChannel(MusicTextChannel, exc);
					MusicLog.WriteException(exc);
					break;
				}

				VoteController.ClearVotes();
			}
		}
	}
}
