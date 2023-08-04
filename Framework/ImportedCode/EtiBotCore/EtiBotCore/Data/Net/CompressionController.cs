using System;
using System.Collections.Generic;
using System.Text;
using System.Net.WebSockets;
using EtiBotCore.Payloads;
using ICSharpCode.SharpZipLib.GZip;
using System.IO;
using System.Linq;
using EtiLogger.Logging;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace EtiBotCore.Data.Net {

	/// <summary>
	/// Provides methods of easily translating between byte data to and from <see cref="Payload"/>s.
	/// </summary>
	internal static class CompressionController {

		/// <summary>
		/// Given a <see cref="byte"/>[] presumably acquired via <see cref="ClientWebSocket.ReceiveAsync(ArraySegment{byte}, CancellationToken)"/>, this will decompress it (if needed) and return the included Json string.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="isBinary"></param>
		/// <returns></returns>
		public static Payload DecompressIntoPayload(byte[] data, bool isBinary) {
			if (!isBinary) {
				string shortData = Encoding.UTF8.GetString(data);
				return JsonConvert.DeserializeObject<Payload>(shortData);
			}

			using MemoryStream outputStr = new MemoryStream();
			using MemoryStream memStr = new MemoryStream(data);
			using InflaterInputStream inputStr = new InflaterInputStream(memStr);
			inputStr.CopyTo(outputStr);
			outputStr.Position = 0;
			string converted = Encoding.UTF8.GetString(outputStr.ToArray());
			return JsonConvert.DeserializeObject<Payload>(converted);
		}

		/// <summary>
		/// Given a <see cref="Payload"/>, this will convert it to its Json string and compress that string using ZLib.
		/// </summary>
		/// <param name="payload"></param>
		/// <returns></returns>
		[Obsolete("Don't compress sent stuff", true)] public static byte[] CompressPayload(Payload payload) {
			using MemoryStream payloadStr = new MemoryStream(payload.ToJsonBytes());
			using MemoryStream memStr = new MemoryStream();
			using GZipOutputStream outputStr = new GZipOutputStream(memStr);
			payloadStr.CopyTo(outputStr);
			return memStr.ToArray();
		}
	}
}
