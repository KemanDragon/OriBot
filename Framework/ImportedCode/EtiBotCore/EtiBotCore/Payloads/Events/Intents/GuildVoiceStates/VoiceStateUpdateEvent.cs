using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads.PayloadObjects;
using EtiBotCore.Utility.Extension;

namespace EtiBotCore.Payloads.Events.Intents.GuildVoiceStates {

	/// <summary>
	/// Fired when the voice state of a member updates due to joining or leaving a channel.<para/>
	/// It is a <see cref="VoiceState"/> object.
	/// </summary>
	internal class VoiceStateUpdateEvent : VoiceState, IEvent {
		public async Task Execute(DiscordClient fromClient) {
			DiscordObjects.Universal.Guild? guild = null;
			if (GuildID != null) {
				guild = await DiscordObjects.Universal.Guild.GetOrDownloadAsync(GuildID.Value);
			}

			DiscordObjects.Universal.User? usr = await DiscordObjects.Universal.User.GetOrDownloadUserAsync(UserID);
			if (usr == null) {
				// The user this affects did not exist beforehand.
				var existing = DiscordObjects.Universal.VoiceState.GetStateForOrCreate(UserID);
				if (guild != null) {
					guild.RegisterAndUpdateVoiceState(existing);
				}

				existing.UpdateFrom(null, this);
				await fromClient.Events.VoiceStateEvents.OnVoiceStateChanged.Invoke(null, existing, GuildID, existing.ChannelID ?? default);
				return;
			}
			
			var voiceState = usr.VoiceState;
			var oldState = usr.VoiceState.MemberwiseClone(usr);
			bool wasConnected = voiceState.IsConnectedToVoice;
			Snowflake? channel = voiceState.ChannelID;
			voiceState.UpdateFrom(guild, this);
			if (wasConnected != voiceState.IsConnectedToVoice) {
				// connection changed
				if (channel == null) {
					// maybe it's not null now
					channel = voiceState.ChannelID;
				}
			}
			var mbrTask = guild?.GetMemberAsync(UserID);
			if (mbrTask != null) {
				var mbr = await mbrTask;
				if (mbr != null) {
					mbr._CurrentVoiceChannel = voiceState.Channel as DiscordObjects.Guilds.VoiceChannel;
				}
			}
			if (guild != null) {
				guild.RegisterAndUpdateVoiceState(voiceState);
			}
			await fromClient.Events.VoiceStateEvents.OnVoiceStateChanged.Invoke(oldState, voiceState, guild?.ID, channel.GetValueOrDefault());
		}
	}
}
