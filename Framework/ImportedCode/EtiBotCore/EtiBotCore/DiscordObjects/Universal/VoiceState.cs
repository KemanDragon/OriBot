using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.Payloads;
using EtiBotCore.Utility.Threading;

namespace EtiBotCore.DiscordObjects.Universal {

	/// <summary>
	/// Represents a user's status in a voice channel or DM call.
	/// </summary>
	
	public class VoiceState {

		internal static readonly ThreadedDictionary<Snowflake, VoiceState> VoiceStateCache = new ThreadedDictionary<Snowflake, VoiceState>();

		/// <summary>
		/// Whether or not this <see cref="VoiceState"/> exists in a DM. Checks <see cref="ServerID"/> == <see langword="null"/>
		/// </summary>
		public bool IsDM => ServerID == null;

		/// <summary>
		/// Whether or not the user this <see cref="VoiceState"/> corresponds to is connected to a voice channel.
		/// </summary>
		public bool IsConnectedToVoice => ChannelID != null;

		/// <summary>
		/// Returns the <see cref="VoiceChannel"/> or <see cref="DMChannel"/> that is currently connected to. May return <see langword="null"/> if the channels were not properly downloaded for whatever reason.
		/// </summary>
		public ChannelBase? Channel {
			get {
				if (ChannelID == null) return null;
				if (IsDM) {
					return DMChannel.DMChannelCache.GetValueOrDefault(ChannelID!.Value);
				}
				return GuildChannelBase.GetFromCache<VoiceChannel>(ChannelID!.Value);
			}
		}

		/// <summary>
		/// The user that this state correlates to.
		/// </summary>
		/// <remarks>
		/// <strong>This reference is cloned in clone objects, and points to the old user that this old <see cref="VoiceState"/> belongs to.</strong>
		/// </remarks>
		public User User {
			get {
				if (_User == null) {
					_User = User.GetOrDownloadUserAsync(UserID).Result!;
				}
				return _User;
			}
		}
		private User? _User = null;

		
		/// <summary>
		/// The ID of the voice channel they are present in, or <see langword="null"/> if they are not connected to a channel.
		/// </summary>
		public Snowflake? ChannelID { get; private set; }

		/// <summary>
		/// The ID of the user that this <see cref="VoiceState"/> corresponds to.
		/// </summary>
		public Snowflake UserID { get; private set; }

		/// <summary>
		/// The ID of the server that this <see cref="VoiceState"/> corresponds to, or <see langword="null"/> if this is a DM.
		/// </summary>
		public Snowflake? ServerID { get; private set; }

		/// <summary>
		/// The ID of this voice session.
		/// </summary>
		public string SessionID { get; private set; } = string.Empty;

		/// <summary>
		/// Whether or not this user is deafened.
		/// </summary>
		public bool Deafened { get; private set; }

		/// <summary>
		/// Whether or not this user is muted.
		/// </summary>
		public bool Muted { get; private set; }

		/// <summary>
		/// Whether or not this user is server deafened
		/// </summary>
		public bool ServerDeafened { get; private set; }

		/// <summary>
		/// Whether or not this user is server muted.
		/// </summary>
		public bool ServerMuted { get; private set; }

		/// <summary>
		/// Whether or not this user is streaming something to the channel, or <see langword="null"/> for DMs
		/// </summary>
		public bool? Streaming { get; private set; }

		/// <summary>
		/// Whether or not this user has their webcam on.
		/// </summary>
		public bool WebcamOn { get; private set; }

		/// <summary>
		/// Whether or not I have this member muted for me.
		/// </summary>
		public bool Suppressed { get; private set; }

		/// <summary>
		/// Returns the given user's voice state, or <see langword="null"/> if they do not have one.
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		public static VoiceState? GetStateFor(Snowflake userId) {
			return VoiceStateCache.GetValueOrDefault(userId);
		}

		/// <summary>
		/// Returns the given user's voice state, or returns a new + empty state if they don't have one.
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		internal static VoiceState GetStateForOrCreate(Snowflake userId) {
			return VoiceStateCache.GetValueOrDefault(userId) ?? new VoiceState(userId);
		}

		internal VoiceState(Guild? server, Payloads.PayloadObjects.VoiceState payloadState) : this(payloadState.UserID) {
			UpdateFrom(server, payloadState);
		}

		internal VoiceState(Snowflake userId) {
			VoiceStateCache[userId] = this;
		}

		internal void UpdateFrom(Guild? server, Payloads.PayloadObjects.VoiceState payloadState) {
			ChannelID = payloadState.ChannelID;
			Deafened = payloadState.Deafened;
			Muted = payloadState.Muted;
			ServerDeafened = payloadState.ServerDeafened;
			ServerMuted = payloadState.ServerMuted;

			ServerID = payloadState.GuildID ?? server?.ID;
			SessionID = payloadState.SessionID;
			Streaming = payloadState.Streaming;
			Suppressed = payloadState.Suppressed;
			UserID = payloadState.UserID;
			WebcamOn = payloadState.WebcamOn;
		}

		/// <summary>
		/// Creates a copy of this <see cref="VoiceState"/>. All fields are new, except for <see cref="Channel"/>.
		/// </summary>
		public VoiceState MemberwiseClone(User cloneUser) {
			VoiceState newState = (VoiceState)base.MemberwiseClone();
			newState._User = cloneUser;
			return newState;
		}

		/// <summary>
		/// Organizes this <see cref="VoiceState"/> into a string for debugging.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return $"VoiceState[ChannelID={ChannelID}, Deafened (Server)={Deafened} ({ServerDeafened}), Muted (Server)={Muted} ({ServerMuted}), Streaming={Streaming ?? false}, Suppressed={Suppressed}, UserID={UserID}, WebcamOn={WebcamOn}]";
		}

	}
}
