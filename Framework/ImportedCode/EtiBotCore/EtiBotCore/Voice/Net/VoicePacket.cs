using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EtiBotCore.Utility.Extension;
using EtiLogger.Logging;


namespace EtiBotCore.Voice.Net {
	/*
	public static class VoicePacket {

		/// <summary>
		/// The sample rate of the incoming audio.
		/// </summary>
		public const int SAMPLE_RATE = 48000;

		/// <summary>
		/// The amount of channels.
		/// </summary>
		public const int CHANNELS = 2;

		/// <summary>
		/// Takes the given media file and converts it to PCM with ffmpeg, then spits out a number of opus frames.
		/// </summary>
		/// <param name="wrapper"></param>
		/// <param name="fromAudioFile"></param>
		/// <returns></returns>
		public static List<byte[]> GetOpusPackets(OpusWrapper wrapper, FileInfo fromAudioFile) {
			FileInfo cacheFile = new FileInfo(fromAudioFile.FullName + "-opuscache-" + wrapper.FrameSize);
			if (cacheFile.Exists) return GetOpusPacketsFromCache(cacheFile);
			short[] pcm = FFMPEG.GetPCM(fromAudioFile);
			List<byte[]> packets = wrapper.EncodeFrames(pcm);
			SavePacketsToCache(cacheFile, packets);
			return packets;
		}

		private static List<byte[]> GetOpusPacketsFromCache(FileInfo cacheFile) {
			Logger.Default.WriteLine("Loading packets from cache...", LogLevel.Trace);
			using BinaryReader reader = new BinaryReader(cacheFile.OpenRead());
			int size = reader.ReadInt32();
			List<byte[]> packets = new List<byte[]>(size);
			for (int i = 0; i < size; i++) {
				byte[] data = new byte[reader.ReadInt32()];
				reader.Read(data, 0, data.Length);
				packets.Add(data);
			}
			return packets;
		}

		private static void SavePacketsToCache(FileInfo cacheFile, List<byte[]> packets) {
			Logger.Default.WriteLine("Saving packets to cache...", LogLevel.Trace);
			using BinaryWriter writer = new BinaryWriter(cacheFile.Create());
			writer.Write(packets.Count);
			for (int i = 0; i < packets.Count; i++) {
				writer.Write(packets[i].Length);
				writer.Write(packets[i]);
			}
			Logger.Default.WriteLine("Done saving packets to cache.", LogLevel.Trace);
		}

		/// <summary>
		/// Create all of the voice packets in a probably not very good way of precalculating the time. 
		/// inb4 this causes terrible audio stuttering.
		/// </summary>
		/// <param name="frameSize"></param>
		/// <param name="ssrc"></param>
		/// <param name="key"></param>
		/// <param name="rawOpusPackets"></param>
		/// <returns></returns>
		public static List<byte[]> CreateVoicePackets(int frameSize, uint ssrc, byte[] key, List<byte[]> rawOpusPackets) {
			Logger.Default.WriteLine($"Using SSRC={ssrc} and a key of {key.Length} bytes, I am going to be encrypting and setting up {rawOpusPackets.Count} packets.", LogLevel.Trace);
			List<byte[]> voicePackets = new List<byte[]>(rawOpusPackets.Count);
			uint timestamp = 0;
			for (int i = 0; i < rawOpusPackets.Count; i++) {
				ushort sequence = (ushort)i;
				timestamp += (uint)frameSize;

				List<byte> header = new List<byte>() { 0x80, 0x78 };
				header.AddRange(sequence.ToBigEndian());
				header.AddRange(timestamp.ToBigEndian());
				header.AddRange(ssrc.ToBigEndian());
				List<byte> headerRaw = header.ToList();
				headerRaw.AddRange(new byte[12]);
				byte[] encryptedAudioData = SecretBox.Create(rawOpusPackets[i].ToArray(), headerRaw.ToArray(), key);
				// ^ Encryptes messages via XSalsa20
				header.AddRange(encryptedAudioData);
				voicePackets.Add(header.ToArray());
			}
			Logger.Default.WriteLine($"There we go. {voicePackets.Count} packets created.", LogLevel.Trace);
			return voicePackets;
		}
	}*/
}
