using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using EtiLogger.Data.Structs;
using OldOriBot.CoreImplementation.Commands;
using OldOriBot.CoreImplementation.Handlers;
using OldOriBot.Data.Commands;
using OldOriBot.Data.Persistence;
using OldOriBot.Interaction;
using OldOriBot.Utility;
using OldOriBot.Utility.Extensions;

namespace OldOriBot.CoreImplementation {
	public class BotContextOriTheGame : BotContext {
		public override string Name { get; } = "Ori the Game Bot Context";
		public override string DataPersistenceName { get; } = "ctxOri";
		protected override Snowflake ServerID { get; } = 577548441878790146;
		protected override Snowflake EventLogID { get; } = 622119204992057354;
		protected override Snowflake MembershipLogID { get; } = 673409492460896296;
		protected override Snowflake MessageBehaviorLogID { get; } = 798066373485658162;
		protected override Snowflake VoiceBehaviorLogID { get; } = 798066188139102258;
		protected override Snowflake ModerationLogID { get; } = 629740939992104970;
		public override Snowflake? BotChannelID { get; } = 621800841871097866;
		public override bool OnlyAllowCommandsInBotChannel { get; } = true;
		public override Command[] Commands { get; set; } = new Command[0];
		public override PassiveHandler[] Handlers { get; set; } = new PassiveHandler[0];
		public override bool DownloadsAllMembers { get; } = true;

		/// <summary>
		/// Log messages that get edited.
		/// </summary>
		public bool LogMessageEdits => Storage.TryGetType("LogMessageEdits", true);

		/// <summary>
		/// Log messages that get deleted.
		/// </summary>
		public bool LogMessageDeletions => Storage.TryGetType("LogMessageDeletions", true);

		/// <summary>
		/// Log activity from members joining or leaving voice channels.
		/// </summary>
		public bool LogMemberVoiceActivity => Storage.TryGetType("LogMemberVoiceActivity", true);

		/// <summary>
		/// Log members joining and leaving.
		/// </summary>
		public bool LogMembership => Storage.TryGetType("LogMembership", true);

		/// <summary>
		/// Log members changing their nickname.
		/// </summary>
		public bool LogNicknameChanges => Storage.TryGetType("LogNicknameChanges", true);

		/// <summary>
		/// Whether or not to log bans.
		/// </summary>
		public bool LogBans => Storage.TryGetType("LogBans", true);

		/// <summary>
		/// Whether or not to automatically download attachments on messages.
		/// </summary>
		public bool EagerlyTrackAttachments => Storage.TryGetType("EagerlyTrackAttachments", true);

		/// <summary>
		/// Whether or not a lockdown is enabled right now.
		/// </summary>
		public double LockdownMinAge => Storage.TryGetType("LockdownMinAgeDays", 3);


		public BotContextOriTheGame() : base() {
			DiscordClient.Current!.DeferNonGuildCreateEvents = true;
		}

		public ManagedRole SpiritsRole;
		public ManagedRole PCMainRole;
		public ManagedRole XBMainRole;
		public ManagedRole SWMainRole;
		public ManagedRole PCRole;
		public ManagedRole XBRole;
		public ManagedRole SWRole;
		public ManagedRole ImagesRole;
		public MemberMuteUtility MuteSystem;

		public override async Task AfterContextInitialization() {

			DirectoryInfo attachmentCache = Directory.CreateDirectory(@"C:\_DiscordAttachmentCache");
			foreach (FileInfo file in attachmentCache.GetFiles()) {
				Snowflake id = file.GetFileID();
				if ((DateTimeOffset.UtcNow - id.ToDateTimeOffset()).TotalDays > 7) {
					file.Delete();
				}
			}

			await Server.RedownloadAllRolesAsync();
			await Server.ForcefullyAcquireChannelsAsync();
			MuteSystem = MemberMuteUtility.GetOrCreate(this);

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
				// new CommandGreg(this),
				new CommandMusic(this),
				new CommandNowPlaying(this),
				new CommandBadge(this),
				new CommandAllowMassPing(this),
				new CommandPurge(this),
				new CommandLog(this),
				new CommandHug(this),
				new CommandRoll(this),
				// new CommandTestThreads(this)
				new CommandMakeTimestamp(this),
				new CommandBan(this),
			};
			Handlers = new PassiveHandler[] {
				// Below: intercepts
				new HandlerProfanityFilter(this),
				new HandlerGenericFilter(this),
				new HandlerAntiSpamSystem(this),
				new HandlerAntiCopypasta(this),
				
				// Below: has no interceptions
				// new HandlerModLogAssistant(this),
				// new HandlerUnavailableModHelper(this),
				new HandlerGrantImageRole(this),
				new HandlerProfileExperienceReward(this),
				new HandlerArtPinSystem(Server, this), // this DOES intercept
				new HandlerSteamSystem(this),
				new HandlerPassiveResponseSystem(this),
				new HandlerSecurityFilter(this),
				new HandlerNicknameMarshaller(this),
				new HandlerRandomModSelector(this)
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
				["completedboth"] = new ManagedRole(
					Server,
					"Completed Both Ori Games"
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
			SpiritsRole = new ManagedRole(Server, "Spirits");
			PCMainRole = new ManagedRole(Server, "PC Mains", new Color(0x7799C4));
			XBMainRole = new ManagedRole(Server, "Xbox Mains", new Color(0x107C10));
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

			var guildContainer = DiscordClient.Current!.Events.GuildEvents;
			var msgContainer = DiscordClient.Current!.Events.MessageEvents;
			var voiceContainer = DiscordClient.Current!.Events.VoiceStateEvents;
			var mbrContainer = DiscordClient.Current!.Events.MemberEvents;
			var banContainer = DiscordClient.Current!.Events.BanEvents;

			guildContainer.OnThreadMembersUpdated += OnThreadMembersUpdated;
			msgContainer.OnMessageCreated += OnMessageCreated;
			msgContainer.OnMessageDeleted += OnMessageDeleted;
			msgContainer.OnMessageEdited += OnMessageEdited;
			msgContainer.OnMessagesBulkDeleted += OnMessagesBulkDeleted;
			voiceContainer.OnVoiceStateChanged += OnVoiceStateChanged;
			mbrContainer.OnGuildMemberAdded += OnMemberJoined;
			mbrContainer.OnGuildMemberAdded += SendMemberWelcomeMessage;
			mbrContainer.OnGuildMemberRemoved += OnMemberLeft;
			mbrContainer.OnGuildMemberUpdated += OnMemberUpdated;
			banContainer.OnMemberBanned += OnMemberBanned;
			banContainer.OnMemberUnbanned += OnMemberPardoned;

			DiscordClient.Current!.Events.ReactionEvents.OnReactionAdded += OnReactionAdded;
			DiscordClient.Current!.Events.ReactionEvents.OnReactionRemoved += OnReactionRemoved;

			ContextLogger.WriteLine("Disabling event deferring.", EtiLogger.Logging.LogLevel.Info);
			DiscordClient.Current!.DeferNonGuildCreateEvents = false;
			CommandMarshaller.Ready = true;
		}

		private async Task SendMemberWelcomeMessage(Guild guild, Member member) {
			string welcomeMessageFmt;
			if (Directory.Exists("V:\\")) {
				welcomeMessageFmt = File.ReadAllText(@"V:\EtiBotCore\UNIVERSAL_WELCOME_MSG.TXT");
			} else {
				welcomeMessageFmt = File.ReadAllText(@"C:\EtiBotCore\UNIVERSAL_WELCOME_MSG.TXT");
			}
			string welcomeMessage = string.Format(welcomeMessageFmt, member.Mention, "<@!114163433980559366>");
			await member.TrySendDMAsync(welcomeMessage);
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
			if (message.Channel.ID != 622269852416999453) return;
			if (message.ID != 685307565667647544) return;
			if (user.IsABot || user.IsSelf) return;
			Member member = await user.InServerAsync(Server);
			Role mainRole = GetMemberMainRole(member);
			if (emoji.ID == 671886904773443584 || emoji.Name == "ori") {
				ContextLogger.WriteLine($"Received event of Ori reaction being added from [{user.ID}] ({user.FullName}).");
				/*
				if (member.Roles.Contains(MuteSystem.Muted)) {
					ContextLogger.WriteLine("...But this person has the muted role, so it has been dropped.");
					return;
				}
				if (MuteSystem.IsMutedInRegistry(member.ID)) {
					ContextLogger.WriteLine("...But this person is muted in the registry, so it has been dropped.");
					return;
				}
				*/
				if (MuteSystem.IsMuted(member)) {
					ContextLogger.WriteLine("...But this person is muted (either by role or by registry), so it has been dropped.");
					return;
				}

				// ori
				if (!member.Roles.Contains(SpiritsRole.Role)) {
					member.BeginChanges(true);
					member.Roles.Add(SpiritsRole.Role);
					var response = await member.ApplyChanges("Clicked on Ori, granted Spirits role.");
					if (response.IsSuccessStatusCode) {
						ContextLogger.WriteLine("...and gave them the role!");
					} else {
						ContextLogger.WriteWarning($"Failed to give spirits role to [{user.ID}] ({user.FullName})! [HTTP {(int)response.StatusCode}] {response.StatusCode} {response.RequestMessage}");
					}
				} else {
					ContextLogger.WriteLine("...But this person already has the spirit role, so it has been dropped.");
				}
			} else if (emoji.ID == 685306067957055549 || emoji.Name == "Windows") {
				if (member.Roles.Contains(MuteSystem.Muted)) return;
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
				if (member.Roles.Contains(MuteSystem.Muted)) return;
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
				if (member.Roles.Contains(MuteSystem.Muted)) return;
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
			if (message.Channel.ID != 622269852416999453) return;
			if (message.ID != 685307565667647544) return;
			if (user.IsABot || user.IsSelf) return;
			Member member = await user.InServerAsync(Server);
			Role mainRole = GetMemberMainRole(member);
			if (emoji.ID == 685306067957055549 || emoji.Name == "Windows") {
				if (member.Roles.Contains(MuteSystem.Muted)) return;
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
				if (member.Roles.Contains(MuteSystem.Muted)) return;
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
				if (member.Roles.Contains(MuteSystem.Muted)) return;
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
		private static readonly Color SILVER = new Color(200, 200, 200);

		// MESSAGE DELETION, EDIT, MEMBER CHANGE
		// like my sk themes? c:
		private static readonly Color DIM_SWARM_PINK = new Color(224, 27, 66);
		private static readonly Color ROYAL_JELLY = new Color(122, 82, 196);
		private static readonly Color COBALT = new Color(123, 182, 184);

		private async Task OnVoiceStateChanged(VoiceState oldState, VoiceState state, Snowflake? guildId, Snowflake channelId) {
			if (guildId != ServerID) return;
			if (!LogMemberVoiceActivity) return;
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
					builder.AddField("Channel", ((VoiceChannel)state.Channel).Name);
				} else {
					builder.Title = "Member disconnected from voice.";
					builder.Color = ORANGEY;
					builder.AddField("Channel", ((VoiceChannel)oldState.Channel).Name);
				}
				
				builder.AddField("Mention", mbr.Mention);
				await VoiceBehaviorLog.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);
			} else if (wasInVoiceChannel && isInVoiceChannel) {
				if (oldState.Channel == state.Channel) return; // No actual change in terms of channels.

				Member mbr = await state.User.InServerAsync(Server);
				EmbedBuilder builder = new EmbedBuilder().StampToNow();
				builder.SetAuthor(mbr.FullNickname, null, state.User.AvatarURL);
				builder.Title = "Member changed voice channels.";
				builder.Color = SILVER;
				builder.AddField("Old Channel", ((VoiceChannel)oldState.Channel).Name, true);
				builder.AddField("New Channel", ((VoiceChannel)state.Channel).Name, true);
				builder.AddField("Mention", mbr.Mention);
				await VoiceBehaviorLog.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);
			}
		}

		private async Task OnMessagesBulkDeleted(Snowflake[] messageIds, ChannelBase channel) {
			if (channel is TextChannel textChannel) {
				if (textChannel.Server != Server) return;
				if (!LogMessageDeletions) return;

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
				if (!LogMessageDeletions) return;

				//Directory.CreateDirectory(@".\DEL_ATTACH_CACHE");

				Message msg = textChannel.GetMessageFromCache(messageId);
				FileInfo[] attachments = Array.Empty<FileInfo>();

				EmbedBuilder builder = new EmbedBuilder().StampToNow();
				builder.Title = "Message Deleted";
				builder.AddField("Channel", textChannel.Mention, true);

				if (msg != null) {
					if (msg.Author?.IsSelf ?? false) return;
					builder.AddField("Author", msg.AuthorMember.FullNickname + " (" + msg.Author.Mention + ")", true);
					attachments = new DirectoryInfo(@"C:\_DiscordAttachmentCache").FindFilesByID(msg.ID);
					builder.Description = msg.Content + "\u00AD";
				} else {
					builder.AddField("Author", "Unknown: Message was not in cache, cannot download because it's deleted. Content is also missing for this reason.", true);
				}
				builder.AddField("Jump Link", $"https://discord.com/channels/{ServerID}/{channel.ID}/{messageId}", true);
				builder.Color = DIM_SWARM_PINK;

				TimeSpan age = DateTimeOffset.UtcNow - messageId.ToDateTimeOffset();
				builder.AddField("ID Info Dump", $"**ID:** {messageId}\n**Created At:** {messageId.GetDisplayTimestampMS()}\n**Age:** {age.GetTimeDifference()}");
				builder.AddTimeFormatFooter();

				string messageContent = null;
				await MessageBehaviorLog.SendMessageAsync(messageContent, builder.Build(), AllowedMentions.AllowNothing, attachments);

				if (attachments.Length > 0 && messageContent == null) {
					await Task.Delay(5000);
					foreach (FileInfo attachment in attachments) {
						attachment.Delete();
					}
				}
			}
		}

		private async Task OnMessageEdited(Message old, Message message, bool? pinned) {
			if (message?.Author?.IsSelf ?? false) return;
			if (message?.Channel is TextChannel textChannel) {
				if (textChannel.Server != Server) return;
				if (!LogMessageEdits) return;

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
			if (!LogNicknameChanges) return;
			if (member.LeftServer) return;
			EmbedBuilder builder = new EmbedBuilder().StampToNow();
			builder.Title = "Member Updated";
			builder.AddField("Member", $"{member.FullName} ({member.Mention})");
			builder.Color = COBALT;
			builder.Description = "Note that this change may not show when clicking on the user due to caching problems with the Discord client.";
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
			if (!LogMembership) return;
			if (!member.LeftServer) return;
			EmbedBuilder builder = new EmbedBuilder().StampToNow();
			builder.SetAuthor(member.FullNickname, null, member.AvatarURL);
			builder.Title = "Member Left";
			builder.Color = REDDISH;
			builder.AddField("Member", member.FullNickname + " (" + member.Mention + ")");

			Snowflake id = member.ID;
			TimeSpan age = member.AccountAge;
			builder.AddField("ID Info Dump", $"**ID:** {id}\n**Created At:** {id.GetDisplayTimestampMS()}\n**Age:** {age.GetTimeDifference()}");
			builder.AddTimeFormatFooter();
			await MembershipLog.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);
		}

		private async Task OnMemberJoined(Guild guild, Member member) {
			if (guild != Server) return;


			bool kicked = false;
			TimeSpan age = member.AccountAge;
			if (age.TotalDays < LockdownMinAge) {
				Message msg = await member.TrySendDMAsync($"Hey {member.Mention}! I'm sorry that I have to do this, but due to a recent influx of bots, we've had to implement countermeasures against them. One of the easiest ways to do this is to have a minimum account age for the server. With that said... Your account age is less than this limit, and consequently, you will be kicked from the server immediately after receiving this message. We do not like having to do this especially for genuine new additions to this wonderful community, but we are left with no other choice due to the damages caused by these bots. You are welcome when your account is {LockdownMinAge} days old. Your account age right now is: " + age.GetTimeDifference() + "\n\nPlease note that you are *not* banned! You are free to return at any time past this waiting period. This restriction will be lifted soon.");
				if (msg != null) {
					await member.KickAsync($"Lockdown system is engaged and this member is younger than {LockdownMinAge} days.");
					kicked = true;
				}
			}

			if (kicked) {
				if (LogMembership) {
					EmbedBuilder kickBuilder = new EmbedBuilder().StampToNow();
					kickBuilder.SetAuthor(member.FullNickname, null, member.AvatarURL);
					kickBuilder.Title = "Member Joined, But Kicked For Security";
					kickBuilder.Description = $"The Account Age Limiter is enabled and set to {LockdownMinAge} days. Since this account is younger, they have been kicked.";
					kickBuilder.Color = YELLOW;
					kickBuilder.AddField("Member", member.FullNickname + " (" + member.Mention + ")");

					Snowflake memID = member.ID;
					kickBuilder.AddField("ID Info Dump", $"**ID:** {memID}\n**Created At:** {memID.GetDisplayTimestampMS()}\n**Age:** {age.GetTimeDifference()}");
					kickBuilder.AddTimeFormatFooter();
					await MembershipLog.SendMessageAsync(null, kickBuilder.Build(), AllowedMentions.AllowNothing);
				}
				return;
			}


			if (!LogMembership) return;
			if (!guild.Members.Contains(member)) return;
			if (member.LeftServer) return;
			EmbedBuilder builder = new EmbedBuilder().StampToNow();
			builder.SetAuthor(member.FullNickname, null, member.AvatarURL);
			builder.Title = "Member Joined";
			builder.Color = GREENISH;
			builder.AddField("Member", member.FullNickname + " (" + member.Mention + ")");

			if (age.TotalDays < LockdownMinAge) {
				builder.Description = "**NOTE:** This member would have been kicked, but they had DMs off. This is atypical for scambots, and rather than kicking someone for seemingly 'no reason' (since they could not be told), they have been allowed to stay. Please observe their behavior.";
			}

			Snowflake id = member.ID;
			builder.AddField("ID Info Dump", $"**ID:** {id}\n**Created At:** {id.GetDisplayTimestampMS()}\n**Age:** {age.GetTimeDifference()}");
			builder.AddTimeFormatFooter();
			await MembershipLog.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);

			if (MuteSystem.IsMutedInRegistry(member.ID)) {
				member.BeginChanges(true);
				member.Roles.Add(MuteSystem.Muted);
				await member.ApplyChanges("This member is muted, and rejoined the server. Adding the role for consistency's sake.");
			}
		}

		internal List<Snowflake> IgnoreBannedIDs = new List<Snowflake>();
		private async Task OnMemberBanned(Guild guild, User user) {
			if (guild != Server) return;
			if (!LogBans) return;
			EmbedBuilder builder = new EmbedBuilder().StampToNow();
			builder.SetAuthor(user.FullName, null, user.AvatarURL);
			builder.Title = "Member Banned";
			builder.Color = YELLOW;
			builder.AddField("Member", user.FullName + " (" + user.Mention + ")");

			Snowflake id = user.ID;
			TimeSpan age = DateTimeOffset.UtcNow - id.ToDateTimeOffset();
			builder.AddField("ID Info Dump", $"**ID:** {id}\n**Created At:** {id.GetDisplayTimestampMS()}\n**Age:** {age.GetTimeDifference()}");
			builder.AddTimeFormatFooter();
			await ModerationLog.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);

			// Start a background task to report on this ban.
			
			_ = Task.Run(async () => {
				await Task.Delay(5000);
				if (!IgnoreBannedIDs.Contains(user.ID)) {
					var auditLog = await guild.DownloadAuditLog(EtiBotCore.Payloads.Data.AuditLogActionType.MEMBER_BAN_ADD);
					var banEntry = auditLog.Entries.FirstOrDefault(entry => {
						if (entry.ActionType == EtiBotCore.Payloads.Data.AuditLogActionType.MEMBER_BAN_ADD) {
							if (entry.TargetID == user.ID) {
								return true;
							}
						}
						return false;
					});

					if (banEntry != null) {
						InfractionLogProvider provider = InfractionLogProvider.GetProvider(this);
						provider.AppendBan(banEntry.UserID, banEntry.TargetID, banEntry.Reason ?? "<<No Reason Specified>>", banEntry.ID.ToDateTimeOffset());
					}
				}
			});
			
		}
		private async Task OnMemberPardoned(Guild guild, User user) {
			if (guild != Server) return;
			if (!LogBans) return;
			EmbedBuilder builder = new EmbedBuilder().StampToNow();
			builder.SetAuthor(user.FullName, null, user.AvatarURL);
			builder.Title = "Member Unbanned";
			builder.Color = PINK;
			builder.AddField("Member", user.FullName + " (" + user.Mention + ")");

			Snowflake id = user.ID;
			TimeSpan age = DateTimeOffset.UtcNow - id.ToDateTimeOffset();
			builder.AddField("ID Info Dump", $"**ID:** {id}\n**Created At:** {id.GetDisplayTimestampMS()}\n**Age:** {age.GetTimeDifference()}");
			builder.AddTimeFormatFooter();
			await ModerationLog.SendMessageAsync(null, builder.Build(), AllowedMentions.AllowNothing);

			
			_ = Task.Run(async () => {
				await Task.Delay(5000);
				var auditLog = await guild.DownloadAuditLog(EtiBotCore.Payloads.Data.AuditLogActionType.MEMBER_BAN_REMOVE);
				var banEntry = auditLog.Entries.FirstOrDefault(entry => {
					if (entry.ActionType == EtiBotCore.Payloads.Data.AuditLogActionType.MEMBER_BAN_REMOVE) {
						if (entry.TargetID == user.ID) {
							return true;
						}
					}
					return false;
				});

				if (banEntry != null) {
					InfractionLogProvider provider = InfractionLogProvider.GetProvider(this);
					provider.AppendPardon(banEntry.UserID, banEntry.TargetID, banEntry.Reason ?? "<<No Reason Specified>>", banEntry.ID.ToDateTimeOffset());
				}
			});
		}

		private async Task OnMessageCreated(Message message, bool? pinned) {
			if (message.Author.IsSelf) return;
			if (message.Channel is TextChannel textChannel) {
				if (textChannel.ID == 872548812525297695 && !message.AuthorMember.HasPermission(EtiBotCore.Payloads.Data.Permissions.Administrator)) {
					// Tickets channel
					await Task.Delay(100);
					await message.DeleteAsync("Unauthorized message.");
					return;
				}

				if (textChannel.Server != Server) return;
				if (!EagerlyTrackAttachments) return;
				if (message.Attachments.Length == 0) return;
				int currentSizeUsed = 0;
				foreach (Attachment attachment in message.Attachments) {
					int nextSize = currentSizeUsed + attachment.Size;
					if (nextSize > message.Server!.FileSizeLimit) {
						break;
					}
					string fileName = message.ID + "-" + attachment.FileName;
					_ = attachment.SaveToFileAsync(@$"C:\_DiscordAttachmentCache\{fileName}");
				}
			}
		}

		private async Task OnThreadMembersUpdated(Guild server, Thread thread, Member[] added, Snowflake[] removed) {
			if (server != Server) return;
			if (thread.ParentID != 872548812525297695) return;

			foreach (Member mbr in added) {
				if (thread.Name.StartsWith(mbr.ID.ToString())) continue; // This thread is theirs
				if (!mbr.Roles.Contains(603306540438388756)) {
					// Not a moderator
					await thread.TryRemoveMemberFromThread(mbr);
				}
			}
		}


		#endregion
	}
}
