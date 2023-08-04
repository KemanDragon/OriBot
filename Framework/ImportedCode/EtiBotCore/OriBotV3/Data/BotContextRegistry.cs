using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Universal;
using EtiLogger.Logging;
using OldOriBot.CoreImplementation;
using OldOriBot.Interaction;
using OldOriBot.Utility;

namespace OldOriBot.Data {

	/// <summary>
	/// All bot contexts
	/// </summary>
	public static class BotContextRegistry {


		private static Dictionary<Snowflake, BotContext> ContextRegistry = new Dictionary<Snowflake, BotContext>();

		/// <summary>
		/// Should be run before the Discord Client has connected. This initializes bot contexts and connects them to events.<para/>
		/// This also handles the resetting of any context-dependant objects.
		/// </summary>
		public static void InitializeBotContexts() {
			Logger.Default.WriteLine("Initialized all BotContexts...");
			ContextRegistry = new Dictionary<Snowflake, BotContext> {
				//[763476814814511175] = new BotContextTestServer()
				[577548441878790146] = new BotContextOriTheGame()
			};

			DiscordClient.Current.Events.GuildEvents.OnGuildCreated += OnGuildCreated;
		}

		private static Task OnGuildCreated(Guild guild) {
			if (ContextRegistry.TryGetValue(guild.ID, out BotContext ctx)) {
				return ctx.OnGuildCreated(guild);
			}
			return Task.CompletedTask;
		}

		/// <summary>
		/// Returns a <see cref="BotContext"/> by its <see cref="Snowflake"/>.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static BotContext GetContext(Snowflake id) {
			return ContextRegistry[id];
		}

		/// <summary>
		/// Searches the <see cref="BotContextRegistry"/> for an instance of the given context type, and returns it.
		/// </summary>
		/// <param name="contextType"></param>
		/// <returns></returns>
		public static BotContext GetContext(Type contextType) {
			if (typeof(BotContext).IsAssignableFrom(contextType)) {
				return ContextRegistry.First(kvp => kvp.Value.GetType() == contextType).Value;
			}
			throw new ArgumentException("Input context type does not extend BotContext");
		}

		/// <summary>
		/// Searches the <see cref="BotContextRegistry"/> for an instance of <typeparamref name="T"/> and returns it.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T GetContext<T>() where T : BotContext {
			return (T)ContextRegistry.First(kvp => kvp.Value.GetType() == typeof(T)).Value;
		}

		/// <summary>
		/// Returns every instantiated <see cref="BotContext"/>
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<BotContext> GetContexts() {
			return ContextRegistry.Values;
		}
	}
}
