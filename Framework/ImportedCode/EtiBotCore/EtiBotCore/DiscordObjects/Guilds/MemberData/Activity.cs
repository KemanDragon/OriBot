using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Payloads.PayloadObjects.ActivityObjects;

namespace EtiBotCore.DiscordObjects.Guilds.MemberData {

	/// <summary>
	/// Represents an activity, such as playing a game.
	/// </summary>
	
	public class Activity {

		/// <summary>
		/// The activity's name. This is the name of the game or content in activities like Playing, Listening To, etc.
		/// </summary>
		public string Name { get; private set; } = string.Empty;

		/// <summary>
		/// The type of activity.
		/// </summary>
		public ActivityType Type { get; private set; } = ActivityType.Playing;

		/// <summary>
		/// The URL of the stream, which is validated if <see cref="Type"/> is <see cref="ActivityType.Streaming"/>.
		/// </summary>
		public string? URL { get; private set; }

		/// <summary>
		/// Unix timestamp of when the activity was added to the user's session.<para/>
		/// This is populated automatically on construction and set to the current time.
		/// </summary>
		public long CreatedAt { get; private set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

		/// <summary>
		/// Time information for this activity (start/end), or <see langword="null"/> if this does not have a designated start or end time.
		/// </summary>
		public TimestampObject? Timestamps { get; private set; }

		/// <summary>
		/// The details of what the current user is doing. Upper text.
		/// </summary>
		public string? Details { get; private set; }

		/// <summary>
		/// The current party status. Lower text.
		/// </summary>
		public string? State { get; private set; }

		/// <summary>
		/// A lightweight Emoji associated with the status.
		/// </summary>
		public EmojiObject? Emoji { get; private set; }

		/// <summary>
		/// Information about the current party. <see langword="null"/> if this is not a joinable game.
		/// </summary>
		public PartyObject? Party { get; private set; }

		/// <summary>
		/// The assets of this activity, namely its small and large images. <see langword="null"/> if these fields are irrelevant.
		/// </summary>
		public ActivityAssets? Assets { get; private set; }

		/// <summary>
		/// Keys needed to join, spectate, or find the match related to this activity. <see langword="null"/> if this is not a joinable game.
		/// </summary>
		public SecretObject? Secrets { get; private set; }

		/// <summary>
		/// Whether or not this is an instanced game session. <see langword="null"/> if this is not a joinable game.
		/// </summary>
		public bool? Instance { get; private set; }

		/// <summary>
		/// Flags about this activity. <see langword="null"/> if this is not a joinable game or music stream.
		/// </summary>
		public ActivityFlags? Flags { get; private set; }

		/// <summary>
		/// Constructs a minimal <see cref="Activity"/> that can be used for a bot's status.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="type"/> is not usable by bots or is not an actual activity type.</exception>
		public static Activity CreateActivityForBot(string text, ActivityType type) {
			if (!Enum.IsDefined(typeof(ActivityType), type)) throw new ArgumentOutOfRangeException(nameof(type));
			if (type == ActivityType.Streaming || type == ActivityType.Custom) throw new ArgumentOutOfRangeException(nameof(type));
			return new Activity {
				Type = type,
				Name = text
			};
		}

		private Activity() { }

		/// <summary>
		/// Takes in a payload activity and converts it to this.
		/// </summary>
		/// <param name="activity"></param>
		/// <returns></returns>
		internal Activity(Payloads.PayloadObjects.Activity activity) {
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
