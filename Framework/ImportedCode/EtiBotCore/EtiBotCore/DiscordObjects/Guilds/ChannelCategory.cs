using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Exceptions.Marshalling;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Threading;

namespace EtiBotCore.DiscordObjects.Guilds {

	/// <summary>
	/// Represents a channel category.
	/// </summary>
	public class ChannelCategory : GuildChannelBase {

		/// <summary>
		/// The channels within this category. This may not be in order of position.
		/// Note that this may not be in order of position. Channels in guilds implement <see cref="IComparable{T}"/>, so it is possible to use <see cref="Array.Sort{T}(T[])"/> on this.
		/// </summary>
		public IReadOnlyCollection<GuildChannelBase> Children => (IReadOnlyCollection<GuildChannelBase>)_Children.Values;
		private readonly ThreadedDictionary<Snowflake, GuildChannelBase> _Children = new ThreadedDictionary<Snowflake, GuildChannelBase>();

		/// <summary>
		/// For internal registration, this puts a channel in the list of children.
		/// </summary>
		/// <param name="channel"></param>
		internal void AddChannel(GuildChannelBase channel) {
			// _Children.Add(channel);
			if (channel is ChannelCategory) throw new InvalidOperationException("Cannot add a channel category as a child of a channel category.");
			_Children[channel.ID] = channel; // Use this method because channels are singletons for a given ID, so the only chance of concurrent modification will be for the same instance.
		}

		/// <summary>
		/// For internal deregistration, this puts a channel in the list of children.
		/// </summary>
		/// <param name="channel"></param>
		internal void RemoveChannel(GuildChannelBase channel) {
			//_Children.Remove(channel);
			if (channel is ChannelCategory) throw new InvalidOperationException("Cannot add a channel category as a child of a channel category.");
			_Children.Remove(channel.ID, out GuildChannelBase _);
		}


		/// <summary>
		/// Construct a new channel category.
		/// </summary>
		internal ChannelCategory(Payloads.PayloadObjects.Channel channel, Guild inServer) : base(channel, inServer, ChannelType.Category) {
			Task updateTask = Update(channel, false);
			updateTask.Wait();
			// ^ None of the update tasks in this chain actually yield, so this is OK.
		}

		/// <inheritdoc/>
		protected internal override Task Update(Payloads.PayloadDataObject obj, bool skipNonNullFields = false) {
			return base.Update(obj, skipNonNullFields);
			// ^ None of the update tasks in this chain actually yield, so this is OK.
		}

		/// <inheritdoc/>
		protected override async Task<HttpResponseMessage?> SendChangesToDiscord(IReadOnlyDictionary<string, object> changes, string? reasons) {
			APIRequestData data = await SendChangesToDiscordCustom(changes, reasons);
			// ^ This set ID parameter on its own.

			return await ModifyChannel.ExecuteAsync(data);
		}
	}
}
