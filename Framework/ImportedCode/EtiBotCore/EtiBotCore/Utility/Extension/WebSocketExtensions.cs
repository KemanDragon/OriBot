using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Exceptions;
using EtiBotCore.Payloads.Data;

namespace EtiBotCore.Utility.Extension {

	/// <summary>
	/// Provides utilities for reading from and writing to <see cref="ClientWebSocket"/>s.
	/// </summary>
	public static class WebSocketExtensions {

		/// <summary>
		/// Receives data from this <see cref="ClientWebSocket"/> and returns it as a byte array.
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="WebSocketErroredException">If something closed the socket.</exception>
		public static async Task<byte[]> ReceiveBytesAsync(this ClientWebSocket socket, CancellationToken cancellationToken) {
			return (await ReceiveBytesAndResultAsync(socket, cancellationToken)).Item1;
		}

		private static Dictionary<ClientWebSocket, ManualResetEventSlim> ReceiveDelayers = new Dictionary<ClientWebSocket, ManualResetEventSlim>();
		

		/// <summary>
		/// Receives data from this <see cref="ClientWebSocket"/> and returns it as a byte array. Additionally returns the <see cref="WebSocketReceiveResult"/>.<para/>
		/// This will receive until the end of the message is received, merging the results of all packets together neatly.
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		/// <exception cref="WebSocketErroredException">If something closed the socket.</exception>
		public static async Task<(byte[], WebSocketReceiveResult?)> ReceiveBytesAndResultAsync(this ClientWebSocket socket, CancellationToken cancellationToken) {
			if (!ReceiveDelayers.ContainsKey(socket)) {
				ReceiveDelayers[socket] = new ManualResetEventSlim(true);
			}
			ManualResetEventSlim receiveDelayer = ReceiveDelayers[socket];
			receiveDelayer.Wait();
			receiveDelayer.Reset();
			List<byte> resultList = new List<byte>();

			WebSocketReceiveResult? result = null;
			if (DiscordClient.ForceNextAsNoEndpoint) {
				DiscordClient.ForceNextAsNoEndpoint = false;
				receiveDelayer.Set();
				throw new WebSocketErroredException("Emulated EndpointUnavailable", 1001);
			}

			while (socket.State == WebSocketState.Open) {
				byte[] largestPacket = new byte[4096];
				try {
					result = await socket.ReceiveAsync(new ArraySegment<byte>(largestPacket), cancellationToken);
				} catch {
					if (result == null) {
						receiveDelayer.Set();
					}
					throw;
				}

				// Did something happen?
				if (result.CloseStatus != null && result.CloseStatus != WebSocketCloseStatus.NormalClosure) {
					int code = (int)result.CloseStatus.Value;
					if (code >= 4000 && code <= 4014) {
						receiveDelayer.Set();
						throw new WebSocketErroredException((DiscordGatewayEventCode)code);
					} else {
						receiveDelayer.Set();
						throw new WebSocketErroredException(result.CloseStatusDescription ?? "A socket error has occurred.", code);
					}
				}
				resultList.AddRangeFrom(largestPacket, result.Count);
				largestPacket.Reset();

				if (result.EndOfMessage) break;
			}

			receiveDelayer.Set();
			return (resultList.ToArray(), result);
		}

		/// <summary>
		/// Sends the given <see cref="byte"/> arary through the socket, and supports splitting the messages into a given packet size.<para/>
		/// This automatically splits it into 4096 byte packets for the sending process.
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="data"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public static async Task SendBytesAsync(this ClientWebSocket socket, byte[] data, CancellationToken cancellationToken) {
			int sentBytes = 0;
			int count = Math.Min(data.Length - sentBytes, 4096);
			while (true) {
				bool isLastMessage = count < 4096 || (sentBytes + count == data.Length);
				// If the amount of bytes to send is less than 4096, then we haven't taken up the full space of a packet and know it's the last one.
				// If the amount of bytes to send is equal to 4096, it *could* be the last one, for instance if datalength % 4096 is 0. 
				//    ^ Here, check if sentBytes + count will be equal to that length. If it is, there's no more data after this.

				await socket.SendAsync(new ArraySegment<byte>(data, sentBytes, count), WebSocketMessageType.Text, isLastMessage, cancellationToken);
				if (isLastMessage) break;
				// Send text.
				// The segment starts at the amount of bytes we've sent and includes up to 4096 bytes.

				sentBytes += count;
				count = Math.Min(data.Length - sentBytes, 4096);
				
			}
		}

	}
}
