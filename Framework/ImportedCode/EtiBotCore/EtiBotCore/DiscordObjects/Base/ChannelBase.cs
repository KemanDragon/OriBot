using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Extension;

namespace EtiBotCore.DiscordObjects.Base {

	/// <summary>
	/// The base class for all channels. While they are a single object, the API differentiates them between guild channel types and DM channel types.
	/// </summary>
	
	public abstract class ChannelBase : DiscordObject {

		#region Web Requests

		/// <summary>
		/// Params: <c>channelId</c>
		/// </summary>
		internal static readonly SendableAPIRequestFactory GetChannel = new SendableAPIRequestFactory("channels/{0}", SendableAPIRequestFactory.HttpRequestType.Get);

		/// <summary>
		/// Params: <c>channelId</c>
		/// </summary>
		internal static readonly SendableAPIRequestFactory ModifyChannel = new SendableAPIRequestFactory("channels/{0}", SendableAPIRequestFactory.HttpRequestType.Patch);

		/// <summary>
		/// Params: <c>channelId</c>
		/// </summary>
		internal static readonly SendableAPIRequestFactory DeleteChannel = new SendableAPIRequestFactory("channels/{0}", SendableAPIRequestFactory.HttpRequestType.Delete);

		#endregion

		#region Common Channel Info

		/// <summary>
		/// The jump link to this channel <c>#channelname</c> formatted with its ID so that it will always resolve, even in DMs (granted the user is in that server)
		/// </summary>
		public string Mention => $"<#{ID}>";

		/// <summary>
		/// The type of channel that this is.
		/// </summary>
		public ChannelType Type { get; }

		/// <summary>
		/// The ID of the latest message, or <see langword="null"/> if this channel cannot have messages (due to being a category or voice channel) or has no messages.
		/// </summary>
		public Snowflake? LastMessageID { get; protected set; }

		/// <summary>
		/// The timestamp of when the latest pinned message was added, or <see langword="null"/> if no messages are pinned.
		/// </summary>
		public ISO8601? LastPinTimestamp { get; protected set; }

		/// <summary>
		/// This can be a number of values, including:
		/// <list type="number">
		/// <item>The ID of the creator of a group DM (for group DMs, from which you must manually use the <see cref="Guild"/> to get a <see cref="Member"/>)</item>
		/// <item>The ID of the creator of a thread (for threads, from which <see cref="Thread.GetThreadCreatorAsync"/> can be used to acquire a <see cref="Member"/>)</item>
		/// <item><see langword="null"/> (for any other channel types)</item>
		/// </list>
		/// </summary>
		public Snowflake? OwnerID { get; protected set; }

		#endregion

		/// <inheritdoc/>
		protected ChannelBase(ulong channelId, ChannelType type) : base(channelId) {
			if (this is TextChannel) {
				if (type != ChannelType.Text && type != ChannelType.News && type != ChannelType.Store && !type.IsThreadChannel()) {
					throw new ArgumentException("Type can only be Text, News, or Store for text-based channels in a guild!", nameof(type));
				}
			}
			Type = type;
		}

		#region Networking

		/// <inheritdoc/>
		protected internal override Task Update(PayloadDataObject obj, bool skipNonNullFields = false) {
			if (obj is Payloads.PayloadObjects.Channel channel) {
				LastMessageID = channel.LastMessageID;
				LastPinTimestamp = channel.LastPinTimestamp;
				OwnerID = channel.OwnerID;
			}
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		protected override Task<HttpResponseMessage?> SendChangesToDiscord(IReadOnlyDictionary<string, object> changes, string? reasons) {
			// Don't use me, if you do. Make a version that returns APIRequestData
			// TODO: Allow type modifications?
			return Task.FromResult<HttpResponseMessage?>(null);
		}

		/// <inheritdoc/>
		protected virtual Task<APIRequestData> SendChangesToDiscordCustom(IReadOnlyDictionary<string, object> changes, string? reasons) {
			return Task.Run(() => {
				return new APIRequestData {
					Params = { ID },
					Reason = reasons
				};
			});
		}

		#endregion

	}
}
