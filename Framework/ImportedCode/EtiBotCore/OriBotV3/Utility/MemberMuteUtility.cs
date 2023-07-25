using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiLogger.Logging;
using OldOriBot.Data.Commands.ArgData;
using OldOriBot.Data.MemberInformation;
using OldOriBot.Data.Persistence;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.PermissionData;
using OldOriBot.Utility.Extensions;

namespace OldOriBot.Utility {
	public sealed class MemberMuteUtility {

		internal static readonly Dictionary<Snowflake, MemberMuteUtility> Cache = new Dictionary<Snowflake, MemberMuteUtility>();

		/// <summary>
		/// The members that are muted by ID (keys) and when they get unmuted (values). A value of <see langword="default"/> means they must be manually unmuted.
		/// </summary>
		private readonly Dictionary<Snowflake, DateTimeOffset> MuteRemovalTimes = new Dictionary<Snowflake, DateTimeOffset>();

		/// <summary>
		/// The members that are muted by ID (keys) and when they were muted (values).
		/// </summary>
		private readonly Dictionary<Snowflake, DateTimeOffset> MuteAtTimes = new Dictionary<Snowflake, DateTimeOffset>();

		/// <summary>
		/// All muted users' IDs.
		/// </summary>
		public IReadOnlyList<Snowflake> MutedUserIDs => MuteRemovalTimes.Keys.ToList();

		/// <summary>
		/// The <see cref="BotContext"/> this exists in.
		/// </summary>
		public BotContext Context { get; private set; }

		/// <summary>
		/// The role for muted people.
		/// </summary>
		public Role Muted { get; private set; }

		/// <summary>
		/// Data Persistence associated with this 
		/// </summary>
		private DataPersistence Storage { get; set; }

		public void Invalidate() {
			DiscordClient.Current.OnHeartbeat -= Tick;
			Cache.Remove(Context.ID);
			Context = null;
			Muted = null;
			Storage = null;
		}

		private MemberMuteUtility(BotContext ctx) {
			Logger.Default.WriteLine("Made a new MemberMuteUtility " + ctx.ID, LogLevel.Trace);
			Context = ctx;
			IEnumerable<Role> r = Context.Server.Roles;
			Muted = r.First(role => role.Name == "Muted"); // Yes, this will throw. I want that.
			Cache[ctx.ID] = this;
			Storage = DataPersistence.GetPersistence(ctx, "muteinfo.cfg");

			List<ulong> alreadyParsedIds = new List<ulong>();
			foreach (string key in Storage.Keys) {
				Match match = Regex.Match(key, @"\d+");
				if (match.Success) {
					string memId = match.Groups[0].Value;
					ulong id = ulong.Parse(memId);
					if (alreadyParsedIds.Contains(id)) {
						continue;
					}
					alreadyParsedIds.Add(id);
				}
			}

			foreach (ulong id in alreadyParsedIds) {
				long start = Storage.TryGetType($"MUTE_START_{id}", 0L);
				long end = Storage.TryGetType($"MUTE_END_{id}", 0L);
				MuteAtTimes[id] = DateTimeOffset.FromUnixTimeSeconds(start);
				MuteRemovalTimes[id] = DateTimeOffset.FromUnixTimeSeconds(end);

				Member mbr = Context.Server.GetMemberAsync(id).Result;
				if (mbr != null && !mbr.LeftServer) {
					Storage.SetValue("MUTE_NAME_CACHE_" + mbr.ID, mbr.FullName); // update
				}
			}

			DiscordClient.Current.OnHeartbeat += Tick;
		}

		/// <summary>
		/// Returns the cached display name of the given member's ID.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public string GetCachedNameOf(Snowflake id) => Storage.GetValue("MUTE_NAME_CACHE_" + id);

		private bool IsMemberHigherRankingOrEqualToSelf(Member mbr) {
			if (mbr.LeftServer) return false;
			Role highestRole = null;
			int highestPos = 0;
			foreach (Role role in Context.Server.BotMember.Roles) {
				if (role.Position > highestPos) {
					highestPos = role.Position;
					highestRole = role;
				}
			}

			Role mbrHighestRole = null;
			highestPos = 0;
			
			foreach (Role role in mbr.Roles) {
				if (role.Position > highestPos) {
					highestPos = role.Position;
					mbrHighestRole = role;
				}
			}

			if (mbrHighestRole == null || highestRole == null) return false;
			return mbrHighestRole.Position >= highestRole.Position;
		}

		/// <summary>
		/// Mutes the given member.
		/// </summary>
		/// <param name="mbr">The member to mute.</param>
		/// <param name="whenUnmuted">The <see cref="DateTimeOffset"/> representing the time at which they will be unmuted.</param>
		/// <param name="reasonForLog">The reason to log and to DM to the user.</param>
		/// <param name="modWhoExecutedCommand">If run from the mute command, this is the person who did it.</param>
		/// <returns></returns>
		public async Task<ActionType> MuteMemberAsync(Member mbr, DateTimeOffset whenUnmuted, string reasonForLog, Member modWhoExecutedCommand = null) {
			if (mbr.GetPermissionLevel() >= PermissionLevel.Operator) throw new InvalidOperationException($"Cannot mute users with {PermissionLevel.Operator.GetFullName()} or above.", new NoThrowDummyException());
			bool alreadyExisted = MuteRemovalTimes.ContainsKey(mbr.ID);
			MuteRemovalTimes[mbr.ID] = whenUnmuted;
			MuteAtTimes[mbr.ID] = DateTimeOffset.UtcNow;

			InfractionLogProvider provider = InfractionLogProvider.GetProvider(Context);
			Member executor = modWhoExecutedCommand ?? Context.Server.BotMember;
			bool isBot = executor.IsSelf;

			if (mbr.LeftServer) {
				provider.AppendMute(executor, mbr.ID, reasonForLog, isBot);
				return ActionType.OnlyRegistered;
			}

			if (!alreadyExisted) {
				provider.AppendMute(executor, mbr.ID, reasonForLog, isBot);

				if (IsMemberHigherRankingOrEqualToSelf(mbr)) {
					return ActionType.OnlyRegistered;
				}
				Storage.SetArray("MUTE_ROLES_" + mbr.ID, mbr.Roles.ToIDArray(role => role.Integrated == false && role.ID != Context.Server.ID && role != Muted)); // ID check is for @everyone role
				Storage.SetValue("MUTE_START_" + mbr.ID, MuteAtTimes[mbr.ID].ToUnixTimeSeconds());
				Storage.SetValue("MUTE_END_" + mbr.ID, MuteRemovalTimes[mbr.ID].ToUnixTimeSeconds());
				Storage.SetValue("MUTE_NAME_CACHE_" + mbr.ID, mbr.FullName);

				List<Role> rolesOld = mbr.Roles.ToList();
				try {
					mbr.BeginChanges();
					mbr.Roles.Clear(role => role.Integrated == false && role.ID != Context.Server.ID); 
					// ^ Integrated check is for bots/nitro booster, and the ID check is for @everyone role (which has the same ID as the server)
					mbr.Roles.Add(Muted);
					mbr.CurrentVoiceChannel = null;
					var response = await mbr.ApplyChanges(reasonForLog);
					if (!response.IsSuccessStatusCode) {
						string mention;
						AllowedMentions allowedMentions;
						if (modWhoExecutedCommand == null) {
							allowedMentions = new AllowedMentions();
							allowedMentions.Roles.Add(603306540438388756);
							mention = "<@&603306540438388756>";
						} else {
							allowedMentions = new AllowedMentions();
							allowedMentions.Users.Add(modWhoExecutedCommand.ID);
							mention = $"<@!{modWhoExecutedCommand.ID}>";
						}

						await Context.ModerationLog.SendMessageAsync($"{mention} Member {mbr.Mention} needs to be manually given the muted role for {MuteAtTimes[mbr.ID].GetTimeDifferenceFrom(MuteRemovalTimes[mbr.ID])}. Reason: {reasonForLog}\n\n**They HAVE been added to the registry (so do NOT use the mute command) - please give them the muted role manually. The failure has been logged to the bot's console.**", null, allowedMentions);
						Context.ContextLogger.WriteCritical($"Member {mbr.ID} could not be muted! Reason: {response.StatusCode} {response.ReasonPhrase ?? "NULL_REASON"}", LogLevel.Info, true);

					} else {
						await Context.ModerationLog.SendMessageAsync($"Member {mbr.Mention} has been muted for {MuteAtTimes[mbr.ID].GetTimeDifferenceFrom(MuteRemovalTimes[mbr.ID])}. Reason: {reasonForLog}\n\n:information_source: The member's roles may still show up if you click / tap on them. This is a caching issue with the Discord client. Pressing Ctrl+R to reload the client or restarting the app should fix this.", null, AllowedMentions.AllowNothing);
					}
				} catch (Exception exc) {
					AllowedMentions allowedMentions = new AllowedMentions();
					allowedMentions.Roles.Add(603306540438388756);

					Context.ContextLogger.WriteCritical($"Failed to mute member {mbr.FullNickname}");
					Context.ContextLogger.WriteException(exc);
					await Context.ModerationLog.SendMessageAsync($"<@&603306540438388756> Failed to mute member {mbr.Mention}! This is pinging all mods because this may have come from an automatic system. Reason: An error was thrown: {exc.GetType().FullName} :: {exc.Message}\n\n**This member was still added to the bot's internal mute registry. __You need to MANUALLY add the muted role! When the bot recovers from this error, it will still unmute them after the given time completes.__** The member has **NOT** been DMed with the reason they were muted, so you will need to handle this too.", null, allowedMentions);
					return ActionType.OnlyRegistered;
				}
				await mbr.TrySendDMAsync($"You have been muted for unwanted behavior. Please remember to follow the server's rules! It is strongly advised that you review them in this time.\n\n**Reason logged:** {reasonForLog}\n\n:information_source: If you believe this action was an error (because errors do happen!), **PLEASE message a moderator that is online!** Staff are more than willing to help you in the event that something wrong happened, and if you tell us, we can prevent the error from happening in the future to other people.");
				return ActionType.Muted;
			} else {
				return ActionType.Nothing;
			}
		}

		/// <summary>
		/// Mutes the given member.
		/// </summary>
		/// <param name="mbr">The member to mute.</param>
		/// <param name="duration">How long the member will be muted.</param>
		/// <param name="reasonForLog">The reason to log and to DM to the user.</param>
		/// <param name="modWhoExecutedCommand">The moderator that issued the mute command, if applicable.</param>
		/// <returns></returns>
		public Task<ActionType> MuteMemberAsync(Member mbr, TimeSpan duration, string reasonForLog, Member modWhoExecutedCommand = null) {
			return MuteMemberAsync(mbr, DateTimeOffset.UtcNow + duration, reasonForLog, modWhoExecutedCommand);
		}

		/// <summary>
		/// Unmutes the given member.
		/// </summary>
		/// <param name="mbr"></param>
		/// <param name="reasonForLog"></param>
		/// <param name="modWhoExecutedCommand"></param>
		/// <returns></returns>
		public async Task<ActionType> UnmuteMemberAsync(Member mbr, string reasonForLog = "Mute time was completed.", Member modWhoExecutedCommand = null) {
			if (!MuteRemovalTimes.ContainsKey(mbr.ID) && !MuteAtTimes.ContainsKey(mbr.ID)) return ActionType.Nothing;
			InfractionLogProvider provider = InfractionLogProvider.GetProvider(Context);
			Member executor = modWhoExecutedCommand ?? Context.Server.BotMember;
			bool isBot = executor.IsSelf;

			bool exists = MuteRemovalTimes.ContainsKey(mbr.ID);
			MuteRemovalTimes.Remove(mbr.ID);
			MuteAtTimes.Remove(mbr.ID);
			if (mbr.LeftServer) {
				Storage.RemoveValue("MUTE_ROLES_" + mbr.ID);
				Storage.RemoveValue("MUTE_START_" + mbr.ID);
				Storage.RemoveValue("MUTE_END_" + mbr.ID);
				Storage.RemoveValue("MUTE_NAME_CACHE_" + mbr.ID);

				provider.AppendUnmute(executor, mbr.ID, reasonForLog, isBot);
				return ActionType.OnlyUnregistered;
			}

			if (!mbr.Roles.Contains(Muted)) return ActionType.Nothing;

			if (exists) {
				List<ulong> roles = Storage.GetListOfType<ulong>("MUTE_ROLES_" + mbr.ID);
				ActionType retnType = ActionType.Unmuted;
				try {
					mbr.BeginChanges(true);
					mbr.Roles.Remove(Muted);
					for (int idx = 0; idx < roles.Count; idx++) {
						Role targetRole = mbr.Server.GetRole(roles[idx]);
						if (targetRole != Muted) mbr.Roles.Add(targetRole);
					}
					var response = await mbr.ApplyChanges(reasonForLog);
					await mbr.TrySendDMAsync("Your mute time has been completed. Again, please remember to follow the server's rules.");
					if (!response.IsSuccessStatusCode) {
						string mention;
						AllowedMentions allowedMentions;
						if (modWhoExecutedCommand == null) {
							allowedMentions = new AllowedMentions();
							allowedMentions.Roles.Add(603306540438388756);
							mention = "<@&603306540438388756>";
						} else {
							allowedMentions = new AllowedMentions();
							allowedMentions.Users.Add(modWhoExecutedCommand.ID);
							mention = $"<@!{modWhoExecutedCommand.ID}>";
						}

						await Context.ModerationLog.SendMessageAsync($"{mention} Member {mbr.Mention} needs to be unmuted. Reason: {reasonForLog}\n\n**They HAVE BEEN REMOVED from the the registry (so do NOT use the mute command) - please give them the muted role manually. The failure has been logged to the bot's console.**", null, allowedMentions);
						Context.ContextLogger.WriteCritical($"Member {mbr.ID} could not be unmuted! Reason: {response.StatusCode} {response.ReasonPhrase ?? "NULL_REASON"}", LogLevel.Info, true);
					} else {
						await Context.ModerationLog.SendMessageAsync($"Member {mbr.Mention} has been unmuted. Reason: {reasonForLog}\n\n:information_source: The muted role may still show up if you click / tap on them. This is a caching issue with the Discord client. Pressing Ctrl+R to reload the client or restarting the app should fix this.", null, AllowedMentions.AllowNothing);
					}
				} catch (Exception exc) {
					string msg = $"ALERT: Unable to restore roles of member {mbr.Mention}! The roles they had were: ";
					foreach (ulong role in roles) {
						msg += "<@&" + role + ">, ";
					}
					msg += "\n\n**Please restore these roles manually.**";
					Context.ContextLogger.WriteException(exc);
					await Context.ModerationLog.SendMessageAsync(msg, null, AllowedMentions.AllowNothing);
					retnType = ActionType.OnlyUnregistered;
				}
				Storage.RemoveValue("MUTE_ROLES_" + mbr.ID);
				Storage.RemoveValue("MUTE_START_" + mbr.ID);
				Storage.RemoveValue("MUTE_END_" + mbr.ID);
				Storage.RemoveValue("MUTE_NAME_CACHE_" + mbr.ID);

				provider.AppendUnmute(executor, mbr.ID, reasonForLog, isBot);
				return retnType;
			}

			provider.AppendUnmute(executor, mbr.ID, reasonForLog, isBot);
			Storage.RemoveValue("MUTE_ROLES_" + mbr.ID);
			Storage.RemoveValue("MUTE_START_" + mbr.ID);
			Storage.RemoveValue("MUTE_END_" + mbr.ID);
			Storage.RemoveValue("MUTE_NAME_CACHE_" + mbr.ID);
			return ActionType.OnlyUnregistered;
		}

		/// <summary>
		/// Attempts to download a member from the given ID. If this fails, it will remove the ID from the registry.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="reasonForLog"></param>
		/// <param name="modWhoExecutedCommand"></param>
		/// <returns></returns>
		public async Task UnmuteMemberByIDAsync(Snowflake id, string reasonForLog = "Mute time was completed.", Member modWhoExecutedCommand = null) {
			if (!MuteRemovalTimes.ContainsKey(id) && !MuteAtTimes.ContainsKey(id)) return;
			InfractionLogProvider provider = InfractionLogProvider.GetProvider(Context);
			Member executor = modWhoExecutedCommand ?? Context.Server.BotMember;
			bool isBot = executor.IsSelf;

			Member mbr = await Context.Server.GetMemberAsync(id);
			if (mbr != null && !mbr.IsShallow) {
				await UnmuteMemberAsync(mbr, reasonForLog, modWhoExecutedCommand);
				return;
			}

			provider.AppendUnmute(executor, id, reasonForLog, isBot);
			MuteRemovalTimes.Remove(id);
			MuteAtTimes.Remove(id);

			Storage.RemoveValue("MUTE_ROLES_" + id);
			Storage.RemoveValue("MUTE_START_" + id);
			Storage.RemoveValue("MUTE_END_" + id);
			Storage.RemoveValue("MUTE_NAME_CACHE_" + id);
			await Context.ModerationLog.SendMessageAsync($"Member with ID {id} has been unmuted via the registry only (they left the server). Reason: {reasonForLog}", null, AllowedMentions.AllowNothing);
		}

		/// <summary>
		/// Returns whether or not the given ID is muted, and their mute removal time is in the future relative to when this is called.<para/>
		/// This method does not necessarily reflect if the member is <em>effectively</em> muted (e.g. not muted in registry, but they have the role).
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool IsMutedInRegistry(Snowflake id) {
			// They are registered, and the time they get unmuted at is ahead of now
			return MuteRemovalTimes.ContainsKey(id) && MuteRemovalTimes[id] > DateTimeOffset.UtcNow;
		}

		/// <summary>
		/// Returns whether or not this member is muted by either their roles or <see cref="IsMutedInRegistry(Snowflake)"/>.<para/>
		/// Generally, it is better to use <see cref="IsMutedInRegistry(Snowflake)"/> for determining how to handle a mute because it respects the time, and roles may desynchronize.<para/>
		/// This is mostly beneficial for cases where a moderator manually adding the muted role needs to be factored in.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">If <paramref name="member"/> is <see langword="null"/>.</exception>
		public bool IsMuted(Member member) {
			if (member == null) throw new ArgumentNullException(nameof(member));
			if (IsMutedInRegistry(member.ID)) return true;
			return member.Roles.Contains(Muted);
		}

		/// <summary>
		/// Returns when the user with the given ID was muted as a <see cref="DateTimeOffset"/>, or <see langword="default"/> if they are not muted.
		/// </summary>
		/// <returns></returns>
		public DateTimeOffset GetMutedAt(Snowflake id) {
			if (MuteAtTimes.TryGetValue(id, out DateTimeOffset at)) {
				return at;
			}
			return default;
		}

		/// <summary>
		/// Returns when the user with the given ID will be unmuted as a <see cref="DateTimeOffset"/>, or <see langword="default"/> if they are not muted.
		/// </summary>
		/// <returns></returns>
		public DateTimeOffset GetUnmutedAt(Snowflake id) {
			if (MuteRemovalTimes.TryGetValue(id, out DateTimeOffset at)) {
				return at;
			}
			return default;
		}

		/// <summary>
		/// Returns the length of the given user's mute, or <see langword="default"/> if they are not muted.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public TimeSpan GetMuteDuration(Snowflake id) {
			if (!IsMutedInRegistry(id)) return default;
			DateTimeOffset start = GetMutedAt(id);
			DateTimeOffset end = GetUnmutedAt(id);
			return end - start;
		}


		/// <summary>
		/// Returns the mute time left on the given ID, or throws <see cref="ArgumentException"/> if the ID is not muted.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public TimeSpan GetRemainingMuteTime(Snowflake id) {
			if (!IsMutedInRegistry(id)) throw new ArgumentException($"UserID {id} isn't muted!");
			return MuteRemovalTimes[id] - DateTimeOffset.UtcNow;
		}

		/// <summary>
		/// Adds the given <see cref="Duration"/> onto the given ID's mute time.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="duration"></param>
		public void AddMuteTime(Snowflake id, Duration duration) {
			DateTimeOffset removeAt = MuteRemovalTimes[id];
			long newTime = removeAt.ToUnixTimeSeconds() + (long)duration.TimeInSeconds;
			MuteRemovalTimes[id] = DateTimeOffset.FromUnixTimeSeconds(newTime);
			Storage.SetValue("MUTE_END_" + id, newTime);
		}

		/// <summary>
		/// Subtracts the given <see cref="Duration"/> from the given ID's mute time. If the new duration ends up putting it before now, they will be unmuted, hence why this is a <see cref="Task"/>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="duration"></param>
		/// <returns>True if the member is still muted.</returns>
		public async Task<bool> SubtractMuteTime(Snowflake id, Duration duration) {
			DateTimeOffset removeAt = MuteRemovalTimes[id];
			long newTime = removeAt.ToUnixTimeSeconds() - (long)duration.TimeInSeconds;
			DateTimeOffset newEnd = DateTimeOffset.FromUnixTimeSeconds(newTime);
			if (newEnd < DateTimeOffset.UtcNow) {
				await UnmuteMemberByIDAsync(id, "Mute duration was set to a value that caused their mute to end before now.");
				return false;
			} else {
				MuteRemovalTimes[id] = newEnd;
				Storage.SetValue("MUTE_END_" + id, newEnd.ToUnixTimeSeconds());
				return true;
			}
		}

		/// <summary>
		/// Sets the length of the given ID's mute time. If the new duration ends up putting it before now, they will be unmuted, hence why this is a <see cref="Task"/>
		/// </summary>
		/// <param name="id"></param>
		/// <param name="duration"></param>
		/// <returns>True if the member is still muted.</returns>
		public async Task<bool> SetMuteDuration(Snowflake id, Duration duration) {
			DateTimeOffset start = MuteAtTimes[id];
			long newTime = start.ToUnixTimeSeconds() + (long)duration.TimeInSeconds;
			DateTimeOffset newEnd = DateTimeOffset.FromUnixTimeSeconds(newTime);
			if (newEnd < DateTimeOffset.UtcNow) {
				await UnmuteMemberByIDAsync(id, "Mute duration was set to a value that caused their mute to end before now.");
				return false;
			} else {
				MuteRemovalTimes[id] = newEnd;
				Storage.SetValue("MUTE_END_" + id, newEnd.ToUnixTimeSeconds());
				return true;
			}
		}

		/// <summary>
		/// Ticks this <see cref="MemberMuteUtility"/>, which unmutes anyone that needs to be unmuted.
		/// </summary>
		/// <returns></returns>
		internal async Task Tick() {
			Dictionary<Snowflake, DateTimeOffset> dct = MuteRemovalTimes;
			foreach (KeyValuePair<Snowflake, DateTimeOffset> muteTime in dct) {
				if (muteTime.Value <= DateTimeOffset.UtcNow) {
					await UnmuteMemberByIDAsync(muteTime.Key);
				}
			}
		}

		/// <summary>
		/// Gets an existing <see cref="MemberMuteUtility"/> for this <see cref="BotContext"/> or creates a new one. Looks for a role named "Muted".
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		public static MemberMuteUtility GetOrCreate(BotContext ctx) {
			if (Cache.ContainsKey(ctx.ID)) {
				return Cache[ctx.ID];
			}
			return new MemberMuteUtility(ctx);
		}

		public enum ActionType {

			/// <summary>
			/// No action was performed.
			/// </summary>
			Nothing = -1,

			/// <summary>
			/// The member was muted.
			/// </summary>
			Muted = 0,

			/// <summary>
			/// The member was unmuted.
			/// </summary>
			Unmuted = 1,

			/// <summary>
			/// The member was already muted, so their mute time changed.
			/// </summary>
			MuteTimeChanged = 2,

			/// <summary>
			/// The member was added to the mute registry, but left the server so they don't have the role.
			/// </summary>
			OnlyRegistered = 3,

			/// <summary>
			/// The member was removed from the mute registry, but left the server so their roles will reset.
			/// </summary>
			OnlyUnregistered = 4

		}

	}
}
