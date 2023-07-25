using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client.EventContainers;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.Exceptions.Marshalling;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Marshalling;
using SignalCore;

namespace EtiBotCore.Client {

	public partial class DiscordClient {

		/// <summary>
		/// Discord provides a number of intents that are privileged and cannot be used without being expressly enabled in the bot's app control panel.<para/>
		/// This property stores the intents that have been enabled. Valid cases are:
		/// <list type="bullet">
		/// <item><see cref="GatewayIntent.GUILD_MEMBERS"/></item>
		/// <item><see cref="GatewayIntent.GUILD_PRESENCES"/></item>
		/// </list>
		/// </summary>
		protected GatewayIntent ActivePrivelegedIntents = GatewayIntent.DEFAULT;

		/// <summary>
		/// The intents that are active in this connection. Inactive intents will not receive events, and attempting to reference their corresponding event handler will raise 
		/// </summary>
		public GatewayIntent ActiveIntents { get; private set; } = GatewayIntent.DEFAULT;

		/// <summary>
		/// All of the events that can fire for this client.
		/// </summary>
		public EventContainer Events { get; }

		/// <summary>
		/// An event that fires when a heartbeat is sent by Discord. This event will not reset. Only bind to it once.
		/// </summary>
		public Signal OnHeartbeat { get; set; } = new Signal();

		/// <summary>
		/// An event that fires when the bot reconnects to Discord. This event will not reset. Only bind to it once.
		/// </summary>
		public Signal OnReconnect { get; set; } = new Signal();

		/// <summary>
		/// A container class for the events.
		/// </summary>
		public class EventContainer {

			internal DiscordClient Client { get; }

			internal EventContainer(DiscordClient container) {
				Client = container;
			}

			/// <summary>
			/// Throws <see cref="GatewayIntentNotEnabledException"/> if the given intent is not enabled.
			/// </summary>
			/// <param name="intent"></param>
			internal void RequireIntent(GatewayIntent intent) {
				if (!Client.ActiveIntents.HasFlag(intent)) {
					throw new GatewayIntentNotEnabledException(intent);
				}
			}

			/// <summary>
			/// Identical to <see cref="RequireIntent(GatewayIntent)"/> in that it will throw <see cref="GatewayIntentNotEnabledException"/>, but in this case, if at least one of the two intents is enabled, it will NOT throw (they must both be missing to throw)<para/>
			/// As implied by the parameter names, this is intended for intents that have both a guild and DM counterpart.
			/// </summary>
			/// <param name="guildIntent"></param>
			/// <param name="dmIntent"></param>
			internal void RequireDualStateIntent(GatewayIntent guildIntent, GatewayIntent dmIntent) {
				if (!Client.ActiveIntents.HasFlag(guildIntent) && !Client.ActiveIntents.HasFlag(dmIntent)) {
					throw new GatewayIntentNotEnabledException(guildIntent | dmIntent);
				}
			}

			/// <summary>
			/// Most passthrough events (not all, as they are only relevant to the bot core). These do not require any intents.
			/// </summary>
			public EventContainerPassthrough PassthroughEvents { get; } = new EventContainerPassthrough();

			/// <summary>
			/// All events from the <see cref="GatewayIntent.GUILDS"/> intent are included in this object.
			/// </summary>
			/// <exception cref="GatewayIntentNotEnabledException">If the required intent is not enabled.</exception>
			public EventContainerGuilds GuildEvents {
				get {
					RequireIntent(GatewayIntent.GUILDS);
					return _GuildEvents;
				}
			}
			internal EventContainerGuilds _GuildEvents = new EventContainerGuilds();

			/// <summary>
			/// All events from the <see cref="GatewayIntent.GUILD_MEMBERS"/> intent are included in this object.
			/// </summary>
			/// <exception cref="GatewayIntentNotEnabledException">If the required intent is not enabled.</exception>
			public EventContainerMembers MemberEvents {
				get {
					RequireIntent(GatewayIntent.GUILD_MEMBERS);
					return _MemberEvents;
				}
			}
			internal EventContainerMembers _MemberEvents = new EventContainerMembers();

			/// <summary>
			/// All events from the <see cref="GatewayIntent.GUILD_BANS"/> intent are included in this object.
			/// </summary>
			/// <exception cref="GatewayIntentNotEnabledException">If the required intent is not enabled.</exception>
			public EventContainerBans BanEvents {
				get {
					RequireIntent(GatewayIntent.GUILD_BANS);
					return _BanEvents;
				}
			}
			internal EventContainerBans _BanEvents = new EventContainerBans();

			/// <summary>
			/// All events from the <see cref="GatewayIntent.GUILD_EMOJIS"/> intent are included in this object.
			/// </summary>
			/// <exception cref="GatewayIntentNotEnabledException">If the required intent is not enabled.</exception>
			public EventContainerEmojis EmojiEvents {
				get {
					RequireIntent(GatewayIntent.GUILD_EMOJIS);
					return _EmojiEvents;
				}
			}
			internal EventContainerEmojis _EmojiEvents = new EventContainerEmojis();


			/// <summary>
			/// All events from the <see cref="GatewayIntent.GUILD_INTEGRATIONS"/> intent are included in this object.
			/// </summary>
			/// <exception cref="GatewayIntentNotEnabledException">If the required intent is not enabled.</exception>
			public EventContainerIntegrations IntegrationEvents {
				get {
					RequireIntent(GatewayIntent.GUILD_INTEGRATIONS);
					return _IntegrationEvents;
				}
			}
			internal EventContainerIntegrations _IntegrationEvents = new EventContainerIntegrations();

			/// <summary>
			/// All events from the <see cref="GatewayIntent.GUILD_WEBHOOKS"/> intent are included in this object.
			/// </summary>
			/// <exception cref="GatewayIntentNotEnabledException">If the required intent is not enabled.</exception>
			public EventContainerWebhooks WebhookEvents {
				get {
					RequireIntent(GatewayIntent.GUILD_WEBHOOKS);
					return _WebhookEvents;
				}
			}
			internal EventContainerWebhooks _WebhookEvents = new EventContainerWebhooks();

			/// <summary>
			/// All events from the <see cref="GatewayIntent.GUILD_INVITES"/> intent are included in this object.
			/// </summary>
			/// <exception cref="GatewayIntentNotEnabledException">If the required intent is not enabled.</exception>
			public EventContainerInvites InviteEvents {
				get {
					RequireIntent(GatewayIntent.GUILD_INVITES);
					return _InviteEvents;
				}
			}
			internal EventContainerInvites _InviteEvents = new EventContainerInvites();

			/// <summary>
			/// All events from the <see cref="GatewayIntent.GUILD_VOICE_STATES"/> intent are included in this object.
			/// </summary>
			/// <exception cref="GatewayIntentNotEnabledException">If the required intent is not enabled.</exception>
			public EventContainerVoiceStates VoiceStateEvents {
				get {
					RequireIntent(GatewayIntent.GUILD_VOICE_STATES);
					return _VoiceStateEvents;
				}
			}
			internal EventContainerVoiceStates _VoiceStateEvents = new EventContainerVoiceStates();

			/// <summary>
			/// All events from the <see cref="GatewayIntent.GUILD_PRESENCES"/> intent are included in this object.
			/// </summary>
			public EventContainerPresences PresenceEvents {
				get {
					RequireIntent(GatewayIntent.GUILD_PRESENCES);
					return _PresenceEvents;
				}
			}
			internal EventContainerPresences _PresenceEvents = new EventContainerPresences();

			/// <summary>
			/// All events from the <see cref="GatewayIntent.GUILD_MESSAGES"/> and <see cref="GatewayIntent.DIRECT_MESSAGES"/> intents are included in this object.<para/>
			/// Some events may or may not fire in this container depending on the intents. Said events will be marked.
			/// </summary>
			/// <exception cref="GatewayIntentNotEnabledException">If the required intent is not enabled.</exception>
			public EventContainerMessages MessageEvents {
				get {
					RequireDualStateIntent(GatewayIntent.GUILD_MESSAGES, GatewayIntent.DIRECT_MESSAGES);
					return _MessageEvents;
				}
			}
			internal EventContainerMessages _MessageEvents = new EventContainerMessages();

			/// <summary>
			/// All events from the <see cref="GatewayIntent.GUILD_MESSAGE_REACTIONS"/> and <see cref="GatewayIntent.DIRECT_MESSAGE_REACTIONS"/> intents are included in this object.
			/// </summary>
			/// <exception cref="GatewayIntentNotEnabledException">If the required intent is not enabled.</exception>
			public EventContainerReactions ReactionEvents {
				get {
					RequireDualStateIntent(GatewayIntent.GUILD_MESSAGE_REACTIONS, GatewayIntent.DIRECT_MESSAGE_REACTIONS);
					return _ReactionEvents;
				}
			}
			internal EventContainerReactions _ReactionEvents = new EventContainerReactions();

			/// <summary>
			/// All events from the <see cref="GatewayIntent.GUILD_MESSAGE_TYPING"/> and <see cref="GatewayIntent.DIRECT_MESSAGE_TYPING"/> intents are included in this object.
			/// </summary>
			/// <exception cref="GatewayIntentNotEnabledException">If the required intent is not enabled.</exception>
			public EventContainerTyping TypingEvents {
				get {
					RequireDualStateIntent(GatewayIntent.GUILD_MESSAGE_TYPING, GatewayIntent.DIRECT_MESSAGE_TYPING);
					return _TypingEvents;
				}
			}
			internal EventContainerTyping _TypingEvents = new EventContainerTyping();
		}
	}
}
