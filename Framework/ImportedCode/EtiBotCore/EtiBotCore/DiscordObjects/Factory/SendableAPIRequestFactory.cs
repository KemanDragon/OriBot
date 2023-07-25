using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using EtiBotCore.Client;
using EtiBotCore.Data.Net;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.Payloads;
using EtiBotCore.Utility.Counting;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Networking;
using EtiBotCore.Utility.Timing;
using EtiLogger.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EtiBotCore.DiscordObjects.Factory {

	/// <summary>
	/// Allows for sending requests to a specific portion of Discord's API. Should be statically defined in a class for an object that is associated with said requests.
	/// </summary>
	public class SendableAPIRequestFactory {

		#region Configs

		/// <summary>
		/// The root of Discord's gateway using https protocol, ending in a slash.
		/// </summary>
		public static readonly string GATEWAY_URL = $"https://discord.com/api/v{DiscordClient.TARGET_API_VERSION}/";

		/// <summary>
		/// If <see langword="true"/>, a time limit will be used to enforce 120 requests per 60 seconds. If <see langword="false"/>, a pool will be created.
		/// </summary>
		public const bool USE_TIME_BASED_LIMIT = false;

		#endregion

		#region Rate Limiting & Request Sanity

		/// <summary>
		/// The amount of requests that are currently processing.
		/// </summary>
		public static int AmountBusy {
			get => _AmountBusy;
			set {
				_AmountBusy = value;
				if (_AmountBusy == 0) NoLongerBusyEvent.Set();
				else NoLongerBusyEvent.Reset();
			}
		}
		private static int _AmountBusy = 0;

		private static readonly ManualResetEventSlim NoLongerBusyEvent = new ManualResetEventSlim(true);

		/// <summary>
		/// Yields until there's no busy requests.
		/// </summary>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the wait.</param>
		public static Task WaitForNoBusyRequestsAsync(CancellationToken cancellationToken) {
			return Task.Run(NoLongerBusyEvent.Wait, cancellationToken);
		}

		/// <summary>
		/// A system that enforces Discord's 120 requests per minute limit by enforcing at least 500ms of delay between requests
		/// </summary>
		private static readonly RateLimiter DiscordGlobalLimitEnforcer = new RateLimiter() {
			DelayTimeMillis = 500
		};

		/// <summary>
		/// A request budget enforcing 120 requests per 60 seconds.
		/// </summary>
		private static readonly BulkBudgetedValue Budget = new BulkBudgetedValue(60000, 120);

		/// <summary>
		/// The epoch that the latest global rate limit ends at. Requests must wait until this epoch passes before sending requests.
		/// </summary>
		internal static long GlobalRateLimitEndsAt = 0;

		#endregion


		internal static readonly Logger GatewayLogger = new Logger("[Sendable API Request] ") {
			DefaultLevel = LogLevel.Info
		};

		/// <summary>
		/// The URL to the appropriate gateway location using string formatting parameters for data that might be needed for this format URL.<para/>
		/// This should EXCLUDE the gateway domain itself, so this should be written like it shows up on the Discord API page. For example, if I want to get a channel, setting this value to <c>https://discord.com/api/v8/channels/{0}</c> is <strong>incorrect.</strong> It should be set to just <c>channels/{0}</c> (no slash at the start either).
		/// </summary>
		public string FormatURL { get; internal set; }

		/// <summary>
		/// Whether or not this endpoint has returned an HTTP 404.
		/// </summary>
		private bool IsMissing { get; set; } = false;

		/// <summary>
		/// The name of the bucket that this request uses.
		/// </summary>
		public RateLimitBucket? Bucket { get; protected internal set; }

		/// <summary>
		/// The type of request this sends.
		/// </summary>
		public HttpRequestType Type { get; internal set; }

		/// <summary>
		/// Whether or not this request will retry if it encounters gets rate limited.<para/>
		/// <strong>Default:</strong> <see langword="true"/>
		/// </summary>
		public bool ShouldRetryWhenRateLimited { get; internal set; } = true;

		/// <summary>
		/// Remarks to certain error codes that can help debug things. Setting a code's remark to <see langword="null"/> will hide that error code.
		/// </summary>
		internal Dictionary<int, string?> SpecialErrorRemarks { get; set; } = new Dictionary<int, string?>();

		/// <summary>
		/// Construct a new request factory with the required information.
		/// </summary>
		/// <param name="fmtUrl">The formatted URL. This should EXCLUDE the gateway domain itself, so this should be written like it shows up on the Discord API page. For example, if I want to get a channel, setting this value to <c>https://discord.com/api/v8/channels/{0}</c> is <strong>incorrect.</strong> It should be set to just <c>channels/{0}</c> (no slash at the start either).</param>
		/// <param name="type">The type of request that this is.</param>
		/// <param name="retry">If <see langword="true"/>, the request will yield and retry if it gets rate limited.</param>
		public SendableAPIRequestFactory(string fmtUrl, HttpRequestType type, bool retry = true) {
			FormatURL = fmtUrl;
			Type = type;
			ShouldRetryWhenRateLimited = retry;
		}

		/// <summary>
		/// Executes this API request, sending it to discord. This returns the response status code and message.
		/// </summary>
		/// <param name="info"></param>
		/// <returns></returns>
		public async Task<HttpResponseMessage?> ExecuteAsync(APIRequestData? info = null) {
			(object? _, HttpResponseMessage? msg) = await ExecuteAsync<object>(info);
			return msg;
		}

		/// <summary>
		/// Executes this API request, sending it to Discord and returning the given result. This also returns the response status code and message.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="info"></param>
		/// <returns></returns>
		public async Task<(T?, HttpResponseMessage?)> ExecuteAsync<T>(APIRequestData? info = null) where T : class {
			if (IsMissing) {
				throw new InvalidOperationException("Cannot execute. This endpoint has returned an http 404 in a previous call, and should not be used.");
			}
			try {
				if (Bucket != null) await Bucket.YieldAndPerform();
				if (USE_TIME_BASED_LIMIT) {
					await DiscordGlobalLimitEnforcer.RequestPerformAction();
				} else {
					await Budget.WaitForNextRestore();
					Budget.Spend();
				}

				AmountBusy++;
				using HttpClient client = SetupClient(new HttpClient(), info);

				List<FileInfo> files = new List<FileInfo>();
				if (info?.Files != null) {
					foreach (FileInfo? f in info.Files) {
						if (f != null && f.Exists) {
							files.Add(f);
						}
					}
				}

				if (files.Count > 0 && !ReferenceEquals(this, TextChannel.CreateMessage)) {
					GatewayLogger.WriteWarning($"WARNING: Request {FormatURL} ({Type}) tried to send with a file, but this is only for TextChannel.CreateMessage");
					files.Clear();
				}

				do {
					await YieldUntilGlobalRatePasses();
					object[] urlFormatEntries = info?.Params?.ToArray() ?? new object[0];

					HttpResponseMessage response = await RunRequest(client, urlFormatEntries, info, files);
					Bucket = RateLimitBucket.GetAndUpdateOrCreate(new DiscordRateLimitHeader(response));

					if (response.StatusCode == HttpStatusCode.NoContent) {
						AmountBusy--;
						return (null, response);
					}

					string textResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

					if (response.IsSuccessStatusCode) {
						AmountBusy--;
						if (typeof(T) == typeof(object)) return (null, response);
						return (JsonConvert.DeserializeObject<T>(textResponse), response);
					}

					if (response.StatusCode == HttpStatusCode.TooManyRequests) {
						DiscordRateLimitResponse resp = JsonConvert.DeserializeObject<DiscordRateLimitResponse>(textResponse);
						if (resp.IsGlobal) {
							GlobalRateLimitEndsAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (long)MathF.Ceiling(resp.RetryAfter * 1000);
							GatewayLogger.WriteCritical($"^#ff0000;HTTP Request {string.Format(FormatURL, urlFormatEntries)} ({Type}) caused a GLOBAL rate limit! Retrying in {resp.RetryAfter} seconds...");
						} else {
							GatewayLogger.WriteSevere($"^#ff0000;HTTP Request {string.Format(FormatURL, urlFormatEntries)} ({Type}) was rate limited! Retrying in {resp.RetryAfter} seconds...");
						}
						if (ShouldRetryWhenRateLimited) await resp.Yield();
					} else if (response.StatusCode == HttpStatusCode.Unauthorized) {
						GatewayLogger.WriteSevere("Not authorized to make this web request! Reconnect immediately.");
						DiscordClient.RestartProgram();
						return (null, response);
					//} else if (response.StatusCode == HttpStatusCode.NotFound) {
						//IsMissing = true;
						//return (null, response);
					} else { 
						int codeInt = (int)response.StatusCode;
						bool shouldWriteError = (SpecialErrorRemarks.ContainsKey(codeInt) && SpecialErrorRemarks[codeInt] != null) || !SpecialErrorRemarks.ContainsKey(codeInt);
						// Should write the error if it's not defined as a custom error, or it IS defined and it's not null.
						// A null custom error should prevent throwing that error.
						if (shouldWriteError) {
							string msg = $"^#ff0000;HTTP Request {string.Format(FormatURL, urlFormatEntries)} ({Type}) Failed! Error {codeInt} ({response.StatusCode})";
							if (SpecialErrorRemarks != null && SpecialErrorRemarks.ContainsKey(codeInt)) {
								msg += $"\n> {SpecialErrorRemarks[codeInt]}";
							}
							GatewayLogger.WriteSevere(msg);
						}
						AmountBusy--;
						return (null, response);
					}
				} while (ShouldRetryWhenRateLimited);
			} catch (Exception exc) {
				GatewayLogger.WriteException(exc);
				while (exc.InnerException != null) {
					exc = exc.InnerException;
					GatewayLogger.WriteException(exc);
				}
			}

			AmountBusy--;
			return (default, null);
		}

		/// <summary>
		/// Sets up an HttpClient
		/// </summary>
		/// <param name="client"></param>
		/// <param name="rqData"></param>
		/// <returns></returns>
		private HttpClient SetupClient(HttpClient client, APIRequestData? rqData) {
			string? reason = rqData?.Reason;

			client.DefaultRequestHeaders.Add("Authorization", DiscordClient.Current!.HttpAuthorization);
			if (reason != null) {
				client.DefaultRequestHeaders.Add("X-Audit-Log-Reason", Uri.EscapeDataString(reason));
				GatewayLogger.WriteLine($"Sending request {FormatURL} ({Type}) because: {reason}");
			}

			return client;
		}

		private async Task<HttpResponseMessage> RunRequest(HttpClient client, object[] urlFormatEntries, APIRequestData? info, List<FileInfo> files) {
			HttpResponseMessage response;
			using StringContent strContent = new StringContent(info?.GetJson() ?? "{}", Encoding.UTF8, "application/json");
			string urlParams = info?.GetURLParams() ?? "";

			/*
			if (Type == HttpRequestType.Get || Type == HttpRequestType.Delete) {
				GatewayLogger.WriteLine(GATEWAY_URL + string.Format(FormatURL, urlFormatEntries) + urlParams, LogLevel.Trace);
			} else {
				GatewayLogger.WriteLine(GATEWAY_URL + string.Format(FormatURL, urlFormatEntries), LogLevel.Trace);
			}
			*/

			if (Type == HttpRequestType.Get) {
				response = await client.GetAsync(GATEWAY_URL + string.Format(FormatURL, urlFormatEntries) + urlParams);

			} else if (Type == HttpRequestType.Post) {
				// The only one where files will matter.
				if (files.Count == 0) {
					response = await client.PostAsync(GATEWAY_URL + string.Format(FormatURL, urlFormatEntries), strContent);
				} else {
					
					using MultipartFormDataContent content = new MultipartFormDataContent("--BOUNDARY--");
					strContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") {
						Name = "payload_json"
					};
					content.Add(strContent);
					List<IDisposable>? lateDisposeOfOthers = info?.AddFilesToContent(content);
					/*
					int fileIndex = 0;
					foreach (FileInfo file in files) {
						byte[] data = File.ReadAllBytes(file.FullName);
						Debug.WriteLine(data.Length);

						using ByteArrayContent fileContent = new ByteArrayContent(data);
						fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") {
							Name = $"files[{fileIndex}]",
							FileName = file.Name
						};
						fileContent.Headers.ContentLength = data.Length;
						// fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("video/mp4");

						content.Add(fileContent);
						fileIndex++;
					}
					*/
					//string result = await content.ReadAsStringAsync();
					//File.WriteAllText(@"C:\attachmentsenddump.txt", result);
					response = await client.PostAsync(GATEWAY_URL + string.Format(FormatURL, urlFormatEntries), content);

					// Exiting scope, NOW dispose of these.
					if (lateDisposeOfOthers != null) {
						foreach (IDisposable disposable in lateDisposeOfOthers) {
							disposable.Dispose();
						}
					}
				}
			} else if (Type == HttpRequestType.Patch) {
				response = await client.PatchAsync(GATEWAY_URL + string.Format(FormatURL, urlFormatEntries), strContent);
			} else if (Type == HttpRequestType.Delete) {
				response = await client.DeleteAsync(GATEWAY_URL + string.Format(FormatURL, urlFormatEntries) + urlParams);
			} else if (Type == HttpRequestType.Put) {
				response = await client.PutAsync(GATEWAY_URL + string.Format(FormatURL, urlFormatEntries), strContent);
			} else {
				// This is just to satisfy the return.
				throw new InvalidEnumArgumentException($"Invalid HTTP request type {Type}");
			}
			return response;
		}

		/// <summary>
		/// Returns a task that will yield until the global rate limit ends, or that does nothing if the rate limit is not in effect.
		/// </summary>
		/// <returns></returns>
		private static Task YieldUntilGlobalRatePasses() {
			long timeToWait = GlobalRateLimitEndsAt - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			if (timeToWait > 0) {
				GatewayLogger.WriteLine($"We've been rate limited so we're waiting {timeToWait} milliseconds.", LogLevel.Trace);
				return Task.Delay((int)timeToWait);
			}
			return Task.CompletedTask;
		}

		/// <summary>
		/// One of the request types that can be sent to the gateway.
		/// </summary>
		public enum HttpRequestType {

			/// <summary>
			/// Acquire this data.
			/// </summary>
			Get,

			/// <summary>
			/// Send completely new data.
			/// </summary>
			Post,

			/// <summary>
			/// Send data that should replace old existing data.
			/// </summary>
			Patch,

			/// <summary>
			/// Delete this data.
			/// </summary>
			Delete,

			/// <summary>
			/// Put data here.
			/// </summary>
			Put,

		}

	}
}
