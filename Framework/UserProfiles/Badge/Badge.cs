using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace OriBot.Framework.UserProfiles.Badges
{
    /// <summary>
    /// <see cref="BadgeRegistry"/> contains a list of all <see cref="Badge"/> that can be used as a template to clone / duplicate <see cref="Badge"/>s for granting to a <see cref="UserProfile"/>.
    ///
    /// </summary>
    public class BadgeRegistry
    {
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
        /// This static method will load all of the <see cref="Badge"/>s into <see cref="BadgeCache"/>
        /// </summary>
        public static void InitializeBadgeRegistry()
        {
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

                        new ApprovedIdeaBadge("Approved Idea","","Professional Thinker",":bulb:",1,2000,"")
                    };
        }

        /// <summary>
        /// Every badge offered by the system.
        /// </summary>
        public static IReadOnlyList<Badge> AllBadges
        {
            get
            {
                if (BadgeCache == null)
                {
                    InitializeBadgeRegistry();
                }
                return BadgeCache;
            }
        }

        // new Badge("Approved Idea", "I suggested something for the bot that ended up getting added as a full feature!\n\nFeature: (eti forgot to fill in the data lol, ping him)", "Professional Thinker", ":bulb:"),

        /// <summary>
        /// Iterates through <see cref="AllBadges"/> and returns a <see cref="Badge"/> with the given name. You are able to use the <see cref="Badge"/>s generated by this function immediately for granting to a <see cref="UserProfile"/>
        /// </summary>
        /// <remarks>
        /// This search is not case sensitive.
        /// <para>
        /// For any badges with custom data, such as the <see cref="ApprovedIdeaBadge"/>, Please see the <see cref="Badge"/>'s <see cref="Badge.Instantiate(string)"/> and <see cref="Badge.Load(string)"/> methods to see how their <see cref="Badge.customData"/> is used.
        /// </para>
        /// <para>
        /// This method also uses the <see cref="Badge.Instantiate(string)"/> method of a <see cref="Badge"/> to create a new <see cref="Badge"/> instance,  as compared to <see cref="LoadBadgeFromString(string)"/> which uses the <see cref="Badge.Load(string)"/> method to create a new <see cref="Badge"/>.
        /// </para>
        /// </remarks>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Badge GetBadgeFromPredefinedRegistry(string name, string customdata = "")
        {
            foreach (Badge badge in AllBadges)
            {
                if (badge.Name.ToLower() == name.ToLower())
                {
                    return badge.Instantiate(customdata);
                }
            }
            return null;
        }

        /// <summary>
        /// Use this method to load a <see cref="Badge"/> from the <see cref="BadgeRegistry"/> by a encoded JSON object.
        /// This method looks at the “Name” property of the JSON object to determine what <see cref="Badge"/> should be loaded from the <see cref="BadgeRegistry"/>
        /// <para>
        /// This method also uses the <see cref="Badge.Load(string)"/> method of a <see cref="Badge"/> to create a new <see cref="Badge"/> instance,  as compared to <see cref="GetBadgeFromPredefinedRegistry(string, string)"/> which uses the <see cref="Badge.Instantiate(string)"/> method to create a new <see cref="Badge"/>.
        /// </para>
        /// </summary>
        /// <param name="jsonstring"></param>
        /// <returns></returns>
        public static Badge LoadBadgeFromString(string jsonstring)
        {
            var loaded = JsonConvert.DeserializeObject<Badge>(jsonstring);
            foreach (Badge badge in AllBadges)
            {
                if (badge.Name.ToLower() == loaded.Name.ToLower())
                {
                    var res = badge.Load(jsonstring);
                    return res;
                }
            }
            return null;
        }
    }

    /// <summary>
    /// <see cref="Badge"/> represents an achievement or award that can be granted to a user profile for either doing something worthy of award.
    /// <para>A <see cref="Badge"/> will have a <see cref="Name"/> which is also used as an identifier internally in the bot system,</para>
    /// <para>A <see cref="Description"/> to signify what this badge means,</para>
    /// <para>A <see cref="MiniDescription"/> to act as a simpler version of <see cref="Description"/>,</para>
    /// <para>A <see cref="Level"/> to indicate at what level this batch is currently at, Note that this is separate from the <see cref="UserProfile.Level"/> and is not connected in anyway,  </para>
    /// <para>An <see cref="ExperienceWorth"/> to indicate how much experience this batch is worth per <see cref="Level"/> </para>
    /// <para>An <see cref="Icon"/> to act as a logo for the badge, use discord emoji syntax for this parameter such as :Ori: :gumo: etc. </para>
    /// <para>An optional <see cref="customData"/> to add custom functionality to this batch based on custom data.</para>
    /// </summary>
    public class Badge
    {
        /// <summary>
        /// The latest version of the badge format.
        /// </summary>
        public const int LATEST_BADGE_VERSION = 5;

        /// <summary>
        /// True if this badge is a template object defined in <see cref="BadgeRegistry"/>.
        /// </summary>

        public bool IsTemplate { get; protected internal set; } = false;

        /// <summary>
        /// The internal name of this <see cref="Badge"/> also used as an ID.
        /// </summary>

        [JsonProperty] public string Name { get; private set; } = "";

        /// <summary>
        /// The display name of this <see cref="Badge"/>
        /// </summary>
        public virtual string DisplayName
        {
            get
            {
                return Name;
            }
        }

        /// <summary>
        /// The description or meaning of this <see cref="Badge"/>
        /// </summary>

        [JsonProperty] public virtual string Description { get; set; } = "";

        /// <summary>
        /// The icon associated with this <see cref="Badge"/>
        /// </summary>

        [JsonProperty] public virtual string Icon { get; set; } = null;

        /// <summary>
        /// The mini-description which shows next to badge levels.
        /// </summary>
        [JsonProperty]
        public virtual string MiniDescription { get; set; } = "";

        /// <summary>
        /// The level of this badge.
        /// </summary>
        [JsonProperty]
        public virtual ushort Level { get; set; } = 1;

        /// <summary>
        /// The amount of exp this badge is worth per rank.
        /// </summary>
        [JsonProperty]
        public virtual double ExperienceWorth { get; set; } = 0;

        /// <summary>
        /// Returns the experience that this badge is worth at its current rank. This is equal to <c>ExperienceWorth * max(Level, 1)</c>
        /// </summary>
        [JsonIgnore]
        public double RankedWorth => ExperienceWorth * Math.Max((int)Level, 1);

        [JsonProperty]
        public string customData { get; set; } = null;

        /// <summary>
        /// Instantiate a new badge as a template. Manually set <see cref="IsTemplate"/> to <see langword="false"/> if this badge is not a template object.
        /// </summary>
        /// <param name="name">The name of the badge.</param>
        /// <param name="description">The description of the badge, which describes it in detail.</param>
        /// <param name="minidesc">The text that is put as subscript to the badge, which is usually a simple joke text to describe the badge, e.g. the staff badge has "Anti-Fun Police" as its minidesc.</param>
        /// <param name="icon">The icon to use with the badge, which should be an emoji. If this is not specified, it defaults to :question:</param>
        /// <param name="level">The rank of this badge, which is displayed next to the title as roman numerals, and in the minidescription as an integer.</param>
        public Badge(string name, string description, string minidesc = null, string icon = ":question:", ushort level = 1, double experience = 0, string CustomData = "")
        {
            Name = name;
            Description = description;
            MiniDescription = minidesc;
            Icon = icon ?? ":question:";
            Level = level;
            IsTemplate = true;
            ExperienceWorth = experience;
            customData = CustomData;
        }

        [JsonConstructor]
        public Badge()
        {
            IsTemplate = false;
        }

        /// <summary>
        /// This function does still representation of this <see cref="Badge"/>.
        /// </summary>
        /// <returns></returns>
        public virtual string AsString()
        {
            string miniDesc;
            if (MiniDescription != null && MiniDescription != "")
            {
                if (Level == 0)
                {
                    miniDesc = $"{MiniDescription}\n\n";
                }
                else
                {
                    miniDesc = $"Level {Level} {MiniDescription}\n\n";
                }
            }
            else
            {
                miniDesc = "";
            }
            if (Level > 1)
            {
                return $"{Icon} __**{Name} {ToRoman(Level)}**__\n{miniDesc}*{Description}*";
            }
            return $"{Icon} __**{Name}**__\n{miniDesc}*{Description}*";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Description, MiniDescription, Icon, Level, ExperienceWorth);
        }

        /// <summary>
        /// This method is used to duplicate this <see cref="Badge"/> instance exactly, along with any custom data in the <paramref name="customdata"/>
        /// </summary>
        /// <remarks>
        /// You are also expected to override this method should you inherit the <see cref="Badge"/> class.
        /// </remarks>
        /// <param name="customdata"></param>
        /// <returns></returns>
        public virtual Badge Instantiate(string customdata)
        {
            Badge dupe = new Badge(Name, Description, MiniDescription, Icon, Level, ExperienceWorth, customdata)
            {
                IsTemplate = false
            };
            return dupe;
        }

        /// <summary>
        /// Use this method to load a generic <see cref="Badge"/> from data, You are not expected to use this method directly to load <see cref="Badge"/>s
        /// To load a badge from JSON, please use <see cref="BadgeRegistry.LoadBadgeFromString(string)"/> instead.
        /// </summary>
        /// <remarks>
        /// You are also expected to override this method should you inherit the <see cref="Badge"/> class.
        /// </remarks>
        /// <param name="jsonobject"></param>
        /// <returns></returns>
        public virtual Badge Load(string jsonobject)
        {
            var loaded = JsonConvert.DeserializeObject<Badge>(jsonobject);
            return loaded;
        }

        /// <summary>
        /// This method will serialize this current <see cref="Badge"/> instance has a JSON string
        /// </summary>
        /// <returns></returns>
        public virtual string Save()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }

        public static string ToRoman(int number)
        {
            if (number > 30)
            {
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

        public static bool operator ==(Badge alpha, Badge bravo)
        {
            if (alpha is null && bravo is null) return true;
            if (alpha is null || bravo is null) return false;
            return alpha.Equals(bravo);
        }

        public static bool operator !=(Badge alpha, Badge bravo) => !(alpha == bravo);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this)) return true;
            if (obj is null) return false;
            if (obj is Badge badge) return base.Equals(badge);
            return false;
        }
    }

    public class ApprovedIdeaBadge : Badge
    {
        private string idea = "";

        public new string Description
        { get { return $"I suggested something for the bot that ended up getting added as a full feature!\n\n**Feature:** {idea}"; } }

        public ApprovedIdeaBadge(string name, string description, string minidesc = null, string icon = ":question:", ushort level = 1, double experience = 0, string customData = "") : base(name, description, minidesc, icon, level, experience, customData)
        {
        }

        public new ApprovedIdeaBadge Load(string jsonobject)
        {
            var loaded = (ApprovedIdeaBadge)JsonConvert.DeserializeObject<Badge>(jsonobject);
            if (loaded != null)
            {
                loaded.idea = loaded.customData;
                loaded.IsTemplate = false;
                return loaded;
            }
            return null;
        }

        public virtual new string Save()
        {
            customData = idea;
            return JsonConvert.SerializeObject(this, Formatting.None);
        }

        public new Badge Instantiate(string customdata)
        {
            ApprovedIdeaBadge dupe = new ApprovedIdeaBadge(Name, Description, MiniDescription, Icon, Level, ExperienceWorth, customdata)
            {
                IsTemplate = false
            };
            dupe.idea = dupe.customData;
            return dupe;
        }
    }
}