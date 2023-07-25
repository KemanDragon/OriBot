using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.DiscordObjects.Guilds.Specialized {

	/// <summary>
	/// Metadata for a thread.
	/// </summary>
	public class ThreadMetadata {

		/// <summary>
		/// Whether or not this thread is archived. Anyone can unarchive it unless <see cref="Locked"/> is <see langword="true"/>.
		/// </summary>
		public bool Archived { get; private set; }

		/// <summary>
		/// The amount of time until this thread is auto-archived in minutes. Can be 60, 1440, 4320, or 10080.
		/// </summary>
		public int AutoArchiveDuration { get; private set; }

		/// <summary>
		/// The timestamp of when the thread was archived.
		/// </summary>
		public DateTimeOffset ArchiveTimestamp { get; private set; }

		/// <summary>
		/// Whether or not this thread has been locked, which means only users with the <see cref="Permissions.ManageThreads"/> permission can unarchive it.
		/// </summary>
		public bool Locked { get; private set; } = default; // Must be set to default

		internal void Update(Payloads.PayloadObjects.ThreadMetadata meta) {
			Archived = meta.Archived;
			AutoArchiveDuration = meta.AutoArchiveDuration;
			ArchiveTimestamp = meta.ArchiveTimestamp.DateTime;
			Locked = meta.Locked;
		}

	}
}
