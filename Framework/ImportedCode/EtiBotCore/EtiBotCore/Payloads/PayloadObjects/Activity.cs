using EtiBotCore.Data;
using EtiBotCore.Data.JsonConversion;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Payloads.PayloadObjects.ActivityObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// The activity payload.
	/// </summary>
	internal class Activity {

		/// <summary>
		/// The activity's name. This is the name of the game or content in activities like Playing, Listening To, etc.
		/// </summary>
		[JsonProperty("name"), JsonRequired]
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// The type of activity.
		/// </summary>
		[JsonProperty("type"), JsonConverter(typeof(EnumConverter))]
		public ActivityType Type { get; set; } = ActivityType.Playing;

		/// <summary>
		/// The URL of the stream, which is validated if <see cref="Type"/> is <see cref="ActivityType.Streaming"/>.
		/// </summary>
		[JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
		public string? URL { get; set; }

		/// <summary>
		/// Unix timestamp of when the activity was added to the user's session.
		/// </summary>
		[JsonProperty("created_at")]
		public long CreatedAt { get; set; }

		/// <summary>
		/// Applicable time information for this activity (start/end)
		/// </summary>
		[JsonProperty("timestamps", NullValueHandling = NullValueHandling.Ignore)]
		public TimestampObject? Timestamps { get; set; }

		/// <summary>
		/// The details of what the current user is doing. Upper text.
		/// </summary>
		[JsonProperty("details")]
		public string? Details { get; set; }

		/// <summary>
		/// The current party status. Lower text.
		/// </summary>
		[JsonProperty("state")]
		public string? State { get; set; }

		/// <summary>
		/// A lightweight Emoji associated with the status.
		/// </summary>
		[JsonProperty("emoji")]
		public EmojiObject? Emoji { get; set; }

		/// <summary>
		/// Information about the current party. <see langword="null"/> if this is not a joinable game.
		/// </summary>
		[JsonProperty("party", NullValueHandling = NullValueHandling.Ignore)]
		public PartyObject? Party { get; set; }

		/// <summary>
		/// The assets of this activity, namely its small and large images. <see langword="null"/> if these fields are irrelevant.
		/// </summary>
		[JsonProperty("assets", NullValueHandling = NullValueHandling.Ignore)]
		public ActivityAssets? Assets { get; set; }

		/// <summary>
		/// Keys needed to join, spectate, or find the match related to this activity. <see langword="null"/> if this is not a joinable game.
		/// </summary>
		[JsonProperty("secrets", NullValueHandling = NullValueHandling.Ignore)]
		public SecretObject? Secrets { get; set; }

		/// <summary>
		/// Whether or not this is an instanced game session. <see langword="null"/> if this is not a joinable game.
		/// </summary>
		[JsonProperty("instance", NullValueHandling = NullValueHandling.Ignore)]
		public bool? Instance { get; set; }

		/// <summary>
		/// Flags about this activity. <see langword="null"/> if this is not a joinable game or music stream.
		/// </summary>
		[JsonProperty("flags", NullValueHandling = NullValueHandling.Ignore), JsonConverter(typeof(EnumConverter))]
		public ActivityFlags? Flags { get; set; }

		/// <summary>
		/// For JSON ONLY
		/// </summary>
		public Activity() { }

		internal Activity(DiscordObjects.Guilds.MemberData.Activity activity) {
			Assets = activity.Assets;
			CreatedAt = activity.CreatedAt;
			Details = activity.Details;
			Emoji = activity.Emoji;
			Flags = activity.Flags;
			Instance = activity.Instance;
			Name = activity.Name;
			Party = activity.Party;
			Secrets = activity.Secrets;
			State = activity.State;
			Timestamps = activity.Timestamps;
			Type = activity.Type;
			URL = activity.URL;
		}
	}
}
