using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Universal;
using OldOriBot.Data;
using OldOriBot.Data.Persistence;
using OldOriBot.Utility.Extensions;

namespace OldOriBot.UserProfiles {
	public class BadgeRegistry {

		/// <summary>
		/// A badge awarded to users who post at least one work in the art gallery.
		/// </summary>
		public static readonly Badge ART_POSTED_BADGE = new Badge("Creative", "I've made a post in the art gallery!", "Paintbrush Wrangler", ":paintbrush:", 1, 250);

		/// <summary>
		/// A badge awarded to users who get pinned in the art gallery.
		/// </summary>
		public static readonly Badge ART_PINNED_BADGE = new Badge("Pincushion", "My work has been pinned in the art gallery!", "Person That Can Legally Say [*\"There, NOW it's art!\"*](https://youtu.be/kwXI3Lh1wwI?t=3)", ":pushpin:", 0, 3000);

		private static List<Badge> BadgeCache = null;

		/// <summary>
		/// Every badge offered by the system.
		/// </summary>
		public static List<Badge> AllBadges {
			get {
				if (BadgeCache == null) {
					BadgeCache = new List<Badge>() {
						// 1000
						new Badge("Lab Rat", "I've voluntarily helped test features or tried to solve bugs for the bot. Probably by trying to break it. Which is a good thing!", "Time-Waster", ":rat:", 1, 1000),
						// 1000
						new Badge("(Really Handy) Little Vermin", "(Don't worry, the title's just a tease!)\nI've managed to find a problem in the bot completely by accident while <@114163433980559366> was watching, causing him to release a patch in response!", "clumsy-except-messing-up-ended-up-helping-everyone-in-the-end Badge", ":bug:", 1, 1000),
						// 1000
						new Badge("Bug Spotter", "I've reported a bug in the bot to <@114163433980559366>!", "Helping Hand", ":beetle:", 1, 1000),
						// 400
						new Badge("Benefactor", "L-O-D-S OF E-M-O-N-E What's that spell? LOADS OF MONEY! Probly.\n\nAnd a lot of it! Users with this badge have proven their willingness to give to others.", "Certified Cash-Money Madlad", ":money_with_wings:", 1, 400),
						// 0
						new Badge("Staff Member", "I'm a staff member on this server.", "Anti-Fun Enforcement Badge", ":oncoming_police_car:", 0),
						// 0
						new Badge("Microsoft", "I work at Microsoft!", "Tiny Program", ":desktop:", 0),
						// 0
						new Badge("Moon Studios", "I work at Moon Studios!", "Spirit Tree Incarnate", ":crescent_moon:", 0),
						// 0
						new Badge("Bot Creator", "I'm the creator of the bot!", "Programmer God (Albeit Clumsy At Times)", ":tools:", 69),
						// 0
						//new Badge("Character Creator", "I'm the creator of Glenya (the character)!", "Certified Author", ":book:"),
						// 0
						//new Badge("Glenya", "I am Glenya!", "Birb", ":bird:"),
						// 0
						new Badge("Ori", "The spirit themselves!", "Certified Spirit Guardian", "<:OriHype:628302357922316329>"),
						// 0
						//new Badge("Gift of Rarity", "I have been given The Gift of Rarity.", "Special Snowflake (In A Good Way)", "<:purelight:564193055842762763>"),
						// 1000
						new Badge("By Your Side", "One of my responses was approved for use in the old `>> wotw` command (which displayed a countdown to *Will of the Wisps*'s release), and made the waiting process better for everyone.", "Smart Cookie", ":notepad_spiral:", 0, 1000),
						// 3333
						new Badge("Trifecta", "I helped by a substantial amount while testing for the release of OriBot V3.0.0!", "Innovation Assistant", ":small_red_triangle:", 3, 3333),
						// 750
						new Badge("Fanart Friday", "My work was recognized for Fanart Friday!", "Friday-Fun-Maker", ":frame_photo:", 1, 750),
						// 2000
						new Badge("Life of the Party", "I contributed officially recognized art for Ori's 6th Anniversary!", "Artist Extraordinaire", ":cake:", 0, 2000),
						// 5000
						new Badge("Master of the Arts", "My post in <#639160533076934686> accumulated over 100 pin reactions.", "Awe-Inspirer", ":dvd:", 1, 5000),
						// 250
						ART_POSTED_BADGE,
						// 3000
						ART_PINNED_BADGE,
					};
				}
				return BadgeCache;
			}
		}
		// new Badge("Approved Idea", "I suggested something for the bot that ended up getting added as a full feature!\n\nFeature: (eti forgot to fill in the data lol, ping him)", "Professional Thinker", ":bulb:"),

		/// <summary>
		/// Iterates through <see cref="AllBadges"/> and returns a badge with the given name. This returns the literal badge object (its template). Clone the return value of this if you wish to add it to someone's profile.
		/// </summary>
		/// <remarks>
		/// This search is not case sensitive.
		/// </remarks>
		/// <param name="name"></param>
		/// <returns></returns>
		public static Badge GetBadgeFromPredefinedRegistry(string name) {
			foreach (Badge badge in AllBadges) {
				if (badge.Name.ToLower() == name.ToLower()) {
					return badge;
				}
			}
			return null;
		}

		public static Badge ConstructApprovedIdeaBadge(string idea) {
			return new Badge(
				"Approved Idea",
				$"I suggested something for the bot that ended up getting added as a full feature!\n\n**Feature:** {idea}",
				"Professional Thinker",
				":bulb:",
				1,
				2000
			);
		}

	}

	public class Badge : IByteSerializable, IEquatable<Badge>, IEmbeddable {

		/// <summary>
		/// The latest version of the badge format.
		/// </summary>
		public const int LATEST_BADGE_VERSION = 4;

		/// <summary>
		/// True if this badge is a template object defined in <see cref="BadgeRegistry"/>.
		/// </summary>
		public bool IsTemplate { get; protected internal set; } = false;

		/// <summary>
		/// The display name of this <see cref="Badge"/>
		/// </summary>
		public string Name { get; set; } = "";

		/// <summary>
		/// The description or meaning of this <see cref="Badge"/>
		/// </summary>
		public string Description { get; set; } = "";

		/// <summary>
		/// The icon associated with this <see cref="Badge"/>
		/// </summary>
		public string Icon { get; set; } = null;

		/// <summary>
		/// The mini-description which shows next to badge levels.
		/// </summary>
		public string MiniDescription { get; set; } = "";

		/// <summary>
		/// The level of this badge.
		/// </summary>
		public ushort Level { get; set; } = 1;

		/// <summary>
		/// The amount of exp this badge is worth per rank.
		/// </summary>
		public double ExperienceWorth { get; set; } = 0;

		/// <summary>
		/// Returns the experience that this badge is worth at its current rank. This is equal to <c>ExperienceWorth * max(Level, 1)</c>
		/// </summary>
		public double RankedWorth => ExperienceWorth * Math.Max((int)Level, 1);

		/// <summary>
		/// Instantiate a new badge as a template. Manually set <see cref="IsTemplate"/> to <see langword="false"/> if this badge is not a template object.
		/// </summary>
		/// <param name="name">The name of the badge.</param>
		/// <param name="description">The description of the badge, which describes it in detail.</param>
		/// <param name="minidesc">The text that is put as subscript to the badge, which is usually a simple joke text to describe the badge, e.g. the staff badge has "Anti-Fun Police" as its minidesc.</param>
		/// <param name="icon">The icon to use with the badge, which should be an emoji. If this is not specified, it defaults to :question:</param>
		/// <param name="level">The rank of this badge, which is displayed next to the title as roman numerals, and in the minidescription as an integer.</param>
		public Badge(string name, string description, string minidesc = null, string icon = ":question:", ushort level = 1, double experience = 0) {
			Name = name;
			Description = description;
			MiniDescription = minidesc;
			Icon = icon ?? ":question:";
			Level = level;
			IsTemplate = true;
			ExperienceWorth = experience;
		}

		internal Badge() { }

		/// <summary>
		/// Clone this badge to a new instance.
		/// </summary>
		/// <returns></returns>
		public Badge Clone() {
			Badge dupe = new Badge(Name, Description, MiniDescription, Icon, Level, ExperienceWorth) {
				IsTemplate = false
			};
			return dupe;
		}

		public static string ToRoman(int number) {
			if (number > 30) {
				return "Lv. " + number.ToString();
			}
			//number = Math.Min(number, 10);
			if (number >= 10) return "X" + ToRoman(number - 10);
			if (number >= 9) return "IX" + ToRoman(number - 9);
			if (number >= 5) return "V" + ToRoman(number - 5);
			if (number >= 4) return "IV" + ToRoman(number - 4);
			if (number >= 1) return "I" + ToRoman(number - 1);
			return string.Empty;
		}

		public override string ToString() {
			string miniDesc;
			if (MiniDescription != null && MiniDescription != "") {
				if (Level == 0) {
					miniDesc = $"{MiniDescription}\n\n";
				} else {
					miniDesc = $"Level {Level} {MiniDescription}\n\n";
				}
			} else {
				miniDesc = "";
			}
			if (Level > 1) {
				return $"{Icon} __**{Name} {ToRoman(Level)}**__\n{miniDesc}*{Description}*";
			}
			return $"{Icon} __**{Name}**__\n{miniDesc}*{Description}*";
		}

		/// <summary>
		/// Adds this badge to the specified <see cref="EmbedBuilder"/> by creating a new field.
		/// </summary>
		/// <param name="builder"></param>
		public void AppendToEmbed(EmbedBuilder builder, bool inline = false) {
			string name = $"{Icon} __**{Name}";
			string miniDesc;
			if (MiniDescription != null && MiniDescription != "") {
				if (Level == 0) {
					miniDesc = $"{MiniDescription}\n\n";
				} else {
					miniDesc = $"Level {Level} {MiniDescription}\n\n";
				}
			} else {
				miniDesc = "";
			}

			if (Level > 1) {
				name += $" {ToRoman(Level)}**__";
			} else {
				name += "**__";
			}

			string effectiveDesc = '*' + Description;
			int asterisks = Description.Count(chr => chr == '*');
			if (asterisks % 2 == 0) {
				effectiveDesc += '*';
			} // else: odd number of asterisks, probably to stop it early. Don't append

			string content = $"{miniDesc}{effectiveDesc}";
			builder.AddField(name, content, inline);
		}

		public byte[] ToBytes() {
			using MemoryStream memory = new MemoryStream(1024);
			using BinaryWriter writer = new BinaryWriter(memory);
			writer.WriteAsChars("BADG");
			writer.Write(LATEST_BADGE_VERSION);

			writer.WriteStringSL(Name);
			writer.WriteStringSL(Description);
			writer.WriteStringSL(Icon);
			writer.WriteStringSL(MiniDescription);

			writer.Write(Level);
			writer.Write(ExperienceWorth);
				
			return memory.ToArray();
		}

		public int FromBytes(byte[] data) {
			uint badgeVersion = 0;
			if (data.Length > 8) {
				string header = ProfileVersionMarshaller.GetHeader(data);
				if (header == "BADG") {
					badgeVersion = BitConverter.ToUInt32(data, 4);
				}
			}
			using MemoryStream memory = new MemoryStream(data);
			long start = memory.Position;
			memory.Position += 8; // for the header

			using BinaryReader reader = new BinaryReader(memory);
			if (badgeVersion == 4) {
				Name = reader.ReadStringSL();
				Description = reader.ReadStringSL();
				Icon = reader.ReadStringSL();
				MiniDescription = reader.ReadStringSL();
				Level = reader.ReadUInt16();
				ExperienceWorth = reader.ReadDouble();
			}
			return data.Length;
		}

		public static bool operator ==(Badge alpha, Badge bravo) {
			if (alpha is null && bravo is null) return true;
			if (alpha is null || bravo is null) return false;
			return alpha.Equals(bravo);
		}

		public static bool operator !=(Badge alpha, Badge bravo) => !(alpha == bravo);

		public override bool Equals(object obj) {
			if (ReferenceEquals(obj, this)) return true;
			if (obj is null) return false;
			if (obj is Badge badge) return Equals(badge);
			return false;
		}

		/// <summary>
		/// Compares the badges by name.
		/// </summary>
		/// <param name="badge"></param>
		/// <returns></returns>
		public bool Equals([AllowNull] Badge badge) {
			if (badge is null) return false;
			return (badge.Name == Name);
		}

		/// <summary>
		/// Compares this badge to the given other badge by name and name alone, ignoring properties like level, description, icon, etc.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool IsSameBadge(Badge other) {
			return Name == other?.Name;
		}

		public override int GetHashCode() {
			return HashCode.Combine(Name, Description, MiniDescription, Icon, Level, ExperienceWorth);
		}

		public Embed ToEmbed() {
			EmbedBuilder builder = new EmbedBuilder {
				Title = $"Badge Information: {Icon} {Name}",
				Description = MiniDescription
			};
			builder.AddField("Description", Description);
			builder.AddField("Experience Reward Per Rank", ExperienceWorth.ToString());
			return builder.Build();
		}
	}
}
