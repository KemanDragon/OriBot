using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Guilds.Specialized;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Utility.Extension;
using EtiLogger.Data.Structs;
using OldOriBot.CoreImplementation.Commands;
using OldOriBot.CoreImplementation.Handlers;
using OldOriBot.Data.Persistence;
using OldOriBot.Interaction;
using OldOriBot.Utility;

namespace OldOriBot.CoreImplementation {
	public class BotContextTestServer : BotContext {
		public override string Name { get; } = "Test Server Bot Context";
		public override string DataPersistenceName { get; } = "ctxTest";
		protected override Snowflake ServerID { get; } = 763476814814511175;
		protected override Snowflake EventLogID { get; } = 790160313911083029;
		protected override Snowflake MessageBehaviorLogID { get; } = 798067505234051112;
		protected override Snowflake VoiceBehaviorLogID { get; } = 798067413048229918;
		protected override Snowflake MembershipLogID { get; } = 798067378238914584;
		public override Snowflake? BotChannelID { get; } = 779024666832535563;
		public override bool OnlyAllowCommandsInBotChannel { get; } = true;
		public override bool DownloadsAllMembers { get; } = true;
		public override Command[] Commands { get; set; }
		public override PassiveHandler[] Handlers { get; set; }

		private bool UseAuditLogToGetStragglers => Storage.TryGetType("UseAuditLogToFindMissingData", true);

		public BotContextTestServer() {
			
		}

		public ManagedRole SpiritsRole;
		public ManagedRole PCMainRole;
		public ManagedRole XBMainRole;
		public ManagedRole SWMainRole;
		public ManagedRole PCRole;
		public ManagedRole XBRole;
		public ManagedRole SWRole;
		public ManagedRole ImagesRole;

		public override async Task AfterContextInitialization() {
			CommandProfile profileCmd = new CommandProfile(this);
			CommandProfile.CommandMiniProfileDeprecated miniProfile = new CommandProfile.CommandMiniProfileDeprecated((CommandProfile.CommandProfileMini)profileCmd.Subcommands.First(cmd => cmd is CommandProfile.CommandProfileMini));
			CommandMute mute = new CommandMute(this);
			CommandUnmute unmute = new CommandUnmute((CommandMute.CommandMuteRemove)mute.Subcommands.FirstOrDefault(cmd => cmd is CommandMute.CommandMuteRemove));
			Commands = new Command[] {
				//new CommandComplicated(this),
				new CommandMute(this),
				unmute,
				new CommandGiveMe(this),
				new CommandColorMe(this),
				new CommandWhoMade(this),
				profileCmd,
				miniProfile,
				new CommandMusic(this),
				new CommandNowPlaying(this),
				new CommandAllowMassPing(this),
				new CommandPurge(this),
				new CommandHug(this)
			};
			Handlers = new PassiveHandler[] {
				new HandlerArtPinSystem(Server, this), // Has no interceptions
				new HandlerAntiSpamSystem(this),
				new HandlerAntiCopypasta(this),

				// Below: has no interceptions
				new HandlerProfileExperienceReward(this),
				new HandlerSteamSystem(this),
				new HandlerPassiveResponseSystem(this),
				new HandlerSecurityFilter(this)
			};

			(Commands.Where(cmd => cmd.GetType() == typeof(CommandColorMe)).FirstOrDefault() as CommandColorMe).InstantiateAllColors(this);
			(Commands.Where(cmd => cmd.GetType() == typeof(CommandGiveMe)).FirstOrDefault() as CommandGiveMe).NameToRoleBindings = new Dictionary<string, ManagedRole> {
				["events"] = new ManagedRole(
					Server,
					"Events"
				),
				["minorannouncements"] = new ManagedRole(
					Server,
					"MinorAnnouncements"
				),
				["botupdates"] = new ManagedRole(
					Server,
					"BotUpdates"
				),
				["completedbf"] = new ManagedRole(
					Server,
					"Completed Ori: BF"
				),
				["completedwotw"] = new ManagedRole(
					Server,
					"Completed Ori: WotW"
				),

				// flair //
				["artist"] = new ManagedRole(
					Server,
					"Visual Artist"
				),
				["author"] = new ManagedRole(
					Server,
					"Author"
				),
				["musician"] = new ManagedRole(
					Server,
					"Musician"
				),
				["photographer"] = new ManagedRole(
					Server,
					"Photographer"
				),
				["chef"] = new ManagedRole(
					Server,
					"Chef"
				)
			};
			DataPersistence.RegisterDomains(this);

			var msgContainer = DiscordClient.Current!.Events.MessageEvents;
			var voiceContainer = DiscordClient.Current!.Events.VoiceStateEvents;
			var mbrContainer = DiscordClient.Current!.Events.MemberEvents;
			var banContainer = DiscordClient.Current!.Events.BanEvents;

			SpiritsRole = new ManagedRole(Server, "Spirits");
			PCMainRole = new ManagedRole(Server, "PC Mains", new Color(0x7799c4));
			XBMainRole = new ManagedRole(Server, "Xbox Mains", new Color(0x107c10));
			SWMainRole = new ManagedRole(Server, "Switch Mains", new Color(0xE74C3C));
			PCRole = new ManagedRole(Server, "PC");
			XBRole = new ManagedRole(Server, "Xbox");
			SWRole = new ManagedRole(Server, "Switch");
			ImagesRole = new ManagedRole(Server, "Images");

			await SpiritsRole.Initialize();
			await PCMainRole.Initialize();
			await XBMainRole.Initialize();
			await SWMainRole.Initialize();
			await PCRole.Initialize();
			await XBRole.Initialize();
			await SWRole.Initialize();
			await ImagesRole.Initialize();

			msgContainer.OnMessageDeleted += OnMessageDeleted;
			msgContainer.OnMessageEdited += OnMessageEdited;
			msgContainer.OnMessagesBulkDeleted += OnMessagesBulkDeleted;
			voiceContainer.OnVoiceStateChanged += OnVoiceStateChanged;
			mbrContainer.OnGuildMemberAdded += OnMemberJoined;
			mbrContainer.OnGuildMemberRemoved += OnMemberLeft;
			mbrContainer.OnGuildMemberUpdated += OnMemberUpdated;
			banContainer.OnMemberBanned += OnMemberBanned;
			banContainer.OnMemberUnbanned += OnMemberPardoned;

			DiscordClient.Current!.Events.ReactionEvents.OnReactionAdded += OnReactionAdded;
			DiscordClient.Current!.Events.ReactionEvents.OnReactionRemoved += OnReactionRemoved;
		}

		#region welcome-readme

		/// <summary>
		/// Returns the member's main platform role, or <see langword="null"/> if they don't have one.
		/// </summary>
		/// <param name="mbr"></param>
		/// <returns></returns>
		private Role GetMemberMainRole(Member mbr) {
			if (mbr.Roles.Contains(PCMainRole.Role)) return PCMainRole.Role;
			if (mbr.Roles.Contains(XBMainRole.Role)) return XBMainRole.Role;
			if (mbr.Roles.Contains(SWMainRole.Role)) return SWMainRole.Role;
			return null;
		}

		/// <summary>
		/// Of the available platform roles, this returns the "highest" role (PC => Xbox => Switch), or <see langword="null"/> if they don't have any.<para/>
		/// <strong>This targets secondary roles, not main roles.</strong>
		/// </summary>
		/// <param name="mbr"></param>
		/// <returns></returns>
		private Role GetHighestRankingPlatformRole(Member mbr, Role exclude) {
			if (mbr.Roles.Contains(PCRole.Role) && exclude != PCRole.Role) return PCRole.Role;
			if (mbr.Roles.Contains(XBRole.Role) && exclude != XBRole.Role) return XBRole.Role;
			if (mbr.Roles.Contains(SWRole.Role) && exclude != SWRole.Role) return SWRole.Role;
			return null;
		}

		/// <summary>
		/// Given a secondary platform role, this returns the main variant.
		/// </summary>
		/// <param name="role"></param>
		/// <returns></returns>
		private Role ToMainType(Role role) {
			if (role == PCRole.Role) return PCMainRole.Role;
			if (role == XBRole.Role) return XBMainRole.Role;
			if (role == SWRole.Role) return SWMainRole.Role;
			throw new ArgumentException();
		}

		/// <summary>
		/// Given a main platform role, this returns the second variant.
		/// </summary>
		/// <param name="role"></param>
		/// <returns></returns>
		private Role ToSecondType(Role role) {
			if (role == PCMainRole.Role) return PCRole.Role;
			if (role == XBMainRole.Role) return XBRole.Role;
			if (role == SWMainRole.Role) return SWRole.Role;
			throw new ArgumentException();
		}

		private async Task OnReactionAdded(Message message, Emoji emoji, User user) {
			if (message.Channel.ID != 799072828032942100) return;
			if (message.ID != 799072847259107358) return;
			if (user.IsABot || user.IsSelf) return;
			Member member = await user.InServerAsync(Server);
			Role mainRole = GetMemberMainRole(member);
			if (emoji.ID == 671886904773443584 || emoji.Name == "ori") {
				// ori
				if (!member.Roles.Contains(SpiritsRole.Role)) {
					member.BeginChanges(true);
					member.Roles.Add(SpiritsRole.Role);
					await member.ApplyChanges("Clicked on Ori, granted Spirits role.");
				}
			} else if (emoji.ID == 685306067957055549 || emoji.Name == "Windows") {
				// veendoze
				Role target = null;
				if (mainRole != null) {
					if (!member.Roles.Contains(PCRole.Role) && mainRole != PCMainRole.Role) {
						target = PCRole.Role;
					}
				} else {
					if (!member.Roles.Contains(PCMainRole.Role)) {
						target = PCMainRole.Role;
					}
				}
				if (target != null) {
					member.BeginChanges(true);
					member.Roles.Add(target);
					await member.ApplyChanges("Clicked on Windows logo, granted applicable PC role.");
				}
			} else if (emoji.ID == 685306067932151841 || emoji.Name == "Xbox") {
				// echsbacks
				Role target = null;
				if (mainRole != null) {
					if (!member.Roles.Contains(XBRole.Role) && mainRole != XBMainRole.Role) {
						target = XBRole.Role;
					}
				} else {
					if (!member.Roles.Contains(XBMainRole.Role)) {
						target = XBMainRole.Role;
					}
				}
				if (target != null) {
					member.BeginChanges(true);
					member.Roles.Add(target);
					await member.ApplyChanges("Clicked on Xbox logo, granted applicable Xbox role.");
				}
			} else if (emoji.Name == "🔴") {
				// red dot for switch
				Role target = null;
				if (mainRole != null) {
					if (!member.Roles.Contains(SWRole.Role) && mainRole != SWMainRole.Role) {
						target = SWRole.Role;
					}
				} else {
					if (!member.Roles.Contains(SWMainRole.Role)) {
						target = SWMainRole.Role;
					}
				}
				if (target != null) {
					member.BeginChanges(true);
					member.Roles.Add(target);
					await member.ApplyChanges("Clicked on Switch logo, granted applicable Switch role.");
				}
			} else {
				// no
				await message.Reactions.RemoveReactionsOfEmojiAsync(emoji, "Message is restricted on which reactions can be added.");
			}
		}

		private async Task OnReactionRemoved(Message message, Emoji emoji, User user) {
			if (message.Channel.ID != 799072828032942100) return;
			if (message.ID != 799072847259107358) return;
			if (user.IsABot || user.IsSelf) return;
			Member member = await user.InServerAsync(Server);
			Role mainRole = GetMemberMainRole(member);
			if (emoji.ID == 685306067957055549 || emoji.Name == "Windows") {
				// veendoze
				if (mainRole == PCMainRole.Role) {
					Role nextBest = GetHighestRankingPlatformRole(member, PCRole.Role);
					if (nextBest != null) {
						Role asMain = ToMainType(nextBest);
						member.BeginChanges(true);
						member.Roles.Remove(nextBest);
						member.Roles.Remove(PCMainRole.Role);
						member.Roles.Add(asMain);
						await member.ApplyChanges($"Removed main PC role, swapped for next highest platform role, which was {asMain.Name}");
					} else {
						member.BeginChanges(true);
						member.Roles.Remove(PCMainRole.Role);
						await member.ApplyChanges($"Removed main PC role, and did not substitute it because they had no other roles.");
					}
				} else {
					if (member.Roles.Contains(PCRole.Role)) {
						member.BeginChanges(true);
						member.Roles.Remove(PCRole.Role);
						await member.ApplyChanges("Removed secondary PC role.");
					}
				}
			} else if (emoji.ID == 685306067932151841 || emoji.Name == "Xbox") {
				// echsbacks
				if (mainRole == XBMainRole.Role) {
					Role nextBest = GetHighestRankingPlatformRole(member, XBRole.Role);
					if (nextBest != null) {
						Role asMain = ToMainType(nextBest);
						member.BeginChanges(true);
						member.Roles.Remove(nextBest);
						member.Roles.Remove(XBMainRole.Role);
						member.Roles.Add(asMain);
						await member.ApplyChanges($"Removed main Xbox role, swapped for next highest platform role, which was {asMain.Name}");
					} else {
						member.BeginChanges(true);
						member.Roles.Remove(XBMainRole.Role);
						await member.ApplyChanges($"Removed main Xbox role, and did not substitute it because they had no other roles.");
					}
				} else {
					if (member.Roles.Contains(XBRole.Role)) {
						member.BeginChanges(true);
						member.Roles.Remove(XBRole.Role);
						await member.ApplyChanges("Removed secondary Xbox role.");
					}
				}
			} else if (emoji.Name == "🔴") {
				// red dot for switch
				if (mainRole == SWMainRole.Role) {
					Role nextBest = GetHighestRankingPlatformRole(member, SWRole.Role);
					if (nextBest != null) {
						Role asMain = ToMainType(nextBest);
						member.BeginChanges(true);
						member.Roles.Remove(nextBest);
						member.Roles.Remove(SWMainRole.Role);
						member.Roles.Add(asMain);
						await member.ApplyChanges($"Removed main Switch role, swapped for next highest platform role, which was {asMain.Name}");
					} else {
						member.BeginChanges(true);
						member.Roles.Remove(SWMainRole.Role);
						await member.ApplyChanges($"Removed main Switch role, and did not substitute it because they had no other roles.");
					}
				} else {
					if (member.Roles.Contains(SWRole.Role)) {
						member.BeginChanges(true);
						member.Roles.Remove(SWRole.Role);
						await member.ApplyChanges("Removed secondary Switch role.");
					}
				}
			}
		}

		#endregion

		#region Logging

		// JOINS/LEAVES
		private static readonly Color GREENISH = new Color(72, 217, 118);
		private static readonly Color REDDISH = new Color(227, 59, 59);

		// BANS/PARDONS
		private static readonly Color PINK = new Color(222, 152, 227);
		private static readonly Color YELLOW = new Color(232, 212, 123);

		// VOICE JOINS/LEAVES
		private static readonly Color ORANGEY = new Color(219, 136, 77);
		private static readonly Color AQUAMARINE = new Color(92, 209, 176);

		// MESSAGE DELETION, EDIT, MEMBER CHANGE
		// like my sk themes? c:
		private static readonly Color DIM_SWARM_PINK = new Color(224, 27, 66);
		private static readonly Color ROYAL_JELLY = new Color(122, 82, 196);
		private static readonly Color COBALT = new Color(123, 182, 184);

		private async Task OnVoiceStateChanged(VoiceState oldState, VoiceState state, Snowflake? guildId, Snowflake channelId) {
			if (guildId != ServerID) return;
			if (state.UserID == User.BotUser.ID) return;

			bool wasInVoiceChannel = oldState.IsConnectedToVoice;
			bool isInVoiceChannel = state.IsConnectedToVoice;

			if (wasInVoiceChannel != isInVoiceChannel) {
				Member mbr = await state.User.InServerAsync(Server);
				EmbedBuilder builder = new EmbedBuilder().StampToNow();
				builder.SetAuthor(mbr.FullNickname, null, state.User.AvatarURL);
				if (isInVoiceChannel) {
					builder.Title = "Member connected to voice.";
					builder.Color = AQUAMARINE;
				} else {
					builder.Title = "Member disconnected from voice.";
					builder.Color = ORANGEY;
				}
				builder.AddField("Mention", mbr.Mention);
				await VoiceBehaviorLog.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);
			}
		}

		private async Task OnMessagesBulkDeleted(Snowflake[] messageIds, ChannelBase channel) {
			if (channel is TextChannel textChannel) {
				if (textChannel.Server != Server) return;

				EmbedBuilder builder = new EmbedBuilder().StampToNow();
				builder.Title = "Messages Bulk Deleted";
				builder.Description = "**Messages:**\n";
				builder.Color = DIM_SWARM_PINK;
				foreach (Snowflake messageId in messageIds) {
					string newDesc = builder.Description + messageId.ToString();
					if (newDesc.Length > 2048) {
						builder.AddField("Note", "The amount of messages deleted is too long to contain in this space.");
						break;
					}
					builder.Description = newDesc;
				}
				await MessageBehaviorLog.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);
			}
		}

		private async Task OnMessageDeleted(Snowflake messageId, ChannelBase channel) {
			if (channel is TextChannel textChannel) {
				if (textChannel.Server != Server) return;

				//Directory.CreateDirectory(@".\DEL_ATTACH_CACHE");

				Message msg = textChannel.GetMessageFromCache(messageId);

				EmbedBuilder builder = new EmbedBuilder().StampToNow();
				builder.Title = "Message Deleted";
				builder.AddField("Channel", textChannel.Mention, true);

				if (msg != null) {
					if (msg.Author?.IsSelf ?? false) return;
					builder.AddField("Author", msg.AuthorMember.FullNickname + " (" + msg.Author.Mention + ")", true);
					builder.Description = msg.Content;
				} else {
					builder.AddField("Author", "Unknown: Message was not in cache, cannot download because it's deleted. Content is also missing for this reason.", true);
				}
				builder.AddField("Jump Link", $"https://discord.com/channels/{ServerID}/{channel.ID}/{messageId}", true);
				builder.Color = DIM_SWARM_PINK;

				TimeSpan age = DateTimeOffset.UtcNow - messageId.ToDateTimeOffset();
				builder.AddField("ID Info Dump", $"**ID:** {messageId}\n**Created At:** {messageId.GetDisplayTimestampMS()}\n**Age:** {age.GetTimeDifference()}");
				builder.AddTimeFormatFooter();
				await MessageBehaviorLog.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);
			}
		}

		private async Task OnMessageEdited(Message old, Message message, bool? pinned) {
			if (message?.Author?.IsSelf ?? false) return;
			if (message?.Channel is TextChannel textChannel) {
				if (textChannel.Server != Server) return;

				EmbedBuilder builder = new EmbedBuilder().StampToNow();
				builder.Title = "Message Edited";
				builder.Description = $"Jump Link: {message.JumpLink}";

				builder.AddField("Author", message.Author.FullName + " (" + message.Author.Mention + ")", true);
				builder.AddField("Channel", textChannel.Mention, true);

				string oldContent = old.Content;
				string newContent = message.Content;
				if (oldContent == newContent) return;
				builder.Color = ROYAL_JELLY;
				if (oldContent.Length <= 1024) {
					if (oldContent.Length == 0) oldContent = "\u00AD";
					builder.AddField("Old Content", oldContent, true);
				} else {
					builder.AddField("Old Content (1/2)", oldContent[..1024], true);
					builder.AddField("Old Content (2/2)", oldContent[1024..], true);
				}
				if (newContent.Length <= 1024) {
					if (newContent.Length == 0) return; // This isn't actually possible.
					builder.AddField("New Content", newContent, true);
				} else {
					builder.AddField("New Content (1/2)", newContent[..1024], true);
					builder.AddField("New Content (2/2)", newContent[1024..], true);
				}
				Snowflake id = message.ID;
				TimeSpan age = DateTimeOffset.UtcNow - id.ToDateTimeOffset();
				builder.AddField("ID Info Dump", $"**ID:** {id}\n**Created At:** {id.GetDisplayTimestampMS()}\n**Age:** {age.GetTimeDifference()}");
				builder.AddTimeFormatFooter();
				await MessageBehaviorLog.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);
			}
		}

		
		private async Task OnMemberUpdated(Guild guild, Member oldMember, Member member) {
			if (guild != Server) return;
			if (member.LeftServer) return;
			EmbedBuilder builder = new EmbedBuilder().StampToNow();
			builder.Title = "Member Updated";
			builder.AddField("Member", $"{member.FullName} ({member.Mention})");
			builder.Color = COBALT;
			if (oldMember.Nickname != member.Nickname) {
				if (oldMember.Nickname == null) {
					builder.AddField("Old Nickname", "\u00AD", true);
				} else {
					builder.AddField("Old Nickname", oldMember.Nickname, true);
				}
				if (member.Nickname == null) {
					builder.AddField("New Nickname", "\u00AD", true);
				} else {
					builder.AddField("New Nickname", member.Nickname, true);
				}
			} else {
				return;
			}
			await MembershipLog.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);
		}
		

		private async Task OnMemberLeft(Guild guild, Member member) {
			if (guild != Server) return;
			if (!member.LeftServer) return;
			EmbedBuilder builder = new EmbedBuilder().StampToNow();
			builder.SetAuthor(member.FullNickname, null, member.AvatarURL);
			builder.Title = "Member Left";
			builder.Color = REDDISH;
			builder.AddField("Member", member.FullNickname + " (" + member.Mention + ")");

			Snowflake id = member.ID;
			TimeSpan age = DateTimeOffset.UtcNow - id.ToDateTimeOffset();
			builder.AddField("ID Info Dump", $"**ID:** {id}\n**Created At:** {id.GetDisplayTimestampMS()}\n**Age:** {age.GetTimeDifference()}");
			builder.AddTimeFormatFooter();
			await MembershipLog.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);
		}

		private async Task OnMemberJoined(Guild guild, Member member) {
			if (guild != Server) return;
			if (!guild.Members.Contains(member)) return;
			if (member.LeftServer) return;
			EmbedBuilder builder = new EmbedBuilder().StampToNow();
			builder.SetAuthor(member.FullNickname, null, member.AvatarURL);
			builder.Title = "Member Joined";
			builder.Color = GREENISH;
			builder.AddField("Member", member.FullNickname + " (" + member.Mention + ")");

			Snowflake id = member.ID;
			TimeSpan age = DateTimeOffset.UtcNow - id.ToDateTimeOffset();
			builder.AddField("ID Info Dump", $"**ID:** {id}\n**Created At:** {id.GetDisplayTimestampMS()}\n**Age:** {age.GetTimeDifference()}");
			builder.AddTimeFormatFooter();
			await MembershipLog.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);
		}

		private async Task OnMemberBanned(Guild guild, User user) {
			if (guild != Server) return;
			EmbedBuilder builder = new EmbedBuilder().StampToNow();
			builder.SetAuthor(user.FullName, null, user.AvatarURL);
			builder.Title = "Member Banned";
			builder.Color = YELLOW;
			builder.AddField("Member", user.FullName + " (" + user.Mention + ")");

			Snowflake id = user.ID;
			TimeSpan age = DateTimeOffset.UtcNow - id.ToDateTimeOffset();
			builder.AddField("ID Info Dump", $"**ID:** {id}\n**Created At:** {id.GetDisplayTimestampMS()}\n**Age:** {age.GetTimeDifference()}");
			builder.AddTimeFormatFooter();
			await MembershipLog.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);
		}
		private async Task OnMemberPardoned(Guild guild, User user) {
			if (guild != Server) return;
			EmbedBuilder builder = new EmbedBuilder().StampToNow();
			builder.SetAuthor(user.FullName, null, user.AvatarURL);
			builder.Title = "Member Unbanned";
			builder.Color = PINK;
			builder.AddField("Member", user.FullName + " (" + user.Mention + ")");

			Snowflake id = user.ID;
			TimeSpan age = DateTimeOffset.UtcNow - id.ToDateTimeOffset();
			builder.AddField("ID Info Dump", $"**ID:** {id}\n**Created At:** {id.GetDisplayTimestampMS()}\n**Age:** {age.GetTimeDifference()}");
			builder.AddTimeFormatFooter();
			await MembershipLog.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);
		}

		#endregion

	}
}
