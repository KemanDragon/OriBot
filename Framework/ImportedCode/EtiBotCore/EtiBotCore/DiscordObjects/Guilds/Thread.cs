using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Guilds.Specialized;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Exceptions.Marshalling;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EtiBotCore.DiscordObjects.Factory.SendableAPIRequestFactory;

namespace EtiBotCore.DiscordObjects.Guilds {

	/// <summary>
	/// This class represents a thread, which is a type of "sub-channel" present in servers.<para/>
	/// They are created (and inherit some attributes from) an existing channel, and cannot be created independently
	/// of some parent channel.
	/// </summary>
	public class Thread : TextChannel {

		#region Network Requests

		internal static readonly SendableAPIRequestFactory CreateNewThreadInstance = new SendableAPIRequestFactory("channels/{0}/threads", HttpRequestType.Post);

		internal static readonly SendableAPIRequestFactory JoinThread = new SendableAPIRequestFactory("channels/{0}/thread-members/@me", HttpRequestType.Put);

		internal static readonly SendableAPIRequestFactory AddThreadMember = new SendableAPIRequestFactory("channels/{0}/thread-members/{1}", HttpRequestType.Put);

		internal static readonly SendableAPIRequestFactory RemoveThreadMember = new SendableAPIRequestFactory("channels/{0}/thread-members/{1}", HttpRequestType.Delete);

		internal static readonly SendableAPIRequestFactory LeaveThread = new SendableAPIRequestFactory("channels/{0}/thread-members/@me", HttpRequestType.Delete);

		internal static readonly SendableAPIRequestFactory GetThreadMembers = new SendableAPIRequestFactory("channels/{0}/thread-members", HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory GetActiveThreads = new SendableAPIRequestFactory("channels/{0}/threads/active", HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory GetPublicArchivedThreads = new SendableAPIRequestFactory("channels/{0}/threads/archived/public", HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory GetPrivateArchivedThreads = new SendableAPIRequestFactory("channels/{0}/threads/archived/private", HttpRequestType.Get);

		internal static readonly SendableAPIRequestFactory GetArchivedPrivateThreadsSelfIsIn = new SendableAPIRequestFactory("channels/{0}/users/@me/threads/archived/private", HttpRequestType.Get);

		#endregion

		#region Thread Properties

		/// <summary>
		/// All members that have access to this thread.
		/// </summary>
		public IReadOnlyList<Member> Members => _Members.ToList().AsReadOnly();
		internal readonly SynchronizedCollection<Member> _Members = new SynchronizedCollection<Member>();

		/// <summary>
		/// A reference to the parent channel.
		/// </summary>
		public TextChannel ParentChannel {
			get {
				if (_ParentChannel == null && ParentID != null) {
					TextChannel? parent = GetFromCache<TextChannel>(ParentID.Value);
					_ParentChannel = parent;
					return parent!;
				}
				return _ParentChannel!;
			}
		}
		internal TextChannel? _ParentChannel = null;

		/// <summary>
		/// The metadata for this thread, which stores information like its archival/lock state and various time values.
		/// </summary>
		public ThreadMetadata Metadata { get; } = new ThreadMetadata();

		// TODO: Include the "message_count" and "member_count" fields? These are useless for bots according to Discord:
		// >>	message_count and member_count store an approximate count, but they stop counting at 50
		//		(these are only used in our UI, so likely are not valuable to bots)

		#endregion

		#region Overridden Behavior

		/// <inheritdoc/>
		public override bool NSFW { get => ParentChannel.NSFW; set => ParentChannel.NSFW = value; }

		/// <inheritdoc/>
		public override PermissionContainer Permissions => ParentChannel.Permissions;

		/// <inheritdoc/>
		/// <remarks>
		/// Calling this on a Thread instance creates a thread in the parent channel since it's not possible to create threads of threads.
		/// </remarks>
		public override Task<Thread?> CreateNewThread(string name, ThreadArchiveDuration archiveAfter, bool isPrivate, string? reason = null) => ParentChannel.CreateNewThread(name, archiveAfter, isPrivate, reason);

		#endregion

		#region Utility Methods and Properties

		#region For Managing Members

		/// <summary>
		/// Returns a reference to the <see cref="Member"/> representing the creator of this thread, or <see langword="null"/> if the member left the server and is not in cache.
		/// </summary>
		public Task<Member?> GetThreadCreatorAsync() => Server.GetMemberAsync(OwnerID!.Value);

		/// <summary>
		/// Tries to join this thread. Returns whether or not joining was successful.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InsufficientPermissionException">If this thread is private and the bot does not have the manage threads permission.</exception>
		public async Task<bool> TryJoinAsync() {
			if (IsPrivate) EnforcePermissions(Server, Payloads.Data.Permissions.ManageThreads);

			var requestResult = await JoinThread.ExecuteAsync(new APIRequestData {
				Params = {
					ID
				}
			});
			return requestResult?.StatusCode == System.Net.HttpStatusCode.NoContent;
		}

		/// <summary>
		/// Leave this thread. Raises <see cref="InvalidOperationException"/> if the bot is not a member of this thread.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">If this bot is not in the thread.</exception>
		public Task LeaveAsync() {
			if (!Members.Contains(Server.BotMember)) throw new InvalidOperationException("The bot is not part of this thread.");
			return LeaveThread.ExecuteAsync(new APIRequestData {
				Params = {
					ID
				}
			});
		}

		/// <summary>
		/// Tries to add the given member to this thread.
		/// </summary>
		/// <param name="mbr"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">If the member is not from the server this thread is in.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the manage threads permission.</exception>
		public async Task<bool> TryAddMemberToThread(Member mbr) {
			EnforcePermissions(Server, Payloads.Data.Permissions.ManageThreads);
			if (mbr.Server != Server) throw new ArgumentException("The given member is not from this server.");

			var requestResult = await AddThreadMember.ExecuteAsync(new APIRequestData {
				Params = {
					ID,
					mbr.ID
				}
			});
			return requestResult?.StatusCode == System.Net.HttpStatusCode.NoContent;
		}

		/// <summary>
		/// Tries to remove the given member from this thread.
		/// </summary>
		/// <param name="mbr"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">If the member is not from the server this thread is in, or the member is the creator of this thread and it's private</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the manage threads permission.</exception>
		public async Task<bool> TryRemoveMemberFromThread(Member mbr) {
			EnforcePermissions(Server, Payloads.Data.Permissions.ManageThreads);
			if (mbr.Server != Server) throw new ArgumentException("The given member is not from this server.");

			Member? creator = await GetThreadCreatorAsync();
			if (IsPrivate && mbr == creator) throw new ArgumentException("Cannot kick the creator of a private thread from their own thread!");

			var requestResult = await RemoveThreadMember.ExecuteAsync(new APIRequestData {
				Params = {
					ID,
					mbr.ID
				}
			});
			return requestResult?.StatusCode == System.Net.HttpStatusCode.NoContent;
		}

		/// <summary>
		/// Deletes this thread.
		/// </summary>
		/// <returns></returns>
		public Task DeleteAsync(string? reason = null) {
			EnforcePermissions(Server, Payloads.Data.Permissions.ManageThreads);
			return DeleteChannel.ExecuteAsync(new APIRequestData {
				Params = {
					ID
				},
				Reason = reason
			});
		}

		#endregion

		#region For Permissions And Type

		/// <summary>
		/// Whether or not this thread is public (be it because it's a news thread or a literal public thread).
		/// </summary>
		public bool IsPublic => Type == ChannelType.NewsThread || Type == ChannelType.PublicThread;

		/// <summary>
		/// Whether or not this thread is private because its type is <see cref="ChannelType.PrivateThread"/>.
		/// </summary>
		public bool IsPrivate => Type == ChannelType.PrivateThread;

		#endregion

		#endregion

		internal Thread(Payloads.PayloadObjects.Channel payload, TextChannel parentChannel, ChannelType type) : base(payload, parentChannel.Server, type) {
			ParentID = parentChannel.ID;
			_ParentChannel = parentChannel;
		}

		/// <inheritdoc/>
		protected internal override Task Update(PayloadDataObject obj, bool skipNonNullFields = false) {
			return base.Update(obj, skipNonNullFields);
		}

	}
}
