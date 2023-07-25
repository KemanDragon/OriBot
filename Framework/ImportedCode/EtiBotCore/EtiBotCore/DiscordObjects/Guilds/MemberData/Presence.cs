using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Payloads.Events.Intents.GuildPresences;
using EtiBotCore.Utility.Extension;
using System.Collections.Generic;
using System.Linq;

namespace EtiBotCore.DiscordObjects.Guilds.MemberData {

	/// <summary>
	/// Represents a member's presence.
	/// </summary>

	public class Presence {

		/// <summary>
		/// Create a presence that is offline, for this member.
		/// </summary>
		public static Presence CreateOfflinePresence(Member mbr) => new Presence() {
			UserID = mbr.ID,
			GuildID = mbr.Server.ID,
		};

		/// <summary>
		/// The activity or activities of this presence.
		/// </summary>
		/// <remarks>
		/// <strong>This reference is cloned in clone objects, but the actual Activity objects are not (the lists will not be synced, but the objects they reference will be).</strong>
		/// </remarks>
		public IReadOnlyList<Activity> Activities => _Activities;
		private List<Activity> _Activities = new List<Activity>();

		/// <summary>
		/// The first activity in this presence, or <see langword="null"/> if there is none. This corresponds to the activity that is displayed.
		/// </summary>
		public Activity? Activity => Activities?.FirstOrDefault();

		/// <summary>
		/// The presence (online/away/dnd/offline) of this client on various platforms.
		/// </summary>
		/// <remarks>
		/// <strong>This reference is cloned in clone objects.</strong>
		/// </remarks>
		public PlatformStatusContainer AllStatuses { get; protected internal set; } = new PlatformStatusContainer();

		/// <summary>
		/// The ID of the guild that this presence ties into.
		/// </summary>
		public Snowflake GuildID { get; private set; }

		/// <summary>
		/// The presence (online/away/dnd/offline) of this client on their current device or platform.
		/// </summary>
		public StatusType Status { get; private set; } = StatusType.Offline;

		/// <summary>
		/// The user this presence is associated with.
		/// </summary>
		public Snowflake UserID { get; private set; }

		/// <summary>
		/// Constructs a new <see cref="Presence"/> from the network event sent by Discord.
		/// </summary>
		/// <param name="evt"></param>
		internal Presence(PresenceUpdateEvent evt) {
			if (evt.Activities != null) {
				_Activities = new List<Activity>();
				foreach (var activity in evt.Activities) {
					_Activities.Add(new Activity(activity));
				}
			}
			if (evt.ClientStatus != null) {
				AllStatuses.OnDesktop = evt.ClientStatus.Desktop ?? AllStatuses.OnDesktop;
				AllStatuses.OnWeb = evt.ClientStatus.Web ?? AllStatuses.OnWeb;
				AllStatuses.OnMobile = evt.ClientStatus.Mobile ?? AllStatuses.OnMobile;
			}
			if (evt.GuildID != null) GuildID = evt.GuildID.Value;
			if (evt.Status != null) Status = evt.Status.Value;
			if (evt.User != null) UserID = evt.User.UserID;
		}

		private Presence() { }

		internal Presence Clone() {
			Presence prs = (Presence)MemberwiseClone();
			prs._Activities = _Activities.LazyCopy();
			prs.AllStatuses = (PlatformStatusContainer)AllStatuses.MemberwiseClone();
			return prs;
		}

		/// <summary>
		/// Represents the user's status on all platforms Discord runs on.
		/// </summary>
		public class PlatformStatusContainer {

			/// <summary>
			/// The user's status on the desktop app for Discord, or <see langword="null"/> if they are not signed in on this platform (offline/invisible).
			/// </summary>
			public StatusType? OnDesktop { get; internal set; }


			/// <summary>
			/// The user's status on the Discord website, or <see langword="null"/> if they are not signed in on this platform (offline/invisible).
			/// </summary>
			public StatusType? OnWeb { get; internal set; }

			/// <summary>
			/// The user's status on the Discord mobile app, or <see langword="null"/> if they are not signed in on this platform (offline/invisible).
			/// </summary>
			public StatusType? OnMobile { get; internal set; }

			/// <inheritdoc/>
			public new object MemberwiseClone() {
				return base.MemberwiseClone();
			}
		}

	}
}
