using EtiBotCore.Data.JsonConversion;
using EtiBotCore.Data.Structs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Payloads.PayloadObjects {
	internal class ThreadMetadata : PayloadDataObject {

		/// <summary>
		/// Whether or not this thread is archived. Anyone can unarchive it unless <see cref="Locked"/> is <see langword="true"/>.
		/// </summary>
		[JsonProperty("archived")]
		public bool Archived { get; set; }

		/// <summary>
		/// The amount of time until this thread is auto-archived in minutes. Can be 60, 1440, 4320, or 10080.
		/// </summary>
		[JsonProperty("auto_archive_duration")]
		public int AutoArchiveDuration { get; set; }

		/// <summary>
		/// The timestamp of when the thread was archived.
		/// </summary>
		[JsonProperty("archive_timestamp"), JsonConverter(typeof(TimestampConverter))]
		public ISO8601 ArchiveTimestamp { get; set; }

		/// <summary>
		/// Whether or not this thread has been locked, which means only users with the ManageThreads permission can unarchive it.
		/// </summary>
		[JsonProperty("locked")]
		public bool Locked { get; set; } = default; // Must be set to default

	}
}
