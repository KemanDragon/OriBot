using System;
using System.Collections.Generic;
using System.Text;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// A type of action that the audit log records.
	/// </summary>
	public enum AuditLogActionType {

		/// <summary>
		/// Someone edited the server.
		/// </summary>
		GUILD_UPDATE = 1,

		/// <summary>
		/// Someone created a channel.
		/// </summary>
		CHANNEL_CREATE = 10,

		/// <summary>
		/// Someone edited a channel.
		/// </summary>
		CHANNEL_UPDATE = 11,

		/// <summary>
		/// Someone deleted a channel.
		/// </summary>
		CHANNEL_DELETE = 12,

		/// <summary>
		/// Someone added new permission information to a channel.
		/// </summary>
		CHANNEL_OVERWRITE_CREATE = 13,

		/// <summary>
		/// Someone changed permissions for someone.something on a channel.
		/// </summary>
		CHANNEL_OVERWRITE_UPDATE = 14,

		/// <summary>
		/// Someone removed permission information from a channel.
		/// </summary>
		CHANNEL_OVERWRITE_DELETE = 15,

		/// <summary>
		/// Someone kicked a member.
		/// </summary>
		MEMBER_KICK = 20,

		/// <summary>
		/// Someoen pruned members.
		/// </summary>
		MEMBER_PRUNE = 21,

		/// <summary>
		/// Someone got banned.
		/// </summary>
		MEMBER_BAN_ADD = 22,

		/// <summary>
		/// Someone got pardoned.
		/// </summary>
		MEMBER_BAN_REMOVE = 23,

		/// <summary>
		/// Someone edited someone else's nickname
		/// </summary>
		MEMBER_UPDATE = 24,

		/// <summary>
		/// Someone edited someone else's roles.
		/// </summary>
		MEMBER_ROLE_UPDATE = 25,

		/// <summary>
		/// Someone moved someone out of a voice channel.
		/// </summary>
		MEMBER_MOVE = 26,

		/// <summary>
		/// Someone disconnected someone from a voice channel.
		/// </summary>
		MEMBER_DISCONNECT = 27,

		/// <summary>
		/// Someone added a bot to the server.
		/// </summary>
		BOT_ADD = 28,

		/// <summary>
		/// Someone created a new role.
		/// </summary>
		ROLE_CREATE = 30,

		/// <summary>
		/// Someone updated a role.
		/// </summary>
		ROLE_UPDATE = 31,

		/// <summary>
		/// Someone deleted a role.
		/// </summary>
		ROLE_DELETE = 32,

		/// <summary>
		/// Someone made a new instant invite.
		/// </summary>
		INVITE_CREATE = 40,

		/// <summary>
		/// Someone changed the settings of an instant invite.
		/// </summary>
		INVITE_UPDATE = 41,

		/// <summary>
		/// Someone removed an instant invite.
		/// </summary>
		INVITE_DELETE = 42,

		/// <summary>
		/// Someone added a new webhook to the server.
		/// </summary>
		WEBHOOK_CREATE = 50,

		/// <summary>
		/// Someone changed information about a webhook on the server.
		/// </summary>
		WEBHOOK_UPDATE = 51,

		/// <summary>
		/// Someone deleted a webhook from the server.
		/// </summary>
		WEBHOOK_DELETE = 52,

		/// <summary>
		/// Someone added a new emoji.
		/// </summary>
		EMOJI_CREATE = 60,

		/// <summary>
		/// Someone changed an emoji's name.
		/// </summary>
		/// <remarks>
		/// As of writing, the only editable component is the name. You can probably tell this is for general emoji changes.
		/// </remarks>
		EMOJI_UPDATE = 61,

		/// <summary>
		/// Someone deleted an emoji.
		/// </summary>
		EMOJI_DELETE = 62,

		/// <summary>
		/// Someone deleted a message.
		/// </summary>
		MESSAGE_DELETE = 72,

		/// <summary>
		/// Someone deleted a lotta messages.
		/// </summary>
		MESSAGE_BULK_DELETE = 73,

		/// <summary>
		/// Someone pinned a message.
		/// </summary>
		MESSAGE_PIN = 74,

		/// <summary>
		/// Someone unpinned a message.
		/// </summary>
		MESSAGE_UNPIN = 75,

		/// <summary>
		/// Someone added a new integration to the server.
		/// </summary>
		INTEGRATION_CREATE = 80,

		/// <summary>
		/// Someone changed an integration on the server.
		/// </summary>
		INTEGRATION_UPDATE = 81,

		/// <summary>
		/// Someone removed an integration from the server.
		/// </summary>
		INTEGRATION_DELETE = 82,

		/// <summary>
		/// Someone created a stage.
		/// </summary>
		STAGE_INSTANCE_CREATE = 83,

		/// <summary>
		/// Someone modified a stage.
		/// </summary>
		STAGE_INSTANCE_UPDATE = 84,

		/// <summary>
		/// Someone deleted a stage.
		/// </summary>
		STAGE_INSTANCE_DELETE = 85,

		/// <summary>
		/// Someone added a sticker to the server.
		/// </summary>
		STICKER_CREATE = 90,

		/// <summary>
		/// Someone updated a sticker on the server.
		/// </summary>
		STICKER_UPDATE = 91,

		/// <summary>
		/// Someone deleted a sticker from the server.
		/// </summary>
		STICKER_DELETE = 92,

		/// <summary>
		/// Someone created a thread channel.
		/// </summary>
		THREAD_CREATE = 110,

		/// <summary>
		/// Someone updated a thread channel.
		/// </summary>
		THREAD_UPDATE = 111,

		/// <summary>
		/// Someone deleted a thread channel.
		/// </summary>
		THREAD_DELETE = 112

	}
}
