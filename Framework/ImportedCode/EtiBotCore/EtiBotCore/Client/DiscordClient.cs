using EtiBotCore.Utility;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EtiBotCore.Payloads.Events;
using System.IO;
using Newtonsoft.Json.Linq;
using EtiBotCore.Data;
using EtiBotCore.Payloads.Events.Passthrough;
using EtiBotCore.Payloads.Commands;
using EtiBotCore.Utility.Threading;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Exceptions;
using EtiLogger.Logging;
using EtiLogger.Data.Structs;
using EtiBotCore.Clockwork;
using EtiBotCore.Data.Net;
using EtiBotCore.Utility.Counting;
using EtiBotCore.DiscordObjects;
using System.Net.Sockets;
using System.Net.Http;
using System.Net.Http.Headers;
using EtiBotCore.Payloads.PayloadObjects;
using System.Net;
using EtiBotCore.DiscordObjects.Factory;
using System.Collections.Concurrent;
using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads.Events.Intents.GuildVoiceStates;
using System.Runtime.CompilerServices;

using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using Process = System.Diagnostics.Process;
using EtiBotCore.Payloads.Events.Intents.Guilds;
using EtiBotCore.Voice;
using EtiBotCore.Payloads.Events.Intents.GuildPresences;

namespace EtiBotCore.Client {


	/// <summary>
	/// A Discord client, which contains methods to connect to or disconnect from Discord, as well as utilities to interact with Discord.
	/// </summary>
	public partial class DiscordClient : IAsyncDisposable {

		/// <summary>
		/// Whether or not to output receiving presence update events. These are by far the most common and spam the log with incredible amounts of garbage data.
		/// </summary>
		public const bool OUTPUT_PRESENCE_UPDATE_EVENT_RECV = false;

		/// <summary>
		/// The API version that this bot targets.
		/// </summary>
		public const int TARGET_API_VERSION = 9;

		/// <summary>
		/// Yield all event handlers until outgoing requests have completed.
		/// </summary>
		private static readonly bool YieldEventHandlersWhileRequestsLive = false;

		/// <summary>
		/// The Discord API URL
		/// </summary>
		public static string DISCORD_GATEWAY_URL { get; private set; } = string.Empty;

		/// <summary>
		/// A URI pointing to <see cref="DISCORD_GATEWAY_URL"/>
		/// </summary>
		private readonly Uri DISCORD_GATEWAY_URI = new Uri(DISCORD_GATEWAY_URL);

		/// <summary>
		/// The <see cref="Logger"/> used by this <see cref="DiscordClient"/>. This is the core log.
		/// </summary>
		public static Logger Log { get; } = new Logger(new LogMessage.MessageComponent("[Core Client System] ", new Color(127, 63, 255)));

		/// <summary>
		/// Directly edits <see cref="Logger.LoggingLevel"/>, which determines the kind of messages this logs.
		/// </summary>
		public static LogLevel LoggingLevel {
			get => Log.DefaultLevel;
			set => Log.DefaultLevel = value;
		}

		/// <summary>
		/// The token for the bot.
		/// </summary>
		protected string Token { get; }

		/// <summary>
		/// When sending requests to Discord, this is the authorization used in the header.
		/// </summary>
		internal string HttpAuthorization => "Bot " + Token;

		/// <summary>
		/// The session ID, used in reconnecting.
		/// </summary>
		protected string? SessionID { get; set; }

		/// <summary>
		/// A queue of payloads to send after <see cref="RequestBudget"/> has been depleted.
		/// </summary>
		private protected Queue<Payload> PayloadQueue { get; } = new Queue<Payload>();

		/// <summary>
		/// Discord allows 120 requests per 60 seconds. This budget enforces that requests are sent in compliance with these rules.
		/// </summary>
		protected BudgetedValue RequestBudget { get; } = new BudgetedValue(60000, 120);

		/// <summary>
		/// The interval at which to send heartbeats.
		/// </summary>
		protected int HeartbeatInterval { get; set; } = -1;

		/// <summary>
		/// Whether or not the bot missed the latest heartbeat. If this is true when the next heartbeat should be sent, the bot assumes it disconnected. It is set to false when receiving a heartbeat acknowledge signal.
		/// </summary>
		protected bool MissedLastHeartbeat { get; set; } = false;

		/// <summary>
		/// The <see cref="ReusableCancellationTokenSource"/> for all socket operations.
		/// </summary>
		protected ReusableCancellationTokenSource CancellationSource { get; } = new ReusableCancellationTokenSource();

		/// <summary>
		/// The socket used for this connection.
		/// </summary>
		protected ClientWebSocket? Socket { get; set; }

		/// <summary>
		/// The last received Sequence number.
		/// </summary>
		internal int? LastReceivedSequenceNumber { get; set; } = null;

		/// <summary>
		/// A <see cref="ReusableCancellationTokenSource"/> that cancels the heartbeat task.
		/// </summary>
		protected ReusableCancellationTokenSource HeartbeatCanceller { get; } = new ReusableCancellationTokenSource();

		/// <summary>
		/// A <see cref="ReusableCancellationTokenSource"/> that cancels the receiver.
		/// </summary>
		protected ReusableCancellationTokenSource ReceiveCanceller { get; } = new ReusableCancellationTokenSource();

		/// <summary>
		/// Used to keep track of heartbeating.
		/// </summary>
		private protected HeartbeatClockwork HeartbeatClockworks = new HeartbeatClockwork();

		/// <summary>
		/// Whether or not the bot is connected to Discord.
		/// </summary>
		public bool Connected { get; private set; } = false;

		/// <summary>
		/// Whether or not this connection should reconnect if it errors out.
		/// </summary>
		public bool ReconnectOnFailure { get; set; }

		/// <summary>
		/// The current <see cref="DiscordClient"/>
		/// </summary>
		public static DiscordClient? Current { get; private set; }

		/// <summary>
		/// If true, all events that are not essential but are subscribed to via intents will be ignored, with the exception of GUILD_CREATE
		/// </summary>
		public bool DeferNonGuildCreateEvents { get; set; } = false;

		/// <summary>
		/// Whether or not developer mode is enabled.
		/// </summary>
		public bool DevMode {
			get => _DevMode;
			set {
				_DevMode = value;
				RefreshActivity?.Invoke();
			}
		}
		private bool _DevMode = false;

		/// <summary>
		/// An action that can be called to refresh the bot's status right now.
		/// </summary>
		public static Action? RefreshActivity { get; set; }

		/// <summary>
		/// For debugging, can be used to force the next received backet to be treated as error 1001 endpoint unavailable.
		/// </summary>
		public static bool ForceNextAsNoEndpoint { get; set; }

		/// <summary>
		/// All incoming payloads in a queue for event processing.
		/// </summary>
		protected readonly ConcurrentQueue<IEvent> IncomingPayloadQueue = new ConcurrentQueue<IEvent>();

		#region For Guild Member Requests
		// <summary>
		// The current request number.
		// </summary>
		// private int ReqNumber = 0;

		/// <summary>
		/// A list of member payloads from the get guild members chunk.
		/// </summary>
		private readonly List<Member> PayloadMembersByRequest = new List<Member>();

		/// <summary>
		/// A <see cref="ManualResetEventSlim"/> to delay the member processing cycle.
		/// </summary>
		private readonly ManualResetEventSlim NotYieldingForPayloadMembers = new ManualResetEventSlim(true);

		/// <summary>
		/// A <see cref="ManualResetEventSlim"/> to delay something that requested members.
		/// </summary>
		private readonly ManualResetEventSlim NotBusyWithRequestYielder = new ManualResetEventSlim(true);

		/// <summary>
		/// The amount of received chunks.
		/// </summary>
		private int ReceivedChunks = 0;

		/// <summary>
		/// When the last request was done.
		/// </summary>
		private long LastDidFullRequestAt = 0;

		#endregion

		/// <summary>
		/// Sets up the discord gateway URL. This should be <strong>awaited</strong> before running the constructor.
		/// </summary>
		/// <returns></returns>
		public static async Task Setup() {
			using HttpClient client = new HttpClient();
			HttpResponseMessage msg = await client.GetAsync($"https://discord.com/api/v{TARGET_API_VERSION}/gateway");
			string content = await msg.Content.ReadAsStringAsync().ConfigureAwait(false);
			JObject jobj = JsonConvert.DeserializeObject<JObject>(content);
			string url = jobj.Value<string>("url");
			if (!url.EndsWith("/")) url += "/";
			DISCORD_GATEWAY_URL = $"{url}?v={TARGET_API_VERSION}&encoding=json&compress=true";
			Log.WriteLine("Connecting to gateway " + DISCORD_GATEWAY_URL);
		}

		/// <summary>
		/// Construct a new <see cref="DiscordClient"/> with the given token and intents.
		/// </summary>
		/// <param name="token">The token to the bot.</param>
		/// <param name="intents">The data that the bot plans on doing with Discord (sending and receiving).</param>
		public DiscordClient(string token, GatewayIntent intents) {
			Token = token;
			Current = this;
			ActivePrivelegedIntents = intents & GatewayIntent.ALL_PRIVILEGED_INTENTS;
			ActiveIntents = intents;
			Events = new EventContainer(this);
			PayloadEventRegistry.Initialize();
		}

		/// <summary>
		/// Connects to Discord and sets up initial events.
		/// </summary>
		/// <returns></returns>
		public async Task ConnectAsync() {
			Socket = new ClientWebSocket();

			bool failedSocketConnection = false;
		CONNECTION_CYCLE:
			Log.WriteLine("Connecting...", LogLevel.Info);
			try {
				await Socket.ConnectAsync(DISCORD_GATEWAY_URI, CancellationSource.CurrentToken).TimeoutAfter(10000, "Failed to connect to gateway within 10 seconds.");
			} catch (Exception exc) {
				Log.WriteException(exc, true, LogLevel.Info);
				if (failedSocketConnection) {
					Log.WriteCritical("This is the second consecutive failure. Terminating and restarting app...");
					RestartProgram();
					return;
				}
				failedSocketConnection = true;
				goto CONNECTION_CYCLE;
			}
			Connected = true;

			await InitialHandshake(false);
			// ^ Will error if it fails.

			// Now we have a gateway established. Do some preliminary setup.
			await DiscordObjects.Universal.User.SetupSelfUser();

			RequestBudget.OnRestored += OnRequestBudgetRestored;
			HeartbeatClockworks.OnTimedOut += OnHeartbeatTimedOut;
			HeartbeatClockworks.StartChecking();
			StartReceiveLoopTask();
			StartHeartbeatTask();
		}

		/// <summary>
		/// Should be run if the bot receives opcode <see cref="PayloadOpcode.Reconnect"/>, which is Discord telling the bot to immediately reconnect.
		/// </summary>
		/// <param name="withResume">If <see langword="true"/>, this is sent with a resume payload instead of reidentifying.</param>
		/// <param name="terminateVoice">If <see langword="true"/>, the voice connection will be terminated too.</param>
		/// <param name="isFromInnerReattempt">Only <see langword="true"/> if this method called <see cref="ReconnectAsync(bool, bool, bool)"/>, which is used to track a double-failure.</param>
		/// <returns></returns>
		protected async Task ReconnectAsync(bool withResume, bool terminateVoice = false, bool isFromInnerReattempt = false) {
			Log.WriteLine("Reconnecting...", LogLevel.Info);

			// Disconnect first.
			Log.WriteLine("Disconnecting...", LogLevel.Debug);
			await DisconnectAsync("Client disconnected for the purpose of reconnection.", terminateVoice: terminateVoice);

			Log.WriteLine("Yielding...", LogLevel.Debug);
			await Task.Delay(1000);

			// Reconnect
			bool failedSocketConnection = false;
		CONNECTION_CYCLE:
			Log.WriteLine("Disposing and re-instantiating socket...", LogLevel.Debug);
			try { Socket?.Dispose(); } catch { }
			Socket = new ClientWebSocket();
			Log.WriteLine("Connecting new socket...", LogLevel.Debug);
			try {
				await Socket.ConnectAsync(DISCORD_GATEWAY_URI, CancellationSource.CurrentToken).TimeoutAfter(10000, "Failed to connect to gateway within 10 seconds.");
			} catch (Exception exc) {
				Log.WriteException(exc, true, LogLevel.Info);
				if (failedSocketConnection) {
					Log.WriteCritical("This is the second consecutive failure. Terminating and restarting app...");
					RestartProgram();
					return;
				}
				failedSocketConnection = true;
				goto CONNECTION_CYCLE;
			}
			Connected = true;

			try {
				Log.WriteLine("Performing initial handshake...", LogLevel.Debug);
				await InitialHandshake(withResume);
			} catch {
				// well that was a load of shit
				if (!isFromInnerReattempt) {
					Log.WriteWarning("Reconnection handshake failed!", LogLevel.Info);
					await ReconnectAsync(false, false, true);
				} else {
					Log.WriteCritical("Reconnection handshake failed AGAIN! Terminating app and restarting it.", LogLevel.Info);
					RestartProgram();
				}
				return;
			}

			_ = OnReconnect.Invoke();

			// Restart tasks
			RequestBudget.OnRestored += OnRequestBudgetRestored;
			HeartbeatClockworks.OnTimedOut += OnHeartbeatTimedOut;
			HeartbeatClockworks.StartChecking();
			StartReceiveLoopTask();
			StartHeartbeatTask();

			Log.WriteLine("Successfully reconnected!", LogLevel.Info);
			Connected = true;
		}

		/// <summary>
		/// Restarts the program by starting a new instance of this EXE and exiting this one immediately after.
		/// </summary>
		public static void RestartProgram() {
			try {
				ProcessStartInfo oriBot = new ProcessStartInfo {
					FileName = (Environment.MachineName.ToLower() == "xan") ? @"V:\OriBotV3\OriBotV3.exe" : @"C:\OriBotV3\OriBotV3.exe"
				};
				Process.Start(oriBot);
			} catch (Exception exc) {
				AlertForRestartFailure(exc).Wait();
			}
			Environment.Exit(1);
		}

		#region Webhook
		private const string WebhookURL = "https://discord.com/api/webhooks/819497644203704340/-BDofVa8Kf4LvCTFirBoksW5QYX_iTDI-15aA95wkKg64Qa8lIuCGATL8xxx05dY7m_i";

		private static async Task AlertForRestartFailure(Exception exc) {
			using HttpClient client = new HttpClient();
			Dictionary<string, string> values = new Dictionary<string, string> {
				["content"] = $"**<@!114163433980559366> ALERT: CRITICAL FAILURE**\nAn attempt to reconnect failed twice in a row, and when attempting to restart the application as a result, a `{exc.GetType().FullName}` was thrown! The log will have the full detail of this exception.\n\nThe included message is: {exc.Message}\n\nFor stability reasons, the bot has self-terminated."
			};
			Log.WriteException(exc);

			FormUrlEncodedContent content = new FormUrlEncodedContent(values);
			await client.PostAsync(WebhookURL, content);
		}
		#endregion

		private void OnHeartbeatTimedOut(TimeoutException exc) {
			Log.WriteException(exc, true, LogLevel.Info);
			_ = ReconnectAsync(false);
		}

		/// <summary>
		/// Disconnects the client from Discord and stops all processes that ensure things are working as they should be.
		/// </summary>
		/// <param name="message">The message to include with the close.</param>
		/// <param name="status">The status to include with the close.</param>
		/// <param name="terminateVoice">If true, any ongoing voice channel connections will also be terminated.</param>
		/// <returns></returns>
		public async Task DisconnectAsync(string message = "Client manually closed the connection.", WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure, bool terminateVoice = true) {
			Connected = false;
			try {
				Task? closeTask = Socket?.CloseAsync(status, message, CancellationSource.CurrentToken);
				if (closeTask != null) await closeTask;
			} catch { }
			// Reset();
			//if (VoiceConnected && terminateVoice) await DisconnectFromVoiceAsync();
			//if (terminateVoice) await VoiceConnectionMarshaller.DisconnectIfExists();

			RequestBudget.OnRestored -= OnRequestBudgetRestored;
			HeartbeatClockworks.OnTimedOut -= OnHeartbeatTimedOut;

			HeartbeatCanceller.Cancel();
			ReceiveCanceller.Cancel();
			HeartbeatClockworks.StopChecking();
			PayloadQueue.Clear();
			RequestBudget.Reset();
			Log.WriteLine("Disconnected. Reason: " + message, LogLevel.Info);

			try { Socket?.Dispose(); } catch { }
		}

		/// <summary>
		/// Identical to <see cref="DisconnectAsync(string, WebSocketCloseStatus, bool)"/>, but this will also close the program.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="status"></param>
		/// <returns></returns>
		public async Task TerminateAsync(string message = "Client manually closed the connection.", WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure) {
			await DisconnectAsync(message, status);
			Environment.Exit(0);
		}

		/// <summary>
		/// Begins the heartbeat loop.
		/// </summary>
		/// <returns></returns>
		private void StartHeartbeatTask() {
			try {
				Task.Run(async () => {
					while (Socket!.State == WebSocketState.Open) {
						await SendHeartbeat();
						await Task.Delay(HeartbeatInterval, HeartbeatCanceller.CurrentToken);
					}
				}, HeartbeatCanceller.CurrentToken);
			} catch (OperationCanceledException) {
			} catch (Exception exc) {
				Log.WriteException(exc);
				if (Socket!.State == WebSocketState.Aborted) { 
					_ = ReconnectAsync(true);
				}
			}
		}

		/// <summary>
		/// Sends a heartbeat when called.
		/// </summary>
		/// <returns></returns>
		private Task SendHeartbeat() {
			Log.WriteLine("Heartbeat sent.", LogLevel.Trace);
			HeartbeatClockworks.Sent();
			return Send(new Payload {
				Operation = PayloadOpcode.Heartbeat,
				Data = LastReceivedSequenceNumber!
			});
		}

		/// <summary>
		/// The main recieve loop. Sends out events.
		/// </summary>
		/// <returns></returns>
		private void StartReceiveLoopTask() {
			EtiTaskExtensions.Launch(async () => {
				while (Socket!.State == WebSocketState.Open) {
					if (!Connected) break;
					Payload? nextPayload = await Receive(); // Yields
					if (nextPayload == null) {
						throw new WebSocketErroredException("A received payload was null!");
					}
					if (nextPayload.Operation == PayloadOpcode.Dispatch) {
						IEvent? payloadEvent = PayloadEventRegistry.CreateInstanceForEventPayload(nextPayload);
						if (payloadEvent == null) {
							// Something's busted.
							await DisconnectAsync("Received a dispatch code with an invalid or null event.", WebSocketCloseStatus.InvalidPayloadData);
							throw new WebSocketErroredException("Invalid data was sent from Discord.");
						}

						/*
						// Special behavior:
						if (WaitingOnVoiceStateUpdate && payloadEvent is VoiceStateUpdateEvent vStateUpdate && vStateUpdate.UserID == DiscordObjects.Universal.User.BotUser.ID) {
							VoiceSessionID = vStateUpdate.SessionID;
							WaitingOnVoiceStateUpdate = false;
							Log.WriteLine("Received voice state update.", LogLevel.Trace);
							if (!WaitingOnVoiceStateUpdate && !WaitingOnVoiceServerUpdate) {
								VoiceStateUpdatesReceived.Set();
							}

						} else if (WaitingOnVoiceServerUpdate && payloadEvent is VoiceServerUpdateEvent vServerUpdate) {
							VoiceToken = vServerUpdate.Token;
							VoiceEndpoint = vServerUpdate.Endpoint;
							WaitingOnVoiceServerUpdate = false;
							Log.WriteLine("Received voice server update.", LogLevel.Trace);
							if (!WaitingOnVoiceStateUpdate && !WaitingOnVoiceServerUpdate) {
								VoiceStateUpdatesReceived.Set();
							}
						}
						*/

						if (!DeferNonGuildCreateEvents || (DeferNonGuildCreateEvents && (payloadEvent.GetEventName() == "GUILD_CREATE" || payloadEvent.GetEventName() == "GUILD_MEMBERS_CHUNK"))) {
							if (SendableAPIRequestFactory.AmountBusy > 0 && YieldEventHandlersWhileRequestsLive) {
								Log.WriteLine($"Received event {payloadEvent.GetEventName()}, but it had to be enqueued because we're waiting on some sent requests to finish up.", LogLevel.Trace);
								IncomingPayloadQueue.Enqueue(payloadEvent);
							} else {
								bool isPresUpdate = payloadEvent is PresenceUpdateEvent;
								if (!isPresUpdate || (isPresUpdate && OUTPUT_PRESENCE_UPDATE_EVENT_RECV)) {
									Log.WriteLine("Received and executed event " + payloadEvent.GetEventName(), LogLevel.Trace);
								}
								_ = Task.Run(() => payloadEvent.Execute(this), ReceiveCanceller.CurrentToken);
							}
						}
					} else if (nextPayload.Operation == PayloadOpcode.Heartbeat) {
						await SendHeartbeat(); // Respond with a heartbeat if one is requested.
											// ^ Docs: The gateway may request a heartbeat from you in some situations, 
											//         and you should send a heartbeat back to the gateway as you normally would.
					} else if (nextPayload.Operation == PayloadOpcode.HeartbeatAcknowledged) {
						HeartbeatClockworks.Acknowledged();
						Log.WriteLine("Heartbeat Acknowledged. You are still alive. Congratulations.", LogLevel.Trace);
						_ = OnHeartbeat.Invoke();
					} else if (nextPayload.Operation == PayloadOpcode.InvalidSession) {
						bool isResumable = false;
						if (nextPayload.Data != null) {
							isResumable = ((JObject)nextPayload.Data).ToObject<bool>();
						}
						Log.WriteLine("§cThis session is invalid! Discord has relayed that it " + (isResumable ? "IS" : "IS NOT") + " resumable.", LogLevel.Info);
						await ReconnectAsync(isResumable);
					}
				}

				Log.WriteLine("Main loop aborted.", LogLevel.Info);
				if (ReconnectOnFailure) {
					_ = ReconnectAsync(true);
				}


			}, Log, ReceiveCanceller);
			EtiTaskExtensions.Launch(async () => {
				while (true) {
					if (IncomingPayloadQueue.Count > 0) {
						if (SendableAPIRequestFactory.AmountBusy > 0) {
							Log.WriteLine("I'm waiting on " + SendableAPIRequestFactory.AmountBusy + " request(s) to finish before dealing with any events.", LogLevel.Trace);
							await SendableAPIRequestFactory.WaitForNoBusyRequestsAsync(ReceiveCanceller.CurrentToken);
							Log.WriteLine("Alrighty! Requests are done.", LogLevel.Trace);
						}
						if (IncomingPayloadQueue.TryDequeue(out IEvent? plEvt)) {
							Log.WriteLine($"I've gotten around to executing a delayed {plEvt.GetEventName()} event.", LogLevel.Trace);
							_ = Task.Run(() => plEvt.Execute(this), ReceiveCanceller.CurrentToken).ConfigureAwait(false);
							// ^ Do NOT await.
						}
					}

					await Task.Delay(10, ReceiveCanceller.CurrentToken);
				}
			}, Log, ReceiveCanceller);
		}

		/// <summary>
		/// Performs the initial handshake with Discord.
		/// </summary>
		/// <param name="sendResumeInstead">If <see langword="true"/>, the system will use the reconnection routine instead of sending an identify payload.</param>
		private async Task InitialHandshake(bool sendResumeInstead) {
			if (!sendResumeInstead) {
				Log.WriteLine("Identifying...", LogLevel.Info);
				Payload identifyPayload = new Payload {
					Operation = PayloadOpcode.Identify,
					Data = new IdentifyCommand {
						Token = Token,
						Intents = ActiveIntents,
						Compress = true
					}
				};
				await Send(identifyPayload, true);

				Payload? helloPacket = await Receive(true);
				if (helloPacket?.Operation == PayloadOpcode.Hello) {
					HelloEvent data = helloPacket.GetObjectFromData<HelloEvent>();
					HeartbeatInterval = data.HeartbeatInterval;
					Log.WriteLine("Discord requested a heartbeat is sent every " + data.HeartbeatInterval + "ms", LogLevel.Debug);
				} else {
					Connected = false;
					await DisconnectAsync($"Client was not expecting this message (expected Hello opcode, recieved opcode {helloPacket?.Operation} (PayloadOpcode.{Enum.GetName(typeof(PayloadOpcode), helloPacket?.Operation ?? PayloadOpcode.NULL)})", WebSocketCloseStatus.InvalidMessageType);
					throw new InvalidDataException($"Invalid message received! Expecting Hello payload, got something else (op={helloPacket?.Operation})");
				}

				Payload? readyPacket = await Receive(true);
				if (readyPacket?.Operation == PayloadOpcode.Dispatch) {
					if (readyPacket.EventName == "READY") {
						ReadyEvent readyEvt = readyPacket.GetObjectFromData<ReadyEvent>();
						SessionID = readyEvt.SessionID;
					} else {
						Connected = false;
						await DisconnectAsync($"Client was not expecting this message (expected READY event, recieved event {readyPacket.EventName})", WebSocketCloseStatus.InvalidMessageType);
						throw new InvalidDataException($"Invalid message received! Expecting READY event, got something else (event={readyPacket.EventName})");
					}
				}
			} else {
				Log.WriteLine("Reidentifying and resuming...", LogLevel.Trace);
				Payload resumePayload = new Payload {
					Operation = PayloadOpcode.Resume,
					Data = new ResumeCommand {
						Token = Token,
						SessionID = SessionID!,
						Sequence = LastReceivedSequenceNumber!.Value
					}
				};
				await Send(resumePayload, true);

				Payload? resumedPayload = await Receive(true);
				bool ok = true;
				if (resumedPayload?.Operation == PayloadOpcode.Dispatch) {
					if (!(resumedPayload.EventName == "RESUMED")) {
						ok = false;
					}
				} else if (resumedPayload?.Operation == PayloadOpcode.InvalidSession) {
					// Weren't able to reconnect fast enough.
					Log.WriteLine("Failed to reconnect! Disconnecting now. Re-identifying and starting from scratch in 5 seconds...");
					await DisconnectAsync("Client closed connection due to reconnection failure.");
					await Task.Delay(5000);
					await ConnectAsync();
					return;
				} else {
					ok = false;
				}
				if (!ok) {
					Connected = false;
					await DisconnectAsync($"Client was not expecting this message (expected RESUMED or OPCODE 9, recieved event {resumedPayload?.EventName})", WebSocketCloseStatus.InvalidMessageType);
					throw new InvalidDataException($"Invalid message received! Expecting RESUMED or OPCODE 9, got something else (event={resumedPayload?.EventName})");
				}

			}
		}

		#region Send & Receive

		/// <summary>
		/// Sends the given <see cref="Payload"/> to Discord.
		/// </summary>
		/// <param name="payload">The <see cref="Payload"/> to send.</param>
		/// <param name="force">If <see langword="true"/>, this will skip the queue process and request limiter, and instead directly send.</param>
		/// <returns></returns>
		internal async Task Send(Payload payload, bool force = false) {
			if (force) {
				await Socket!.SendBytesAsync(payload.ToJsonBytes(), CancellationSource.CurrentToken);
				return;
			}

			if (!RequestBudget.Depleted) {
				await Socket!.SendBytesAsync(payload.ToJsonBytes(), CancellationSource.CurrentToken);
			} else {
				Log.WriteLine("A payload had to be deferred!\n", LogLevel.Debug);
				Log.WriteLine(payload, LogLevel.Debug);
				PayloadQueue.Enqueue(payload);
			}
		}

		/// <summary>
		/// Recieves a <see cref="Payload"/> from Discord and returns it. Also sets <see cref="LastReceivedSequenceNumber"/> if applicable.
		/// </summary>
		/// <returns></returns>
		private async Task<Payload?> Receive(bool doNotReconnect = false) {
			try {
				if (!Connected) return null;
				(byte[] bytes, WebSocketReceiveResult? result) = await Socket!.ReceiveBytesAndResultAsync(CancellationSource.CurrentToken);
				Payload payload = CompressionController.DecompressIntoPayload(bytes, result?.MessageType == WebSocketMessageType.Binary);
				if (payload == null) return null;

				if (payload.Operation == PayloadOpcode.Dispatch) {
					LastReceivedSequenceNumber = payload.Sequence;
				}
				return payload;
			} catch (OperationCanceledException) {
			} catch (Exception genericExc) {
				Log.WriteException(genericExc, true, LogLevel.Info);
				if (!doNotReconnect) _ = ReconnectAsync(false, false);
			}
			return null;
		}

		#endregion

		/// <summary>
		/// Executed when the request budget is restored. Clears out the payload queue.
		/// </summary>
		private async Task OnRequestBudgetRestored() {
			if (PayloadQueue.Count > RequestBudget.Size) {
				Log.WriteLine($"§cWARNING: More than {RequestBudget.Size} entries were enqueued, and will ^u;immediately exhaust the current event limit.^!u; This may result in cascading latency issues and a huge backlog of payloads!");
			}
			Log.WriteLine($"Sending {Math.Min(PayloadQueue.Count, RequestBudget.Size)} backlogged payloads...", LogLevel.Trace);
			while (PayloadQueue.Count > 0 && !RequestBudget.Depleted) {
				await Send(PayloadQueue.Dequeue());
			}
		}

		private async Task SendRequestGuildMembers(Snowflake guildId) {
			// ReqNumber++;
			// string requestId = "G-" + ReqNumber;
			ReceivedChunks = 0;
			PayloadMembersByRequest.Clear();
			NotYieldingForPayloadMembers.Reset();
			Payload request = new Payload {
				Operation = PayloadOpcode.RequestGuildMembers,
				Data = new RequestGuildMembersCommand {
					GuildID = guildId,
					Query = string.Empty,
					Limit = 0,
					Presences = false,
					Users = null,
					// Nonce = requestId
				}
			};
			await Send(request);
		}

		internal void GuildChunkReceived(GuildMembersChunkEvent chunk) {
			PayloadMembersByRequest.AddRange(chunk.Members);
			ReceivedChunks++;
			if (ReceivedChunks >= chunk.ChunkCount) NotYieldingForPayloadMembers.Set();
		}

		/// <summary>
		/// Requests every single member from the given guild. This is very expensive. Please use it sparingly.<para/>
		/// If more than one request is done per minute, a cached value will be returned.
		/// </summary>
		/// <param name="serverId"></param>
		/// <returns></returns>
		internal async Task<DiscordObjects.Guilds.Member[]> RequestAllGuildMembersAsync(Snowflake serverId) {
			long epoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			if (epoch - LastDidFullRequestAt < 60000) {
				return DiscordObjects.Guilds.Member.InstantiatedMembers[serverId].Values.ToArray();
			}

			if (!NotBusyWithRequestYielder.IsSet) {
				NotBusyWithRequestYielder.Wait();
				return DiscordObjects.Guilds.Member.InstantiatedMembers[serverId].Values.ToArray();
			}
			LastDidFullRequestAt = epoch;

			NotBusyWithRequestYielder.Reset();
			await SendRequestGuildMembers(serverId);
			NotYieldingForPayloadMembers.Wait();
			var mbrs = new DiscordObjects.Guilds.Member[PayloadMembersByRequest.Count];
			for (int idx = 0; idx < mbrs.Length; idx++) {
				await DiscordObjects.Guilds.Member.CreateFromPayloadInRequestChunk(PayloadMembersByRequest[idx], serverId);
			}
			NotBusyWithRequestYielder.Set();
			return DiscordObjects.Guilds.Member.InstantiatedMembers[serverId].Values.ToArray();
		}

		/// <summary>
		/// Sets the bot's current activity.
		/// </summary>
		/// <param name="activity">The activity to use.</param>
		/// <param name="statusType">The new status (online, away, etc.) - offline and invisible function identically.</param>
		/// <returns></returns>
		public Task SetActivity(DiscordObjects.Guilds.MemberData.Activity activity, StatusType statusType = StatusType.Online) {
			string status = "online";
			if (statusType == StatusType.Online) {
				status = "online";
			} else if (statusType == StatusType.Idle) {
				status = "idle";
			} else if (statusType == StatusType.DoNotDisturb) {
				status = "dnd";
			} else if (statusType == StatusType.Invisible || statusType == StatusType.Offline) {
				status = "invisible";
			}
			StatusUpdate upd = new StatusUpdate {
				Activities = new Activity[] { new Activity(activity) },
				Status = status
			};
			Payload pl = new Payload {
				Operation = PayloadOpcode.PresenceUpdate,
				Data = upd
			};
			return Send(pl);
		}

		/// <summary>
		/// Disconnect this <see cref="DiscordClient"/> from the gateway and dispose of its information.
		/// </summary>
		/// <returns></returns>
		public async ValueTask DisposeAsync() {
			await DisconnectAsync();
		}

		private class StatusUpdate {

			[JsonProperty("since")]
			public int? Since { get; set; } = null;

			[JsonProperty("activities")]
			public Activity[]? Activities { get; set; }

			[JsonProperty("status")]
			public string Status { get; set; } = "online";

			[JsonProperty("afk")]
			public bool AFK { get; set; } = false;
		}
	}
}
