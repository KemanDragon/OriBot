using EtiBotCore.Client;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Guilds.MemberData;
using EtiBotCore.DiscordObjects.Guilds.Specialized;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Extension;
using OldOriBot.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OldOriBot.CoreImplementation.Handlers {
	public class HandlerRandomModSelector : PassiveHandler {
		public ManagedRole AnyModRole { get; }
		public override string Name { get; } = "Random Mod Selector";
		public override string Description { get; } = "Manages the existence of the @AnyMod role, which is handled by the bot to select a random suitable mod to ping. The primary purpose is to assist users in getting staff attention without having to spend time picking someone or pinging the whole role.";

		public IEnumerable<Member> AvailableMods = new Member[0];

		public HandlerRandomModSelector(BotContext ctx) : base(ctx) {
			AnyModRole = new ManagedRole(ctx.Server, "AnyMod");
			DiscordClient.Current!.Events.PresenceEvents.OnPresenceUpdated += OnPresenceUpdated;
			OnPresenceUpdated(null, null).Wait(); // Trigger a standard update. this has no async code so wait is OK
		}

		private Task OnPresenceUpdated(Presence _, Presence np) {
			if (np != null && (np.GuildID != Context.Server.ID)) return Task.CompletedTask; 
			if (!(Context is BotContextOriTheGame oriContext)) return Task.CompletedTask;

			// Get every mod. Start by trimming out offline / dnd mods.
			IEnumerable<Member> mods = oriContext.Server.FindMembersWithRole(oriContext.Server.GetRole(603306540438388756));
			mods = mods.Where(mod => {
				return (mod.Presence.Status == StatusType.Online || mod.Presence.Status == StatusType.Idle) && !mod.Roles.Contains(836933631950716938);
			});

			if (mods.Count() == 0) {
				// Well shit.
				AvailableMods = mods;
				return Task.CompletedTask;
			}

			// Good. Now we need to sort by importance. Do we have any mods that are not in a game and not streaming?
			IEnumerable<Member> modsNotStreaming = mods.Where(mod => mod.Presence.Activities.FirstOrDefault(activity => activity.Type != ActivityType.Streaming) != null);
			IEnumerable<Member> modsNotStreamingOrPlaying = modsNotStreaming.Where(mod => mod.Presence.Activities.FirstOrDefault(activity => activity.Type != ActivityType.Playing || activity.Name == "Visual Studio") != null);
			// ^ Explicit exclusion for visual studio. Visual studio is usually done by me and shouldn't stop @s

			if (modsNotStreamingOrPlaying.Count() == 0) {
				// Well shit. Everyone's occupied. At this point, it's best to pick someone who's playing a game rather than streaming because a stream
				// requires constant attention.

				AvailableMods = modsNotStreaming;
				// If modsNotStreaming is empty, then this does the emergency shit-hit-the-fan option
				// (setting to empty enumerable) which just tells it to ping all mods.
				return Task.CompletedTask;
			}

			// Okay, we have a few candidates.
			AvailableMods = modsNotStreamingOrPlaying;
			return Task.CompletedTask;
		}

		public override async Task<bool> ExecuteHandlerAsync(Member executor, BotContext executionContext, Message message) {
			if (!AnyModRole.IsInitialized) {
				await AnyModRole.Initialize();
			}

			if (message.Author.IsABot && message.Author.IsDiscordSystem) return false;

			if (message.Content.Contains("<@&" + AnyModRole.Role!.ID + ">")) {
				if (AvailableMods.Count() == 0) {
					await message.ReplyAsync("No mods are readily available! I have to ping the whole role so that whoever is here can get to you. It's no problem! <@&603306540438388756>");
				} else {
					Member mod = AvailableMods.Random();
					await message.ReplyAsync($"I've selected {mod.Mention} out of a random selection of the available mods. They should be here to lend a hand soon. If they don't show up after a few minutes, you might want to ping {AnyModRole.Name} again.");
				}
			}

			return false; // This doesn't ever intercept the message so it shouldn't stop other handlers
		}
	}
}
