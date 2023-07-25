using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Payloads.Data {

	/// <summary>
	/// Represents a gateway intent, which describes what data the bot wants to receive.
	/// </summary>
	[Flags]
	public enum GatewayIntent {

		#region Presets and Aliases

		/// <summary>
		/// An invalid gateway intent. Attempting to send this will result in a response of 4013 / INVALID PAYLOAD.
		/// </summary>
		NULL = -1,

		/// <summary>
		/// All intents packaged into a single value.
		/// </summary>
		ALL = 0b111_1111_1111_1111,

		/// <summary>
		/// All intents that are privileged and must be enabled in the bot's configuration.
		/// </summary>
		ALL_PRIVILEGED_INTENTS = GUILD_MEMBERS | GUILD_PRESENCES,

		/// <summary>
		/// All DM-related events.
		/// </summary>
		ALL_DM_EVENTS = DIRECT_MESSAGES | DIRECT_MESSAGE_REACTIONS | DIRECT_MESSAGE_TYPING,

		/// <summary>
		/// All guild-related events that are not privileged.
		/// </summary>
		ALL_STANDARD_GUILD_EVENTS = ALL_GUILD_EVENTS ^ (ALL_PRIVILEGED_INTENTS),

		/// <summary>
		/// All guild-related events, including privileged events. Use <see cref="ALL_STANDARD_GUILD_EVENTS"/> to exclude privileged events.
		/// </summary>
		ALL_GUILD_EVENTS = ALL ^ ALL_DM_EVENTS,

		#endregion

		#region Standard

		/// <summary>
		/// The bot intends to receive no special data.<para/>
		/// As a result, the bot will only process the events that Discord sends by default (which are not locked behind any intents):
		/// <list type="bullet">
		/// <item>READY</item>
		/// <item>RESUMED</item>
		/// <item>VOICE_SERVER_UPDATE</item>
		/// <item>USER_UPDATE</item>
		/// </list>
		/// </summary>
		DEFAULT = 0,

		/// <summary>
		/// The bot intends to listen to or send events for core guild events. Included events are:
		/// <list type="bullet">
		/// <item>GUILD_CREATE</item>
		/// <item>GUILD_UPDATE</item>
		/// <item>GUILD_DELETE</item>
		/// <item>GUILD_ROLE_CREATE</item>
		/// <item>GUILD_ROLE_UPDATE</item>
		/// <item>GUILD_ROLE_DELETE</item>
		/// <item>CHANNEL_CREATE</item>
		/// <item>CHANNEL_UPDATE</item>
		/// <item>CHANNEL_DELETE</item>
		/// <item>CHANNEL_PINS_UPDATE</item>
		/// </list>
		/// </summary>
		GUILDS = 1 << 0,

		/// <summary>
		/// The bot intends to listen to or send events for guild membership events.<para/>
		/// <strong>This is a privileged event and must be explicitly enabled in the bot's control panel.</strong><para/>
		/// Included events are:
		/// <list type="bullet">
		/// <item>GUILD_MEMBER_ADD</item>
		/// <item>GUILD_MEMBER_UPDATE</item>
		/// <item>GUILD_MEMBER_REMOVE</item>
		/// </list>
		/// </summary>
		GUILD_MEMBERS = 1 << 1,

		/// <summary>
		/// The bot intends to listen to or send events for guild ban events. Included events are:
		/// <list type="bullet">
		/// <item>GUILD_BAN_ADD</item>
		/// <item>GUILD_BAN_REMOVE</item>
		/// </list>
		/// </summary>
		GUILD_BANS = 1 << 2,

		/// <summary>
		/// The bot intends to listen to or send events for guild emoji events. Included events are:
		/// <list type="bullet">
		/// <item>GUILD_EMOJIS_UPDATE</item>
		/// </list>
		/// </summary>
		GUILD_EMOJIS = 1 << 3,

		/// <summary>
		/// The bot intends to listen to or send events for guild integration events. Included events are:
		/// <list type="bullet">
		/// <item>GUILD_INTEGRATIONS_UPDATE</item>
		/// </list>
		/// </summary>
		GUILD_INTEGRATIONS = 1 << 4,

		/// <summary>
		/// The bot intends to listen to or send events for guild webhook events. Included events are:
		/// <list type="bullet">
		/// <item>WEBHOOKS_UPDATE</item>
		/// </list>
		/// </summary>
		GUILD_WEBHOOKS = 1 << 5,

		/// <summary>
		/// The bot intends to listen to or send events for guild invite events. Included events are:
		/// <list type="bullet">
		/// <item>INVITE_CREATE</item>
		/// <item>INVITE_DELETE</item>
		/// </list>
		/// </summary>
		GUILD_INVITES = 1 << 6,

		/// <summary>
		/// The bot intends to listen to or send events for guild voice state events. Included events are:
		/// <list type="bullet">
		/// <item>VOICE_STATE_UPDATE</item>
		/// </list>
		/// </summary>
		GUILD_VOICE_STATES = 1 << 7,

		/// <summary>
		/// The bot intends to listen to or send events for presence updates.<para/>
		/// <strong>This is a privileged event and must be explicitly enabled in the bot's control panel.</strong><para/>
		/// Included events are:
		/// <list type="bullet">
		/// <item>PRESENCE_UPDATE</item>
		/// </list>
		/// </summary>
		GUILD_PRESENCES = 1 << 8,

		/// <summary>
		/// The bot intends to listen to or send events for <strong>guild messages</strong>. Included events are:
		/// <list type="bullet">
		/// <item>MESSAGE_CREATE</item>
		/// <item>MESSAGE_UPDATE</item>
		/// <item>MESSAGE_DELETE</item>
		/// <item>MESSAGE_DELETE_BULK</item>
		/// </list>
		/// </summary>
		GUILD_MESSAGES = 1 << 9,

		/// <summary>
		/// The bot intends to listen to or send events for reactions on <strong>guild messages</strong>. Included events are:
		/// <list type="bullet">
		/// <item>MESSAGE_REACTION_ADD</item>
		/// <item>MESSAGE_REACTION_REMOVE</item>
		/// <item>MESSAGE_REACTION_REMOVE_ALL</item>
		/// <item>MESSAGE_REACTION_REMOVE_EMOJI</item>
		/// </list>
		/// </summary>
		GUILD_MESSAGE_REACTIONS = 1 << 10,

		/// <summary>
		/// The bot intends to listen to or send events for starting to type in <strong>guild channels</strong>. Included events are:
		/// <list type="bullet">
		/// <item>TYPING_START</item>
		/// </list>
		/// </summary>
		GUILD_MESSAGE_TYPING = 1 << 11,

		/// <summary>
		/// The bot intends to send and receive <strong>direct messages</strong>. Included events are:
		/// <list type="bullet">
		/// <item>MESSAGE_CREATE</item>
		/// <item>MESSAGE_UPDATE</item>
		/// <item>MESSAGE_DELETE</item>
		/// <item>MESSAGE_PINS_UPDATE</item>
		/// </list>
		/// </summary>
		DIRECT_MESSAGES = 1 << 12,

		/// <summary>
		/// The bot intends to listen to or send events for reactions on <strong>direct messages</strong>. Included events are:
		/// <list type="bullet">
		/// <item>MESSAGE_REACTION_ADD</item>
		/// <item>MESSAGE_REACTION_REMOVE</item>
		/// <item>MESSAGE_REACTION_REMOVE_ALL</item>
		/// <item>MESSAGE_REACTION_REMOVE_EMOJI</item>
		/// </list>
		/// </summary>
		DIRECT_MESSAGE_REACTIONS = 1 << 13,

		/// <summary>
		/// The bot intends to listen to or send events for starting to type in <strong>direct message</strong> channels. Included events are:
		/// <list type="bullet">
		/// <item>TYPING_START</item>
		/// </list>
		/// </summary>
		DIRECT_MESSAGE_TYPING = 1 << 14

		#endregion

	}
}
