using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Threading;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using EtiLogger.Logging;
using Newtonsoft.Json;

using EtiBotCore.Utility.Extension;
using System.Linq;

using System.Threading;
using System.IO;
using System.Diagnostics;
using EtiBotCore.Data.Net;
using EtiBotCore.Exceptions;
using Newtonsoft.Json.Linq;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Factory;
using EtiLogger.Data.Structs;
using EtiBotCore.DiscordObjects.Universal.Data;

using VoiceStatePayload = EtiBotCore.Payloads.PayloadObjects.VoiceState;
using VoiceState = EtiBotCore.DiscordObjects.Universal.VoiceState;

namespace EtiBotCore.Voice {

	/// <summary>
	/// A utility class that assists in creating, maintaining, and utilizing a connection to a voice channel.
	/// </summary>
	public sealed class VoiceConnectionMarshaller : IDisposable, IAsyncDisposable {

		#region Static Core Data

		/// <summary>
		/// The log associated with the system.
		/// </summary>
		private static readonly Logger Log = new Logger(new LogMessage.MessageComponent("[Voice Connection Marshaller] ", new EtiLogger.Data.Structs.Color(0x8BADAA)));

		/// <summary>
		/// The current voice gateway version.
		/// </summary>
		private const int VOICE_GATEWAY_VERSION = 4;

		/// <summary>
		/// This machine's external IP.
		/// </summary>
		private static IPAddress MyExternalIP {
			get {
				if (_MyExternalIP == null) {
					string externalip = new WebClient().DownloadString("http://icanhazip.com"); // TODO: Not rely on someone else's service.
					_MyExternalIP = IPAddress.Parse(externalip.Trim());
				}
				return _MyExternalIP;
			}
		}
		private static IPAddress? _MyExternalIP = null;

		/// <summary>
		/// The current active <see cref="VoiceConnectionMarshaller"/>.
		/// </summary>
		public static VoiceConnectionMarshaller? Current { get { return null; } }

		#endregion

		#region Connection Data

		/// <summary>
		/// Whether or not the voice socket is connected.
		/// </summary>
		public bool Connected => VoiceSocket.State == WebSocketState.Open;

		/// <summary>
		/// The socket used to maintain voice connections.
		/// </summary>
		private ClientWebSocket VoiceSocket { get; set; } = new ClientWebSocket();

		/// <summary>
		/// The actual UDP client used to send and receive voice packets.
		/// </summary>
		private UdpClient VoiceClient { get; } = new UdpClient();

		/// <summary>
		/// A supplier for voice cancellation tokens.
		/// </summary>
		private readonly ReusableCancellationTokenSource VoiceTokenSource = new ReusableCancellationTokenSource();

		/// <summary>
		/// The current SSRC
		/// </summary>
		private uint SSRC { get; set; }

		/// <summary>
		/// The IP of Discord's voice gateway right now.
		/// </summary>
		private string? TargetIP { get; set; }

		/// <summary>
		/// The port of Discord's voice gateway right now.
		/// </summary>
		private ushort? TargetPort { get; set; }

		/// <summary>
		/// The current secret key.
		/// </summary>
		private byte[]? SecretKey { get; set; }

		/// <summary>
		/// The current session's ID. Used only for resuming.
		/// </summary>
		private string? CurrentSessionID { get; set; }

		/// <summary>
		/// The current session's token. Used only for resuming.
		/// </summary>
		private string? CurrentToken { get; set; }

		/// <summary>
		/// The endpoint that Discord will send and receive data to and from. A <see langword="null"/> endpoint means that the voice server allocated has gone away and is trying to be reallocated. You should attempt to disconnect from the currently connected voice server, and not attempt to reconnect until a new voice server is allocated.
		/// </summary>
		private string? VoiceEndpoint { get; set; }

		/// <summary>
		/// A sequence number offset for when the stream nukes itself and has to restart.
		/// </summary>
		private int SequenceNumberOffset = 0;

		#endregion

		/// <summary>
		/// Whether or not the system has suffered a problem that requires it to be recreated.
		/// </summary>
		public bool Faulted { get; private set; }

		/// <summary>
		/// Whether or not to pause transmission.
		/// </summary>
		public bool Paused { get; set; } = false;

		/// <summary>
		/// If true, the next voice packet will emulate a connection failure.
		/// </summary>
		private bool EmulateFailure { get; set; } = false;

		/// <summary>
		/// The channel this <see cref="VoiceConnectionMarshaller"/> exists for.
		/// </summary>
		public VoiceChannel Channel { get; }

		/// <summary>
		/// The channel used for music-related information.
		/// </summary>
		public TextChannel? RadioTextChannel { get; set; }
		
		/// <summary>
		/// The current timestamp of the song.
		/// </summary>
		public TimeSpan CurrentMusicTime { get; private set; }

		/// <summary>
		/// The length of the song.
		/// </summary>
		public TimeSpan MusicLength { get; private set; }

		/// <summary>
		/// A method used to stop the music from the MusicController.
		/// </summary>
		public Action? MusicControllerReset { get; set; }

		/// <summary>
		/// Disconnects <see cref="Current"/> if it is not <see langword="null"/>.
		/// </summary>
		/// <returns></returns>
		public static async Task DisconnectIfExists() {
			if (Current != null) {
				try { await Current.DisposeAsync(); } catch { }
				Current = null;
			}
		}

		/// <summary>
		/// Simulates a network failure.
		/// </summary>
		public static void FalseFail() {
			if (Current != null) Current.EmulateFailure = true;
		}

		/// <summary>
		/// Construct a new <see cref="VoiceConnectionMarshaller"/> in the given channel. This will set up all necessary data and prepare the system for connection.
		/// </summary>
		/// <param name="inChannel">The Spirit Radio voice channel.</param>
		/// <param name="radioTextChannel">The #spirit-radio-chat channel.</param>
		public VoiceConnectionMarshaller(VoiceChannel inChannel, TextChannel? radioTextChannel = null) {
			Current = this;
			Channel = inChannel;
			RadioTextChannel = radioTextChannel;
		}

		/// <summary>
		/// Initiates the connection to Discord. This will yield until Discord replies with both necessary events. Propagates any exceptions that occur, but also logs them.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="AggregateException"></exception>
		public async Task InitializeConnectionAsync() {
			VoiceTokenSource.Cancel(); // Terminate any stragglers.

			// Start by telling Discord we want to connect to the given channel.
			DiscordClient current = DiscordClient.Current!;

			// Just clean this up since Reconnect calls it.
			try { VoiceSocket.Dispose(); } catch { }
			// try { VoiceClient.Dispose(); } catch { }

			Payload tellDiscordNewVoiceState = new Payload {
				Operation = PayloadOpcode.VoiceStateUpdate,
				Data = new VoiceStatePayload {
					GuildID = Channel.ServerID,
					ChannelID = Channel.ID,
					Deafened = true,
					Muted = false
				}
			};
			await current.Send(tellDiscordNewVoiceState);

			ValueTuple<VoiceState, VoiceState, Snowflake?, Snowflake> voiceStateInfo = default;
			ValueTuple<Snowflake, string, string?> voiceServerInfo = default;
			
			try {
				Task state = Task.Run(async () => {
					do {
						voiceStateInfo = await current.Events.VoiceStateEvents.OnVoiceStateChanged.Wait();
					} while (voiceStateInfo.Item4 != Channel.ID);
				});
				Task server = Task.Run(async () => {
					do {
						voiceServerInfo = await current.Events.PassthroughEvents.OnVoiceServerUpdated.Wait();
					} while (voiceServerInfo.Item1 != Channel.ServerID);
				});
				Task.WaitAll(state, server);
			} catch (Exception exc) {
				Faulted = true;
				Log.WriteException(exc);
				throw;
			}
#pragma warning disable CS8600 // Nullable garbage. I know it's not null here.
			(VoiceState oldState, VoiceState newState, Snowflake? _, Snowflake channelId) = voiceStateInfo;
			(Snowflake serverId, string token, string? endpoint) = voiceServerInfo;

			VoiceEndpoint = endpoint;

#pragma warning restore CS8600

			CurrentSessionID = newState!.SessionID;
			CurrentToken = token!;

			Payload identifyYourself = new Payload {
				Operation = PayloadOpcode.Dispatch, // 0
				Data = new VoiceIdentifyPayload {
					ServerID = serverId,
					UserID = User.BotUser.ID,
					SessionID = CurrentSessionID,
					Token = token!
				}
			};

			string socketUrl = $"wss://{VoiceEndpoint}?v={VOICE_GATEWAY_VERSION}";
			VoiceSocket = new ClientWebSocket();
			await VoiceSocket.ConnectAsync(new Uri(socketUrl), VoiceTokenSource.CurrentToken);

			Log.WriteLine("Identifying voice...", LogLevel.Trace);
			// IDENTIFY TO GATEWAY
			await SendVoice(identifyYourself);

			Task hello = EtiTaskExtensions.Launch(async () => {
				while (true) {
					Payload? helloPl = await ReceiveVoice();
					if (helloPl == null || helloPl.Operation != PayloadOpcode.VoiceHello) continue;

					StartVoiceHeartbeatTask(((JObject)helloPl!.Data!).GetValue("heartbeat_interval")!.ToObject<int>());
					break;
				}
			}, Log, VoiceTokenSource);
			await hello;
			if (hello.IsFaulted) return;

			VoiceReadyPayload? ready = null;
			Task readyUp = EtiTaskExtensions.Launch(async () => {
				while (true) {
					Payload? readyPl = await ReceiveVoice();
					if (readyPl == null || readyPl.Operation != PayloadOpcode.Identify) continue;

					ready = readyPl.GetObjectFromData<VoiceReadyPayload>();
					SSRC = ready.SSRC;
					Payload selectProtoPl = new Payload {
						Operation = PayloadOpcode.SelectProtocol,
						Data = new SelectProtocolPayload {
							Protocol = "udp",
							Data = new SelectProtocolPayload.SelectProtocolData {
								IP = MyExternalIP.ToString(),
								Port = ready.Port,
								Mode = "xsalsa20_poly1305"
							}
						}
					};

					Log.WriteLine("Selecting protocol...", LogLevel.Trace);
					await SendVoice(selectProtoPl);
					break;
				}
			}, Log, VoiceTokenSource);
			await readyUp;
			if (readyUp.IsFaulted) return;

			Task getProtocol = EtiTaskExtensions.Launch(async () => {
				while (true) {
					Payload? sessionPl = await ReceiveVoice();
					if ((int)sessionPl!.Operation != 4) continue;

					ReceiveProtocolPayload recv = sessionPl!.GetObjectFromData<ReceiveProtocolPayload>();

					TargetIP = ready!.IP;
					TargetPort = ready.Port;
					SecretKey = recv.SecretKey;

					Log.WriteLine("Done. Starting voice receive task.", LogLevel.Trace);
					break;
				}
			}, Log, VoiceTokenSource);
			await getProtocol;
			if (getProtocol.IsFaulted) return;

			StartVoiceReceiveTask();
		}

		/// <inheritdoc/>
		public void Dispose() {
			DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();
		}

		/// <inheritdoc/>
		public async ValueTask DisposeAsync() {
			try {
				MusicControllerReset?.Invoke();
				Current = null;
				VoiceTokenSource.Cancel();
				try {
					await VoiceSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Transmission completed.", CancellationToken.None);
					VoiceSocket.Dispose();
				} catch { }
				try {
					VoiceClient.Dispose();
				} catch { }

				CurrentSessionID = null;

				Payload pl = new Payload {
					Operation = PayloadOpcode.VoiceStateUpdate,
					Data = new VoiceStatePayload {
						GuildID = Channel.ServerID,
						ChannelID = null,
						Deafened = false,
						Muted = false
					}
				};
				await DiscordClient.Current!.Send(pl, true); // Sends over the standard client socket.
			} catch (Exception exc) {
				Log.WriteException(exc);
			}
		}


		#region Send Voice

		/// <summary>
		/// Send the entire audio file. Returns a <see cref="Task"/> that runs until the transmission has completed. This will not disconnect the transmission.
		/// </summary>
		/// <param name="rawOpusPackets">An array of opus packets acquired from <see cref="TransmitHelper"/>.</param>
		/// <param name="token">A token that can be used to stop transmission.</param>
		/// <returns></returns>
		public Task TransmitAll(List<byte[]?> rawOpusPackets, CancellationToken token) {
			return Task.Run(async () => {
				SequenceNumberOffset = 0;
				await SendSpeaking(true, true);

				Stopwatch sw = new Stopwatch();
				int durationMillis = TransmitHelper.DELAY_TIME_NO_PHASE * rawOpusPackets.Count;
				int currentMillis = 0;
				MusicLength = TimeSpan.FromMilliseconds(durationMillis);

				#region Silence
				List<byte[]?> silencePackets;
				if (Directory.Exists("V:\\")) {
					silencePackets = TransmitHelper.Encode(new FileInfo(@"V:\OriBotMusic\_SILENCE.mp3"), true);
				} else {
					silencePackets = TransmitHelper.Encode(new FileInfo(@"C:\OriBotMusic\_SILENCE.mp3"), true);
				}

				for (int i = 0; i < silencePackets.Count; i++) {
					if (!Connected) break;
					if (token.IsCancellationRequested) break;

					try { 
						byte[] hdr = ConstructRawOpusPacket(silencePackets, i - SequenceNumberOffset);
						VoiceClient.Send(hdr, hdr.Length, TargetIP!, TargetPort!.Value);
						if (EmulateFailure) {
							EmulateFailure = false;
							throw new SocketException(-1);
						}
					} catch (SocketException exc) {
						// SequenceNumberOffset = -i;
						Log.WriteException(exc);
						Log.WriteLine("Failed to send a silence packet. Trying to reconnect.");
						Paused = true;
						bool success = await TryResume();
						Paused = false;
						if (!success) return;
						await SendSpeaking(true, true);
					}

					sw.Start();
					while (sw.ElapsedMilliseconds < TransmitHelper.DELAY_TIME_NO_PHASE) {
						System.Threading.Thread.SpinWait(500);
						if (!Connected) break;
						if (token.IsCancellationRequested) break;
					}
					while (Paused) {
						System.Threading.Thread.SpinWait(500);
						if (!Connected) break;
						if (token.IsCancellationRequested) break;
					}
					sw.Reset();
				}
				#endregion

				if (!Connected) return;
				if (token.IsCancellationRequested) return;

				#region Actual Music
				for (int i = 0; i < rawOpusPackets.Count; i++) {
					if (!Connected) break;
					if (token.IsCancellationRequested) break;

					try {
						byte[] hdr = ConstructRawOpusPacket(rawOpusPackets, i + silencePackets.Count - SequenceNumberOffset);
						VoiceClient.Send(hdr, hdr.Length, TargetIP!, TargetPort!.Value);
						if (EmulateFailure) {
							EmulateFailure = false;
							throw new SocketException(-1);
						}
					} catch (SocketException exc) {
						// SequenceNumberOffset = -(i + silencePackets.Count);
						Log.WriteException(exc);
						Log.WriteLine("Failed to send a music packet. Trying to reconnect.");
						Paused = true;
						bool success = await TryResume();
						Paused = false;
						if (!success) return;
						await SendSpeaking(true, true);
					}

					if (i % TransmitHelper.PACKETS_PER_DELAY_PHASE == 0 && i > 0) {
						sw.Start();
						while (sw.ElapsedMilliseconds < TransmitHelper.DELAY_TIME) {
							System.Threading.Thread.SpinWait(500);
							if (!Connected) break;
							if (token.IsCancellationRequested) break;
						}
						sw.Reset();
					}
					while (Paused) {
						System.Threading.Thread.SpinWait(500);
						if (!Connected) break;
						if (token.IsCancellationRequested) break;
					}
					currentMillis += TransmitHelper.DELAY_TIME_NO_PHASE;
					CurrentMusicTime = TimeSpan.FromMilliseconds(currentMillis);

					rawOpusPackets[i] = null; // not using this anymore
				}

				#endregion

				if (!Connected) return;
				if (token.IsCancellationRequested) return;

				Debug.WriteLine("Done transmitting that.");
				await SendSpeaking(false);
			}, token);
		}

		/// <summary>
		/// Constructs an opus packet from the given packet array and sequence number.
		/// </summary>
		/// <param name="packets"></param>
		/// <param name="sequenceNumber"></param>
		/// <returns></returns>
		private byte[] ConstructRawOpusPacket(List<byte[]?> packets, int sequenceNumber) {
			int fakeSequenceNumber = sequenceNumber;
			while (fakeSequenceNumber > ushort.MaxValue) fakeSequenceNumber -= ushort.MaxValue;

			ushort sequence = (ushort)fakeSequenceNumber;
			byte[] opusPacket = packets[sequenceNumber]!;
			List<byte> header = new List<byte>() { 0x80, 0x78 };
			header.AddRange(sequence.ToBigEndian());
			header.AddRange(((uint)(sequenceNumber * TransmitHelper.PAGE_LENGTH)).ToBigEndian());
			header.AddRange(SSRC.ToBigEndian());
			List<byte> headerRaw = header.ToList();
			headerRaw.AddRange(new byte[12]);
			byte[] nonce = headerRaw.ToArray();
			byte[] encryptedAudioData = SodiumWrapper.Encrypt(opusPacket, nonce, SecretKey);
			header.AddRange(encryptedAudioData);

			packets[sequenceNumber] = null; // Dispose of the data we don't need anymore.
			return header.ToArray();
		}

		#endregion

		#region Send & Receive Payloads

		/// <summary>
		/// Sends the given <see cref="Payload"/> to Discord. This is strictly for voice connections and uses a different socket.
		/// </summary>
		/// <param name="payload">The <see cref="Payload"/> to send.</param>
		/// <returns></returns>
		private async Task SendVoice(Payload payload) {
			try {
				await VoiceSocket.SendBytesAsync(payload.ToJsonBytes(), VoiceTokenSource.CurrentToken);
			} catch (OperationCanceledException) { }
		}

		/// <summary>
		/// Recieves a <see cref="Payload"/> from Discord's voice connection and returns it.
		/// </summary>
		/// <returns></returns>
		private async Task<Payload?> ReceiveVoice() {
			try {
				if (!Connected) return null;
				(byte[] bytes, WebSocketReceiveResult? result) = await VoiceSocket.ReceiveBytesAndResultAsync(CancellationToken.None);
				Payload payload = CompressionController.DecompressIntoPayload(bytes, result?.MessageType == WebSocketMessageType.Binary);
				if (payload == null) return null;
				return payload;
			} catch (OperationCanceledException) {
			} catch (WebSocketException webExc) {
				Log.WriteException(webExc);
				Paused = true;
				//bool success = await TryResume();
				await TryResume(); // If this fails, we'll get another error and it'll go below.
				Paused = false;
			} catch (WebSocketErroredException sockExc) {
				Log.WriteException(sockExc);
				if (sockExc.Code == (int)DiscordGatewayEventCode.NotAuthorizedToUseIntentOrVoiceDisconnected) {
					Task? t = RadioTextChannel?.SendMessageAsync(null, EmbedForDisconnection, AllowedMentions.AllowNothing);
					if (t != null) await t;
					Faulted = true;
					await TryDeepReconnect();
					return null;
				} else if (sockExc.Code == (int)DiscordGatewayEventCode.SessionNoLongerValid) {
					Task? t = RadioTextChannel?.SendMessageAsync(null, EmbedForInvalidSession, AllowedMentions.AllowNothing);
					if (t != null) await t;
					Faulted = true;
					await TryDeepReconnect();
					return null;
				}

				if (Enum.IsDefined(typeof(DiscordGatewayEventCode), sockExc.Code)) {
					Paused = true;
					bool success = await TryDeepReconnect();
					Paused = false;
					if (!success) await DisposeAsync();
				} else {
					Paused = true;
					await TryResume(); // If this fails, we'll get another error and it'll come here.
					Paused = false;
				}
			} catch (ObjectDisposedException disposed) {
				Log.WriteException(disposed);
				VoiceTokenSource.Cancel();
				return null;
			} catch (Exception genericExc) {
				Log.WriteException(genericExc);
				await DisposeAsync();
			}
			return null;
		}

		private async Task DisconnectAsyncInternal() {
			VoiceTokenSource.Cancel();
			try {
				await VoiceSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Transmission completed.", CancellationToken.None);
				VoiceSocket.Dispose();
			} catch { }
		}

		private async Task<bool> TryDeepReconnect() {
			Log.WriteLine("Trying to restart the connection completely...");
			try {
				await DisconnectAsyncInternal();
				await InitializeConnectionAsync();
				Log.WriteLine("Successfully reconnected.");
				return true;
			} catch (Exception) {
				Log.WriteLine("Failed to reconnect.");
				return false;
			}
			/*
			Log.WriteLine("But this has been removed.");
			return false;
			*/
		}

		private Task<bool> TryResume() {
			return EtiTaskExtensions.Launch(async () => {
				Log.WriteLine("Trying to resume...");
				try {
					string socketUrl = $"wss://{VoiceEndpoint}?v={VOICE_GATEWAY_VERSION}";
					VoiceSocket = new ClientWebSocket();
					await VoiceSocket.ConnectAsync(new Uri(socketUrl), VoiceTokenSource.CurrentToken);
					VoiceIdentifyPayload reID = new VoiceIdentifyPayload {
						ServerID = Channel.ServerID,
						SessionID = CurrentSessionID!,
						Token = CurrentToken!
					};
					await SendVoice(new Payload {
						Operation = PayloadOpcode.VoiceResume,
						Data = reID
					});
					while (true) {
						Payload? pl = await ReceiveVoice();
						if (pl == null) {
							Log.WriteLine("Failed to resume due to a null payload in response. Performing complete reconnection.");
							await TryDeepReconnect();
							return false;
						}
						if (pl != null && pl.Operation == PayloadOpcode.VoiceResumed) {
							break;
						}
					}
					Log.WriteLine("Resumed!");
				} catch (Exception exc) {
					Log.WriteLine("Failed to resume due to an exception. Performing complete reconnection.");
					Log.WriteException(exc);
					await TryDeepReconnect();
					return false;
				}
				return true;
			}, Log, VoiceTokenSource);
		}

		/// <summary>
		/// Sends a heartbeat when called.
		/// </summary>
		/// <returns></returns>
		private Task SendVoiceHeartbeat() {
			Log.WriteLine("Voice heartbeat sent.", LogLevel.Trace);
			return SendVoice(new Payload {
				Operation = PayloadOpcode.VoiceHeartbeat,
				Data = DiscordClient.Current!.LastReceivedSequenceNumber!
			});
		}

		/// <summary>
		/// Begins the heartbeat loop.
		/// </summary>
		/// <returns></returns>
		private void StartVoiceHeartbeatTask(int heartbeatInterval) {
			try {
				EtiTaskExtensions.Launch(async () => {
					while (Connected) {
						await SendVoiceHeartbeat();
						await Task.Delay(heartbeatInterval, VoiceTokenSource.CurrentToken);
					}
				}, Log, VoiceTokenSource);
			} catch (OperationCanceledException) {
			} catch (Exception exc) {
				Log.WriteException(exc);
			}
		}

		private void StartVoiceReceiveTask() {
			try {
				EtiTaskExtensions.Launch(async () => {
					while (Connected) {
						Payload? nextPayload = await ReceiveVoice();
						if (nextPayload == null) {
							await DisposeAsync();
							return;
						}
						if (nextPayload.Operation == PayloadOpcode.VoiceHeartbeatAcknowledged) {
							Log.WriteLine("Voice heartbeat acknowledged. You haven't lost your voice. Congratulations.", LogLevel.Trace);
						}
					}
				}, Log, VoiceTokenSource);
			} catch (OperationCanceledException) {
			} catch (Exception exc) {
				Log.WriteException(exc);
			}
		}

		/// <summary>
		/// Update the speaking status of the bot.
		/// </summary>
		/// <param name="speaking">If true, the bot will be signified as speaking.</param>
		/// <param name="priority">If true, the bot will also lower the volume of other members.</param>
		/// <returns></returns>
		private Task SendSpeaking(bool speaking, bool priority = false) {
			int speakCode = 0;
			if (speaking) speakCode |= 1 << 0;
			// 1 << 1 is soundshare which doesn't work for this context, it requires a stream
			if (priority) speakCode |= 1 << 2;
			return SendVoice(new Payload() {
				Operation = PayloadOpcode.Speaking,
				Data = new SpeakingPayload {
					Speaking = speakCode,
					SSRC = SSRC
				}
			});
		}


		#endregion

		/// <summary>
		/// An embed for when error 4014 (Disconnected) is sent.
		/// </summary>
		private Embed EmbedForDisconnection {
			get {
				if (_EmbedForDisconnection == null) {
					EmbedBuilder builder = new EmbedBuilder {
						Title = "Voice System Disconnected",
						Description = "Discord relayed [Error 4014 :: Disconnected] // \"Channel was deleted, you were kicked, voice server changed, or the main gateway session was dropped. Should not reconnect. Issue a new connection instead.\"",
						Color = Color.DARK_RED
					};
					_EmbedForDisconnection = builder.Build();
				}
				return _EmbedForDisconnection;
			}
		}
		private Embed? _EmbedForDisconnection = null;

		/// <summary>
		/// An embed for when error 4006 (Session no longer valid) is sent.
		/// </summary>
		private Embed EmbedForInvalidSession {
			get {
				if (_EmbedForInvalidSession == null) {
					EmbedBuilder builder = new EmbedBuilder {
						Title = "Voice System Session Invalid",
						Description = "Discord relayed [Error 4006 :: Session no longer valid] // \"Your session is no longer valid. Please reconnect a new socket and perform the complete connection handshake again.\"",
						Color = Color.DARK_RED
					};
					_EmbedForInvalidSession = builder.Build();
				}
				return _EmbedForInvalidSession;
			}
		}
		private Embed? _EmbedForInvalidSession = null;

		private class VoiceIdentifyPayload {

			/// <summary>
			/// The ID of the server.
			/// </summary>
			[JsonProperty("server_id")]
			public ulong ServerID { get; set; }

			/// <summary>
			/// The ID of the bot user.
			/// </summary>
			[JsonProperty("user_id")]
			public ulong UserID { get; set; }

			/// <summary>
			/// The ID of the voice session.
			/// </summary>
			[JsonProperty("session_id")]
			public string SessionID { get; set; } = string.Empty;

			/// <summary>
			/// The voice connection's token.
			/// </summary>
			[JsonProperty("token")]
			public string Token { get; set; } = string.Empty;

		}

		private class VoiceReadyPayload {

			/// <summary>
			/// da magic numba or something idk
			/// </summary>
			[JsonProperty("ssrc")]
			public uint SSRC { get; set; }

			/// <summary>
			/// The IP Address of the server.
			/// </summary>
			[JsonProperty("ip")]
			public string IP { get; set; } = string.Empty;

			/// <summary>
			/// The port this connection is on.
			/// </summary>
			[JsonProperty("port")]
			public ushort Port { get; set; }

			/// <summary>
			/// The valid modes of encryption that can be used.
			/// </summary>
			[JsonProperty("modes")]
			public string[] Modes { get; set; } = new string[0];

			/// <summary>
			/// This is an erroneous field.
			/// </summary>
#pragma warning disable CS0169
#pragma warning disable IDE0051
			private int heartbeat_interval;
#pragma warning restore IDE0051
#pragma warning restore CS0169
		}

		private class SelectProtocolPayload {

			/// <summary>
			/// The protocol used when connecting.
			/// </summary>
			[JsonProperty("protocol")]
			public string Protocol { get; set; } = "udp";

			/// <summary>
			/// Information about this connection.
			/// </summary>
			[JsonProperty("data")]
			public SelectProtocolData? Data { get; set; }

			public class SelectProtocolData {

				/// <summary>
				/// The IP address of my machine that Discord will send data to.
				/// </summary>
				[JsonProperty("address")]
				public string IP { get; set; } = string.Empty;

				/// <summary>
				/// The port that Discord will send data to.
				/// </summary>
				[JsonProperty("port")]
				public ushort Port { get; set; }

				/// <summary>
				/// The encryption mode.
				/// </summary>
				[JsonProperty("mode")]
				public string Mode { get; set; } = "xsalsa20_poly1305_suffix";

			}
		}

		private class ReceiveProtocolPayload {

			/// <summary>
			/// The encryption mode.
			/// </summary>
			[JsonProperty("mode")]
			public string Mode { get; set; } = string.Empty;

			/// <summary>
			/// The secret key as a byte array.
			/// </summary>
			[JsonProperty("secret_key")]
			public byte[] SecretKey { get; set; } = new byte[0];

		}

		private class SpeakingPayload {

			/// <summary>
			/// 0b001 for mic<para/>
			/// 0b010 for soundshare<para/>
			/// 0b100 for priority<para/>
			/// </summary>
			[JsonProperty("speaking")]
			public int Speaking { get; set; }

			[JsonProperty("delay")]
			public int Delay { get; set; } = 0;

			[JsonProperty("ssrc")]
			public uint SSRC { get; set; }
		}
	}
}
