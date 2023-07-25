using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Universal;
using OldOriBot.Data;
using OldOriBot.Data.Persistence;
using OldOriBot.Interaction;
using OldOriBot.Utility.Music.FileRepresentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OldOriBot.Utility.Music {

	/// <summary>
	/// Offers utility methods for controlling the voting subsystems of the music command.
	/// </summary>
	public class MusicVotingHelper {

		/// <summary>
		/// The list of live votes running right now.
		/// </summary>
		public List<Vote> LiveVotes = new List<Vote>();

		/// <summary>
		/// The <see cref="MusicController"/> that this exists in.
		/// </summary>
		public MusicController ParentController { get; }

		/// <summary>
		/// If true, votes to the next track or category are locked and will not apply.
		/// </summary>
		public bool TrackVotingLocked { get; set; } = false;

		public MusicVotingHelper(MusicController parent) {
			ParentController = parent;
			InitializeVoteUpdateCheck();
		}

		/// <summary>
		/// Adds the specified <see cref="XanBotMember"/> to the list of people voting for a song skip. Returns true if they were counted, or false if they were removed due to being already counted.
		/// </summary>
		/// <param name="executingMember">The member who is voting.</param>
		/// <returns></returns>
		public bool AddVoteToMusicSkip(Member executingMember) {
			try {
				if (executingMember.CurrentVoiceChannel != ParentController.EffectiveMusicChannel) return false;
			} catch (Exception) {
				return false;
			}

			Vote skipVote = null;
			foreach (Vote vote in LiveVotes) {
				if (vote.TargetType == VoteType.SkipSong) {
					skipVote = vote;
					break;
				}
			}
			if (skipVote == null) {
				skipVote = new Vote(this) {
					TargetType = VoteType.SkipSong
				};
				LiveVotes.Add(skipVote);
			}

			bool retVal = skipVote.CountOrRemoveVote(executingMember);
			if (skipVote.VotePassed) skipVote.PerformSpecificAction().Wait();
			return retVal;
		}

		/// <summary>
		/// Adds the specified <see cref="XanBotMember"/> to the list of people voting for a music restart. Returns true if they were counted, or false if they were removed due to being already counted.
		/// </summary>
		/// <param name="executingMember">The member who is voting.</param>
		/// <returns></returns>
		public bool AddVoteToMusicRestart(Member executingMember) {
			try {
				if (executingMember.CurrentVoiceChannel != ParentController.EffectiveMusicChannel) return false;
			} catch (Exception) {
				return false;
			}

			Vote resetVote = null;
			foreach (Vote vote in LiveVotes) {
				if (vote.TargetType == VoteType.RestartMusic) {
					resetVote = vote;
					break;
				}
			}
			if (resetVote == null) {
				resetVote = new Vote(this) {
					TargetType = VoteType.RestartMusic
				};
				LiveVotes.Add(resetVote);
			}

			bool retVal = resetVote.CountOrRemoveVote(executingMember);
			if (resetVote.VotePassed) resetVote.PerformSpecificAction().Wait();
			return retVal;
		}

		/// <summary>
		/// Adds a vote that states a user wants a given next track.
		/// </summary>
		/// <param name="executingMember"></param>
		/// <returns></returns>
		public bool AddVoteToNextTrack(Member executingMember, MusicFile targetTrack) {
			if (TrackVotingLocked) return false;
			try {
				if (executingMember.CurrentVoiceChannel != ParentController.EffectiveMusicChannel) return false;
			} catch (Exception) {
				return false;
			}

			Vote voteNextVote = null;
			foreach (Vote vote in LiveVotes) {
				if (vote.TargetType == VoteType.VoteNextTrack && vote.TargetTrack == targetTrack) {
					voteNextVote = vote;
					break;
				}
			}
			if (voteNextVote == null) {
				voteNextVote = new Vote(this) {
					TargetType = VoteType.VoteNextTrack,
					TargetTrack = targetTrack
				};
				LiveVotes.Add(voteNextVote);
			}

			// One important thing here is that we REMOVE other votes for tracks and categories first.
			foreach (Vote vote in LiveVotes) {
				if ((vote.TargetType == VoteType.VoteNextTrack || vote.TargetType == VoteType.VoteNextCategory) && vote != voteNextVote) {
					if (vote.Voters.Contains(executingMember)) {
						vote.CountOrRemoveVote(executingMember); // Will always remove.
					}
				}
			}


			bool retVal = voteNextVote.CountOrRemoveVote(executingMember);
			if (voteNextVote.VotePassed) voteNextVote.PerformSpecificAction().Wait();
			return retVal;
		}

		/// <summary>
		/// Adds a vote that states a user wants a given next category.
		/// </summary>
		/// <param name="executingMember"></param>
		/// <returns></returns>
		public bool AddVoteToNextCategory(Member executingMember, MusicDirectory targetCategory) {
			if (TrackVotingLocked) return false;
			try {
				if (executingMember.CurrentVoiceChannel != ParentController.EffectiveMusicChannel) return false;
			} catch (Exception) {
				return false;
			}

			Vote voteNextVote = null;
			foreach (Vote vote in LiveVotes) {
				if (vote.TargetType == VoteType.VoteNextCategory && vote.TargetCategory == targetCategory) {
					voteNextVote = vote;
					break;
				}
			}
			if (voteNextVote == null) {
				voteNextVote = new Vote(this) {
					TargetType = VoteType.VoteNextCategory,
					TargetCategory = targetCategory
				};
				LiveVotes.Add(voteNextVote);
			}

			// One important thing here is that we REMOVE other votes for tracks and categories first.
			foreach (Vote vote in LiveVotes) {
				if ((vote.TargetType == VoteType.VoteNextTrack || vote.TargetType == VoteType.VoteNextCategory) && vote != voteNextVote) {
					if (vote.Voters.Contains(executingMember)) {
						vote.CountOrRemoveVote(executingMember); // Will always remove.
					}
				}
			}

			bool retVal = voteNextVote.CountOrRemoveVote(executingMember);
			if (voteNextVote.VotePassed) voteNextVote.PerformSpecificAction().Wait();
			return retVal;
		}

		/// <summary>
		/// Resets all running votes and sets locked = false
		/// </summary>
		/// <returns></returns>
		public void ClearVotes() {
			LiveVotes.Clear();
			TrackVotingLocked = false;
		}

		/// <summary>
		/// This system updates music votes so that whenever a member joins or leaves, the vote is updated.
		/// </summary>
		public void InitializeVoteUpdateCheck() {
			DiscordClient.Current!.Events.VoiceStateEvents.OnVoiceStateChanged += OnVoiceStateChanged;
		}

		private Task OnVoiceStateChanged(VoiceState old, VoiceState state, Snowflake? guildId, Snowflake channelId) {
			if (channelId != ParentController.EffectiveMusicChannel.ID) return Task.CompletedTask;
			if (old.IsConnectedToVoice != state.IsConnectedToVoice) {
				foreach (Vote vote in LiveVotes) {
					if (vote.VotePassed) vote.PerformSpecificAction().Wait();
				}
			}
			return Task.CompletedTask;
		}
	}

	public class Vote : IEmbeddable, IEquatable<Vote> {

		/// <summary>
		/// A list of the people who have voted.
		/// </summary>
		public List<Member> Voters { get; } = new List<Member>();

		/// <summary>
		/// The <see cref="MusicVotingHelper"/> that instantiated this <see cref="Vote"/>
		/// </summary>
		public MusicVotingHelper Parent { get; }

		/// <summary>
		/// The amount of people who have voted.
		/// </summary>
		public int CurrentVoterCount => Voters.Count;

		/// <summary>
		/// The total amount of people who can vote.
		/// </summary>
		public int TotalPossibleVoters => Parent.ParentController.ListenerCount;

		/// <summary>
		/// The required percentage of members to pass this vote.
		/// </summary>
		public float RequiredPercentage { get; set; } = 2/3f;

		/// <summary>
		/// If this <see cref="Vote"/> is of the type <see cref="VoteType.VoteNextTrack"/>, this is the <see cref="MusicFile"/> that should be played. This is also set whenever <see cref="TargetCategory"/> is set.
		/// </summary>
		public MusicFile TargetTrack { get; set; }

		/// <summary>
		/// If this <see cref="Vote"/> is of the type <see cref="VoteType.VoteNextCategory"/>, this is the <see cref="MusicDirectory"/> that should have a random track selected from it.
		/// </summary>
		public MusicDirectory TargetCategory {
			get => _TargetCategory;
			set {
				_TargetCategory = value;
				TargetTrack = value?.GetRandomSong();
			}
		}
		private MusicDirectory _TargetCategory;

		/// <summary>
		/// The amount of votes required to pass this vote.
		/// </summary>
		public int RequiredVotesToPass {
			get {
				return (int)Math.Round(TotalPossibleVoters * RequiredPercentage);
				//return 69;
			}
		}

		/// <summary>
		/// The type of vote that this is.
		/// </summary>
		public VoteType TargetType { get; set; }

		/// <summary>
		/// True if this vote has passed and whatever was being voted on should be executed.
		/// </summary>
		public bool VotePassed {
			get {
				if (TotalPossibleVoters == 0) return true;

				// Catch case: Someone left. Don't count their vote.
				foreach (Member voter in Voters) {
					if (!Parent.ParentController.EffectiveMusicChannel.ConnectedMembers.Contains(voter)) {
						Voters.Remove(voter);
					}
				}

				//return ((float)CurrentVoterCount / TotalPossibleVoters) >= RequiredPercentage;
				return CurrentVoterCount >= RequiredVotesToPass;
			}
		}

		/// <summary>
		/// Attempts to count the vote of the input <paramref name="voter"/>. Returns true and adds them to the voter array if they were not in it already, returns false if they already voted and were removed from the voter array.
		/// </summary>
		/// <param name="voter">The member who is voting.</param>
		/// <returns></returns>
		public bool CountOrRemoveVote(Member voter) {
			if (Voters.Contains(voter)) {
				Voters.Remove(voter);
				return false;
			}
			Voters.Add(voter);
			return true;
		}

		/// <summary>
		/// Hardcoded feature for the music system. The corresponding action (based on <see cref="TargetType"/>) will be taken on the bot system and this vote will be marked as inactive.<para/>
		/// THIS DOES NOT CHECK IF <see cref="VotePassed"/> IS TRUE.
		/// </summary>
		/// <returns></returns>
		public async Task PerformSpecificAction() {
			if (MusicController.Configuration.TryGetType("IsVotingEnabled", true)) {
				Parent.LiveVotes.Remove(this);
				if (TargetType == VoteType.SkipSong) {
					await Parent.ParentController.Skip();
					Parent.TrackVotingLocked = false;
				} else if (TargetType == VoteType.RestartMusic) {
					await Parent.ParentController.Reinitialize();
					Parent.TrackVotingLocked = false;
				} else if (TargetType == VoteType.VoteNextTrack || TargetType == VoteType.VoteNextCategory) {
					// File is randomly selected whenever the setter for TargetCategory runs via populating TargetTrack with a random song from the category.
					Parent.TrackVotingLocked = true;
					Parent.ParentController.ForceNext = TargetTrack.File;

					// Now remove all votes for the next category.
					List<Vote> votes = Parent.LiveVotes.ToList(); // dupe it
					foreach (Vote vote in votes) {
						if (vote.TargetType == VoteType.VoteNextTrack || vote.TargetType == VoteType.VoteNextCategory) {
							if (Parent.LiveVotes.Contains(vote)) Parent.LiveVotes.Remove(vote);
						}
					}
				}
			} else {
				Parent.LiveVotes.Remove(this);
			}
		}

		public Vote(MusicVotingHelper helper) {
			Parent = helper;
		}
		

		public static bool operator ==(Vote left, Vote right) {
			if (left is Vote) return left.Equals(right);
			if (right is Vote) return right.Equals(left);
			return ReferenceEquals(left, right);
		}

		public static bool operator !=(Vote left, Vote right) => !(left == right);

		public bool Equals(Vote other) {
			if (other == null) return false;
			if (ReferenceEquals(other, this)) return true;
			if (TargetType == other.TargetType) {
				if (TargetType == VoteType.SkipSong) {
					return true; // This is a singleton vote.
				} else if (TargetType == VoteType.RestartMusic) {
					return true; // This is a singleton vote.
				} else if (TargetType == VoteType.VoteNextCategory) {
					return TargetCategory == other.TargetCategory;
				} else if (TargetType == VoteType.VoteNextTrack) {
					return TargetTrack == other.TargetTrack;
				}
			}
			return false;
		}

		public override bool Equals(object obj) => obj is Vote vote ? Equals(vote) : ReferenceEquals(obj, this);

		public override int GetHashCode() => HashCode.Combine(TargetType, TargetCategory, TargetTrack);

		public Vote() { }

		public Embed ToEmbed() {
			EmbedBuilder builder = new EmbedBuilder {
				Title = "Vote Info Dump",
				Description = "Information about a current ongoing vote type."
			};
			builder.AddField("Vote Type", TargetType.ToString());
			if (TargetType == VoteType.VoteNextTrack) {
				builder.AddField("Target Track", TargetTrack.Metadata.Title);
			} else if (TargetType == VoteType.VoteNextCategory) {
				builder.AddField("Target Category", TargetCategory.Name);
			}
			builder.AddField("Number Of Total Possible Votes", TotalPossibleVoters.ToString());
			builder.AddField("Number Of Current Votes", CurrentVoterCount.ToString());
			builder.AddField("Number Of Votes Necessary To Pass", RequiredVotesToPass.ToString());
			builder.AddField("Passed", VotePassed.ToString());
			return builder.Build();
		}
	}

	public enum VoteType {
		SkipSong,
		RestartMusic,
		VoteNextTrack,
		VoteNextCategory
	}
}
