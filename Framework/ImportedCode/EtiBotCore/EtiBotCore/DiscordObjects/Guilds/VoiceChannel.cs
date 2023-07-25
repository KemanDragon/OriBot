using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.Exceptions.Marshalling;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Threading;
using EtiBotCore.Voice;

namespace EtiBotCore.DiscordObjects.Guilds {

	/// <summary>
	/// Represents a voice channel in a guild.
	/// </summary>
	
	public class VoiceChannel : GuildChannelBase {

		#region Properties
		/// <summary>
		/// The bitrate of this channel in bits.
		/// </summary>
		/// <exception cref="PropertyLockedException">If the property is locked.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot modify channels.</exception>
		public int Bitrate {
			get => _Bitrate;
			set {
				EnforcePermissions(this, Payloads.Data.Permissions.ManageChannels);
				SetProperty(ref _Bitrate, value);
			}
		}
		private int _Bitrate;

		/// <summary>
		/// The maximum number of users in this channel at once. 0 means infinite users, 99 is the maximum.
		/// </summary>
		/// <exception cref="PropertyLockedException">If the property is locked.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot cannot modify channels.</exception>
		/// <exception cref="ArgumentOutOfRangeException">If the value is over 99.</exception>
		public int? UserLimit {
			get => _UserLimit;
			set {
				if (value < 0 || value > 99) throw new ArgumentOutOfRangeException(nameof(UserLimit));
				EnforcePermissions(this, Payloads.Data.Permissions.ManageChannels);
				SetProperty(ref _UserLimit, value);
			}
		}
		private int? _UserLimit = 0;
		#endregion

		#region Extended Code

		/// <summary>
		/// A list of every member that is connected to this voice channel.
		/// </summary>
		public IReadOnlyCollection<Member> ConnectedMembers => (IReadOnlyCollection<Member>)_ConnectedMembers.Values;
		private readonly ThreadedDictionary<Snowflake, Member> _ConnectedMembers = new ThreadedDictionary<Snowflake, Member>();

		/// <summary>
		/// Connect the bot to this voice channel for transmission.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InsufficientPermissionException">If the bot cannot join this channel.</exception>
		public async Task ConnectAsync() {
			Permissions allowed = Server.BotMember.GetPermissionsInChannel(this);
			if (!allowed.HasFlag(Payloads.Data.Permissions.ConnectVoice | Payloads.Data.Permissions.Speak)) {
				throw new InsufficientPermissionException(Payloads.Data.Permissions.ConnectVoice | Payloads.Data.Permissions.Speak);
			}
		}

		/// <summary>
		/// Disconnects the bot from this voice channel.
		/// </summary>
		/// <returns></returns>
		public async Task DisconnectAsync() {
		//	await VoiceConnectionMarshaller.DisconnectIfExists();
		}

		#endregion

		#region Internals & Implementation
		internal VoiceChannel(Payloads.PayloadObjects.Channel channel, Guild inServer) : base(channel, inServer, ChannelType.Voice) {
			Task updateTask = Update(channel, false);
			updateTask.Wait();
			// ^ None of the update tasks in this chain actually yield, so this is OK.
		}

		internal void UpdateVoiceStatesInternal() {
			IEnumerable<VoiceState> states = Server.VoiceStates.Where(state => state.ChannelID == ID);
			_ConnectedMembers.Clear();
			foreach (VoiceState state in states) {
				Member mbr = state.User.InServerAsync(Server).Result!;
				//_ConnectedMembers.Add(state.User.InServerAsync(Server).Result!);
				_ConnectedMembers[mbr.ID] = mbr;
			}
		}


		/// <inheritdoc/>
		protected internal override async Task Update(Payloads.PayloadDataObject obj, bool skipNonNullFields = false) {
			await base.Update(obj, skipNonNullFields);
			if (obj is Payloads.PayloadObjects.Channel channel) {
				_Bitrate = channel.Bitrate!.Value;
				_UserLimit = channel.UserLimit!.Value;
			}
		}

		/// <inheritdoc/>
		protected override async Task<HttpResponseMessage?> SendChangesToDiscord(IReadOnlyDictionary<string, object> changes, string? reasons) {
			APIRequestData request = await SendChangesToDiscordCustom(changes, reasons);
			if (changes.ContainsKey("Bitrate")) request.SetJsonField("bitrate", Bitrate);
			if (changes.ContainsKey("UserLimit")) request.SetJsonField("user_limit", UserLimit);
			return await ModifyChannel.ExecuteAsync(request);
		}
		#endregion
	}
}
