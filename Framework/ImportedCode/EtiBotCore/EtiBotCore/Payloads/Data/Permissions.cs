using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// Represents permissions for a user or role.
	/// </summary>
	[Flags]
	public enum Permissions : ulong {

		/// <summary>
		/// Every single permission.
		/// </summary>
		// Friendly self reminder that the amount of 1s will be the bit shift + 1 (so <<31 means 32 1s)
		All = 0b_0011_1101_1111_1111_1111_1111_1111_1111_1111_1111,
		// n.b. hole "1101" because permision <<33 is skipped.

		/// <summary>
		/// No permissions available.
		/// </summary>
		None = 0,

		/// <summary>
		/// The creation of instant invites.
		/// </summary>
		CreateInstantInvite = 1u << 0,

		/// <summary>
		/// The ability to kick members from the server.
		/// </summary>
		KickMembers = 1u << 1,

		/// <summary>
		/// The ability to ban members from the server.
		/// </summary>
		BanMembers = 1u << 2,

		/// <summary>
		/// Full administrative permissions, which bypasses all other permissions.
		/// </summary>
		Administrator = 1u << 3,

		/// <summary>
		/// Alter the information, placement, and permissions of channels.
		/// </summary>
		ManageChannels = 1u << 4,

		/// <summary>
		/// Alter the information on the server.
		/// </summary>
		ManageGuild = 1u << 5,

		/// <summary>
		/// Add reactions to messages. 
		/// </summary>
		AddReactions = 1u << 6,

		/// <summary>
		/// View the server's audit log.
		/// </summary>
		ViewAuditLog = 1u << 7,

		/// <summary>
		/// Use Priority Speaker mode in voice chats.
		/// </summary>
		PrioritySpeaker = 1u << 8,

		/// <summary>
		/// Stream and use video in voice chats.
		/// </summary>
		Stream = 1u << 9,

		/// <summary>
		/// View this channel.
		/// </summary>
		ViewChannel = 1u << 10,

		/// <summary>
		/// Send messages in this channel.<para/>
		/// <strong>Note:</strong> For threads, if the user is a member of a thread, then they can chat in that thread even if this permission is false,
		/// granted they have the appropriate thread permission (<see cref="UsePublicThreads"/> or <see cref="UsePrivateThreads"/>, depending on what
		/// type of thread it is).
		/// </summary>
		SendMessages = 1u << 11,

		/// <summary>
		/// Send TTS messages in this channel.
		/// </summary>
		SendTTSMessages = 1u << 12,

		/// <summary>
		/// Delete or pin messages in this channel.
		/// </summary>
		ManageMessages = 1u << 13,

		/// <summary>
		/// Embed links in this channel.
		/// </summary>
		EmbedLinks = 1u << 14,

		/// <summary>
		/// Send files in this channel.
		/// </summary>
		AttachFiles = 1u << 15,

		/// <summary>
		/// View messages in a channel that were sent prior to login / viewing the channel
		/// </summary>
		ReadMessageHistory = 1u << 16,

		/// <summary>
		/// Mention @everyone and @here
		/// </summary>
		MentionEveryone = 1u << 17,

		/// <summary>
		/// Use emojis from other servers.
		/// </summary>
		UseExternalEmojis = 1u << 18,

		/// <summary>
		/// View guild insights, which is stats abotu the server.
		/// </summary>
		ViewGuildInsights = 1u << 19,

		/// <summary>
		/// Connect to this voice channel.
		/// </summary>
		ConnectVoice = 1u << 20,

		/// <summary>
		/// Speak in this voice channel.
		/// </summary>
		Speak = 1u << 21,

		/// <summary>
		/// Server mute people in voice channels.
		/// </summary>
		MuteMembers = 1u << 22,

		/// <summary>
		/// Server deafen people in voice channels.
		/// </summary>
		DeafenMembers = 1u << 23,

		/// <summary>
		/// Move members to other voice channels, or disconnect them.
		/// </summary>
		MoveMembers = 1u << 24,

		/// <summary>
		/// Use voice activity in voice channels.
		/// </summary>
		UseVAD = 1u << 25,

		/// <summary>
		/// Change their own nickname.
		/// </summary>
		ChangeNickname = 1u << 26,

		/// <summary>
		/// Change the nicknames of other people.
		/// </summary>
		ManageNicknames = 1u << 27,

		/// <summary>
		/// Manage the roles both they and other people have.
		/// </summary>
		ManageRoles = 1u << 28,

		/// <summary>
		/// Manage webhooks on this channel.
		/// </summary>
		ManageWebhooks = 1u << 29,

		/// <summary>
		/// Manage the emojis and stickers in this server.
		/// </summary>
		ManageEmojisAndStickers = 1u << 30,

		/// <summary>
		/// Use slash commands in this channel.
		/// </summary>
		UseSlashCommands = 1u << 31,

		/// <summary>
		/// Request to speak in this stage channel.
		/// </summary>
		RequestToSpeak = 1u << 32,

		/// <summary>
		/// Delete and archive threads, and automatically view private threads.
		/// </summary>
		ManageThreads = 1u << 34, // What is up with discord skipping numbers lol

		/// <summary>
		/// Create and participate in public threads.<para/>
		/// <strong>Note:</strong> Creating threads also requires <see cref="SendMessages"/>. See <see cref="SendMessages"/> for more information on how threads may bypass the permission.
		/// </summary>
		UsePublicThreads = 1u << 35,

		/// <summary>
		/// Create and participate in private threads.<para/>
		/// <strong>Note:</strong> Creating threads also requires <see cref="SendMessages"/>. See <see cref="SendMessages"/> for more information on how threads may bypass the permission.
		/// </summary>
		UsePrivateThreads = 1u << 36,

		/// <summary>
		/// Use custom stickers from other servers.
		/// </summary>
		UseExternalStickers = 1u << 37

	}

	/// <summary>
	/// The state of a permission.
	/// </summary>
	public enum PermissionState {

		/// <summary>
		/// When configuring permissions, this means it is on.
		/// </summary>
		Allow = 0,

		/// <summary>
		/// An alternative name for mnemonic purposes in role permissions.
		/// </summary>
		On = Allow,

		/// <summary>
		/// When configuring permissions, this means that it will inherit the value within the associated role.<para/>
		/// Alternatively, if this applies to a user, it will inherit the total permissions of the user (whatever their top role sets).<para/>
		/// It is important to note that permissions are calculated via a <strong>bit-wise OR</strong>. In Layman's terms, in your role configuration menu (the one for the entire server), once it's on, it will *stay* on for all higher roles (of course, this is based on what roles someone actually has, not what roles exist). For example, if I have a role called "Member" that allows viewing channels, and above it a role called "Muted" that has the little switch turned off, if someone has both of those roles, they will still be able to see channels because Member turned it on, and once it's on, it stays on. The only way to overwrite this is to change channel permissions.
		/// </summary>
		Inherit = 1,

		/// <summary>
		/// When configuring permissions, this means it is off.
		/// </summary>
		Deny = 2,

		/// <summary>
		/// An alternative name for mnemonic purposes in role permissions, as turning off a permission for roles does not necessarily deny it - if a different role turns it on and a user has both roles, then it will be on.
		/// </summary>
		Off = Deny,

	}
}
