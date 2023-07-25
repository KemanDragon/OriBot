using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;

namespace EtiBotCore.DiscordObjects.Universal {

	/// <summary>
	/// An attachment on a message.
	/// </summary>
	
	public class Attachment {

		/// <summary>
		/// The ID of this attachment.
		/// </summary>
		public Snowflake ID { get; internal set; }

		/// <summary>
		/// The name of the file in this attachment.
		/// </summary>
		public string FileName { get; internal set; } = string.Empty;

		/// <summary>
		/// The size of this attachment in bytes.
		/// </summary>
		public int Size { get; internal set; }

		/// <summary>
		/// The URL linking to this attachment.
		/// </summary>
		public Uri URL { get; internal set; }

		/// <summary>
		/// Alternative, proxied variant of <see cref="URL"/>.
		/// </summary>
		public Uri ProxyURL { get; internal set; }

		/// <summary>
		/// The height of this attachment if it is an image, or <see langword="null"/> otherwise.
		/// </summary>
		public int? Height { get; internal set; }

		/// <summary>
		/// The width of this attachment if it is an image, or <see langword="null"/> otherwise.
		/// </summary>
		public int? Width { get; internal set; }

		/// <summary>
		/// Downloads this <see cref="Attachment"/> and writes all data to the file at the given path.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public Task SaveToFileAsync(string path) {
			using WebClient client = new WebClient();
			return client.DownloadFileTaskAsync(URL, path);
		}

		/// <summary>
		/// Downloads this <see cref="Attachment"/> and writes all data to the given <see cref="FileInfo"/>.
		/// </summary>
		/// <param name="path"></param>
		public Task SaveToFileAsync(FileInfo path) => SaveToFileAsync(path.FullName);

		/// <summary>
		/// Downloads this <see cref="Attachment"/> and puts it in a file named <see cref="FileName"/> in the given <see cref="DirectoryInfo"/>.
		/// </summary>
		/// <param name="inDirectory"></param>
		/// <returns></returns>
		public Task SaveToFileAsync(DirectoryInfo inDirectory) => SaveToFileAsync(Path.Combine(inDirectory.FullName, FileName));

		private Attachment(string url, string proxy) {
			URL = new Uri(url);
			ProxyURL = new Uri(proxy);
		}

		internal Attachment(Attachment other) : this(other.URL.ToString(), other.ProxyURL.ToString()) {
			ID = other.ID;
			FileName = other.FileName;
			Size = other.Size;
			Height = other.Height;
			Width = other.Width;
		}

		/// <summary>
		/// Creates a new <see cref="Attachment"/> from the given payload variant.
		/// </summary>
		/// <param name="pl"></param>
		/// <returns></returns>
		internal static Attachment? CreateFromPayload(Payloads.PayloadObjects.Attachment? pl) {
			if (pl == null) return null;
			return new Attachment(pl.URL, pl.ProxyURL) {
				ID = pl.ID,
				FileName = pl.FileName,
				Size = pl.Size,
				Height = pl.Height,
				Width = pl.Width
			};
		}

	}
}
