using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Universal;

using EtiLogger.Data.Structs;
using EtiLogger.Logging;

using OldOriBot.Data;
using OldOriBot.Data.MemberInformation;
using OldOriBot.Interaction;
using OldOriBot.PermissionData;
using OldOriBot.UserProfiles.Extension;
using OldOriBot.Utility;
using OldOriBot.Utility.Extensions;
using OldOriBot.Utility.Formatting;
using OldOriBot.Utility.Responding;

using static OldOriBot.UserProfiles.ProfileVersionMarshaller;

namespace OldOriBot.UserProfiles
{
    /// <summary>
    /// Represents a user's profile.
    /// </summary>
    public class UserProfile : IComparable<UserProfile>
    {
        public static readonly double[] LevelToExperience = new double[MAX_LEVEL + 1];

        //  private static readonly Logger ProfileLogger = new Logger("§9[User Profile System] ")
        //   {
        //       NoLevel = true
        //   };

        static UserProfile()
        {
            //  ProfileLogger.WriteLine("§2Instantiating all levels...");
            double req = 0;
            LevelToExperience[0] = 0;
            for (int lvl = 1; lvl <= MAX_LEVEL; lvl++)
            {
                req += LEVEL_1_THRESHOLD * PER_LEVEL_MULT * lvl;
                LevelToExperience[lvl] = req;
            }
            MAX_EXPERIENCE = req;
            //   ProfileLogger.WriteLine("§2Done instantiating levels.");

            Directory.CreateDirectory(ProfileDirectoryName);
        }

        #region Constants

        /// <summary>
        /// A string of the *true* double-precision floating point max value.
        /// </summary>
#pragma warning disable IDE0051 // Remove unused private members
        private const string MAX_DOUBLE_FULL_DETAIL = "179,769,313,486,231,570,814,527,423,731,704,356,798,070,567,525,844,996,598,917,476,803,157,260,780,028,538,760,589,558,632,766,878,171,540,458,953,514,382,464,234,321,326,889,464,182,768,467,546,703,537,516,986,049,910,576,551,282,076,245,490,090,389,328,944,075,868,508,455,133,942,304,583,236,903,222,948,165,808,559,332,123,348,274,797,826,204,144,723,168,738,177,180,919,299,881,250,404,026,184,124,858,368";
#pragma warning restore IDE0051 // Remove unused private members

        /// <summary>
        /// The amount of exp required for level 1.
        /// </summary>
        private const double LEVEL_1_THRESHOLD = 40;

        /// <summary>
        /// The amount that <see cref="LEVEL_1_THRESHOLD"/> is multiplied by for every level to make a new experience requirement.
        /// </summary>
        private const double PER_LEVEL_MULT = 1.0025;

        /// <summary>
        /// The maximum amount of experience it's possible to have. Calculated from <see cref="MAX_LEVEL"/>
        /// </summary>
        public static readonly double MAX_EXPERIENCE;

        /// <summary>
        /// The maximum achievable level.
        /// </summary>
        public const int MAX_LEVEL = 10000;

        /// <summary>
        /// The default values in the profile's customdata that the user can set.
        /// </summary>
        public static IReadOnlyDictionary<string, object> DefaultProfileConfigs { get; } = new Dictionary<string, object>()
        {
            ["DefaultDenyPins"] = false,
            ["DMWhenArtPinned"] = false,
            ["HasMadeFirstGalleryPost"] = false,
            ["HasSeenEmojiSubmissionReminder"] = false,
            [ResponseUtil.PING_ON_REPLY] = true,
        };

        #endregion Constants

        #region Props

        /// <summary>
        /// The name of the directory storing user profiles. The default is <c>C:\EtiBotCore\.profiles</c>
        /// </summary>
        public static string ProfileDirectoryName
        {
            get
            {
                if (ProfileDirectoryNameInternal == null)
                {
                    if (Directory.Exists("V:\\"))
                    {
                        ProfileDirectoryNameInternal = @"V:\EtiBotCore\.profiles";
                    }
                    else
                    {
                        ProfileDirectoryNameInternal = @"C:\EtiBotCore\.profiles";
                    }
                }
                return ProfileDirectoryNameInternal;
            }
            set => ProfileDirectoryNameInternal = value;
        }

        private static string ProfileDirectoryNameInternal = null;

        /// <summary>
        /// The version that this profile exists as.
        /// </summary>
        public int Version { get; internal set; } = 0;

        /// <summary>
        /// The latest version handler, which is also the handler used to save this file.
        /// </summary>
        private ProfileVersion LatestVersionHandler { get; set; }

        /// <summary>
        /// The documented number of messages this user has sent.
        /// </summary>
        public uint MessagesSent { get; set; } = 0;

        /// <summary>
        /// The base experience this user has. Not clamped. Needs to be clamped in the associated functions.
        /// </summary>
        public double Experience
        {
            get => _Experience;
            set
            {
                _Experience = value;
                if (_Experience % 10 == 0) Save(); // lol
                                                   // now I won't spam the filesystem with updates
            }
        }

        internal double _Experience = 0;
        // TODO: Acceptable to save every time this is set?

        /// <summary>
        /// The total experience this user has from badges and base experience combined. Clamped to <see cref="MAX_EXPERIENCE"/>.
        /// </summary>
        public double OverallExperience => Math.Min(GetExperienceFromBadges() + Experience, MAX_EXPERIENCE);

        /// <summary>
        /// This user's level. This is calculated via the experience of badges + the base experience of the user.
        /// </summary>
        public double Level => GetLevel(OverallExperience);

        /// <summary>
        /// A reference to the <see cref="Member"/> this <see cref="UserProfile"/> belongs to.
        /// </summary>
        public Member Member { get; }

        /// <summary>
        /// The <see cref="BotContext"/> of the <see cref="Member"/> this profile exists for.
        /// </summary>
        public BotContext Context { get; }

        /// <summary>
        /// The title this user has currently. Setting this will save the profile.
        /// </summary>
        public string Title
        {
            get
            {
                return _Title;
            }
            set
            {
                //_Title = CleanUpInput(value);
                if (value == null || value == default)
                {
                    value = "";
                }
                _Title = value;
                Save();
            }
        }

        internal string _Title = "";

        /// <summary>
        /// The description this user has currently. Setting this will save the profile.
        /// </summary>
        public string Description
        {
            get
            {
                return _Description;
            }
            set
            {
                //_Desc = CleanUpInput(value);
                if (value == null || value == default)
                {
                    value = "";
                }
                _Description = value;
                Save();
            }
        }

        internal string _Description = "";

        /// <summary>
        /// The color of this user's embed. A null color means to use Discord's default. Setting this will save the profile.
        /// </summary>
        public int? Color
        {
            get => _Color;
            set
            {
                _Color = value;
                Save();
            }
        }

        internal int? _Color = null;

        /// <summary>
        /// If true, the field limit imposed when saving data is ignored (allowing fields to have an arbitrary length.)
        /// </summary>
        public bool HasFieldLimitBypass { get; set; } = false;

        /// <summary>
        /// The badges this user owns.
        /// </summary>
        internal readonly List<Badge> BadgesInternal = new List<Badge>();

        /// <summary>
        /// The badges this user owns.
        /// </summary>
        public IReadOnlyList<Badge> Badges => BadgesInternal.AsReadOnly();

        private static readonly Dictionary<Snowflake, UserProfile> ProfileCache = new Dictionary<Snowflake, UserProfile>();

        /// <summary>
        /// A reference to every profile that has been instantiated.
        /// </summary>
        public static IReadOnlyDictionary<Snowflake, UserProfile> AllProfiles => ProfileCache;

        /// <summary>
        /// This user's custom config data.
        /// </summary>
        public UserDataContainer UserData
        {
            get
            {
                if (_UserData == null) _UserData = new UserDataContainer(this);
                return _UserData;
            }
        }

        private UserDataContainer _UserData = null;

        /// <summary>
        /// A function that returns whether or not to automatically update preset badges to their new content.
        /// </summary>
        public static bool AutoUpdateBadges { get; set; } = true;

        #endregion Props

        /// <summary>
        /// Returns the total experience of all badges this user has. Clamped to <see cref="MAX_EXPERIENCE"/>
        /// </summary>
        /// <returns></returns>
        public double GetExperienceFromBadges()
        {
            double exp = 0;
            foreach (Badge badge in BadgesInternal)
            {
                exp += badge.RankedWorth;
            }
            return Math.Min(exp, MAX_EXPERIENCE);
        }

        /// <summary>
        /// Returns the level that the given experience calculates out to. If the experience is too high, it returns <see cref="MAX_LEVEL"/>.
        /// </summary>
        /// <param name="experienceOffset"></param>
        /// <returns></returns>
        public double GetLevel(double experienceOffset = 0, int keyOffset = 0)
        {
            int highest = 0;
            for (int level = keyOffset; level < LevelToExperience.Length - keyOffset; level++)
            {
                if (LevelToExperience[level] <= experienceOffset)
                {
                    highest = level;
                }
                else
                {
                    // Specifically found a level higher. We didn't run out.
                    return highest;
                }
            }
            return MAX_LEVEL;
        }

        /// <summary>
        /// Cleans up text input for titles and descriptions, preventing spam.<para/>
        /// This removes all instances of "\r", and then replaces triplets of "\n" with duos instead.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string CleanUpInput(string value)
        {
            if (value == null || value == default) return value;
            value = value.Replace("\r", "");
            while (value.Contains("\n\n\n"))
            {
                value = value.Replace("\n\n\n", "\n\n");
            }
            return value;
        }

        public static async Task UpdateAllProfiles(BotContext ctx, bool onlyExisting = true)
        {
            Directory.CreateDirectory(ProfileDirectoryName);

            if (onlyExisting)
            {
                Member[] members = await ctx.Server.DownloadAllMembersAsync();
                Dictionary<ulong, Member> memberBindings = new Dictionary<ulong, Member>();
                foreach (Member m in members)
                {
                    memberBindings[m.ID] = m;
                }

                FileInfo[] files = new DirectoryInfo(ProfileDirectoryName).GetFiles();
                //ProfileLogger.WriteLine($"Starting update on {files.Length} files...");
                int i = 1;
                foreach (FileInfo file in files)
                {
                    string idStr = file.Name.Replace(".profile", "");
                    if (ulong.TryParse(idStr, out ulong userId))
                    {
                        if (memberBindings.TryGetValue(userId, out Member member) && !member.LeftServer)
                        {
                            GetOrCreateProfileOf(member);
                            //ProfileLogger.WriteLine($"Updated profile of user {member.FullName} ({i} of {files.Length})");
                            // profile.Save();
                            // Using exit-save
                        }
                        else
                        {
                            //ProfileLogger.WriteLine($"UserID {userId} is not in this server anymore! Renaming profile to flag for deletion...");
                            file.MoveTo(file.FullName + ".NONMEMBER");
                        }
                    }
                    else
                    {
                        //ProfileLogger.WriteLine($"§4Failed to cast {idStr} into ulong");
                    }
                }
            }
            else
            {
                Member[] members = await ctx.Server.DownloadAllMembersAsync();
                foreach (Member member in members)
                {
                    GetOrCreateProfileOf(member);
                }
            }
        }

        private UserProfile(Snowflake memberId, BotContext ctx) : this(ctx.Server.GetMemberAsync(memberId).Result)
        {
        }

        private UserProfile(Member member)
        {
            Member = member;
            Context = BotContextRegistry.GetContext(member.Server.ID);
            ProfileCache[member.ID] = this;

            // Load from data persistence
            string memberProfile = Member.ID.ToString() + ".profile";
            FileInfo target = new FileInfo(Path.Combine(ProfileDirectoryName, memberProfile));

            if (target.Exists)
            {
                byte[] data;
                // File exists.
                FileStream stream = target.Open(FileMode.Open);
                MemoryStream dataStream = new MemoryStream();
                stream.CopyTo(dataStream);
                data = dataStream.ToArray();
                stream.Dispose();
                dataStream.Dispose();

                try
                {
                    ProfileVersion versionHandler = GetProfileVersionHandlerFor(this, data);
                    if (!versionHandler.Faulted)
                    {
                        versionHandler.FromBytes(data);
                        //ProfileLogger.WriteLine("§3Loaded user profile for " + member.FullName, LogLevel.Trace);
                    }
                    else
                    {
                        //ProfileLogger.WriteLine("§cProfile faulted! Failed to load profile for " + member.FullName, LogLevel.Debug);
                    }
                    //ProfileLogger.WriteLine("§3Saving under latest format...", LogLevel.Trace);
                    Save();
                }
                catch (Exception exc)
                {
                    //ProfileLogger.WriteException(exc, false, LogLevel.Debug);
                }
            }
            else
            {
                Save();
            }

            LatestVersionHandler = GetLatestHandler(this);

            EmojiBadgeGenerator.GenerateBadge(this);
        }

        private UserProfile(ulong member, byte[] data)
        {
            ProfileCache[member] = this;
            // Load from data persistence

            // File exists.

            try
            {
                ProfileVersion versionHandler = GetProfileVersionHandlerFor(this, data);
                if (!versionHandler.Faulted)
                {
                    versionHandler.FromBytes(data);
                    Console.WriteLine("Loaded profile for: " + member);
                }
                else
                {
                    Console.WriteLine("Fail to load profile for: " + member);
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("Exception in profile loader: " + member + " , " + exc.ToString());
            }

            LatestVersionHandler = GetLatestHandler(this);

            //EmojiBadgeGenerator.GenerateBadge(this);
        }

        /// <summary>
        /// Attempts to get a <see cref="UserProfile"/> from the specified userId, but if the member is no longer in the server, will return null.<para/>
        /// This will create a new file.
        /// </summary>
        /// <param name="userId"></param>
        private static async Task<UserProfile> TryGetUserProfile(BotContext inContext, ulong userId)
        {
            Member member = await inContext.Server.GetMemberAsync(userId);
            if (member == null) return null;
            return new UserProfile(member);
        }

        /// <summary>
        /// Gets an array of <see cref="UserProfile"/>s representing all of the files that exist in the server at this time.<para/>
        /// These profiles are sorted by experience.
        /// </summary>
        /// <param name="inContext"></param>
        /// <returns></returns>
        public static async Task<UserProfile[]> GetAllExistingProfiles(BotContext inContext)
        {
            DirectoryInfo dir = new DirectoryInfo(ProfileDirectoryName);
            FileInfo[] files = dir.GetFiles();
            UserProfile[] profiles = new UserProfile[files.Length];
            for (int idx = 0; idx < files.Length; idx++)
            {
                FileInfo profileFile = files[idx];
                string userIdStr = profileFile.Name.Replace(".profile", "");
                if (ulong.TryParse(userIdStr, out ulong userId))
                {
                    UserProfile profile = await TryGetUserProfile(inContext, userId);
                    profiles[idx] = profile;
                }
                else
                {
                    profiles[idx] = null;
                }
            }
            Array.Sort(profiles);
            return profiles;
        }

        /// <summary>
        /// Gets an existing profile or creates a new profile for this user.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static UserProfile GetOrCreateProfileOf(Member member)
        {
            if (ProfileCache.TryGetValue(member.ID, out UserProfile profile))
            {
                return profile;
            }
            return new UserProfile(member);
        }

        public static UserProfile GetOrCreateProfileOf2(ulong member, byte[] data)
        {
            
            if (ProfileCache.TryGetValue(member, out UserProfile profile))
            {
                return profile;
            }
            return new UserProfile(member, data);
        }

        /// <summary>
        /// Gets an existing profile or creates a new profile for this user.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static UserProfile GetOrCreateProfileOf(Snowflake memberID, BotContext ctx)
        {
            if (ProfileCache.TryGetValue(memberID, out UserProfile profile))
            {
                return profile;
            }
            return new UserProfile(memberID, ctx);
        }

        /// <summary>
        /// Grants the specified badge to this profile. If the badge is indeed newly added to the profile, this clones the input <paramref name="badge"/>, which is expected to be a data container. It returns the clone.<para/>
        /// If the badge already exists for the user, it returns the existing badge instance stored in their profile.
        /// </summary>
        /// <param name="badge">The badge to add. If a badge with data equal to the given instance already exists on the user's profile, it will not be added.</param>
        /// <param name="upgradeIfAlreadyExists">If true, and if a badge with data equal to <paramref name="badge"/> already exists, the existing badge will have its level incremented by 1.</param>
        /// <param name="replaceIfAlreadyExists">Overrides the behavior of <paramref name="upgradeIfAlreadyExists"/>, and will instead remove the old badge and replace it with the given instance.</param>
        /// <exception cref="ArgumentNullException"/>
        public Badge GrantBadge(Badge badge, bool upgradeIfAlreadyExists = false, bool replaceIfAlreadyExists = false)
        {
            if (badge == null) throw new ArgumentNullException("badge");
            badge = badge.Clone();
            if (BadgesInternal.Contains(badge))
            {
                if (replaceIfAlreadyExists)
                {
                    BadgesInternal.Remove(badge);
                }
                else
                {
                    Badge existing = GetBadgeFromData(badge);
                    if (upgradeIfAlreadyExists)
                    {
                        existing.Level++;
                    }
                    Save();
                    return badge;
                }
            }
            BadgesInternal.Add(badge);

            // Save when this happens
            Save();

            return badge;
        }

        /// <summary>
        /// Removes the specified badge from this profile.
        /// </summary>
        /// <param name="badge"></param>
        /// <exception cref="ArgumentNullException"/>
        public void RemoveBadge(Badge badge)
        {
            if (badge == null) throw new ArgumentNullException("badge");
            if (!BadgesInternal.Contains(badge)) return;
            BadgesInternal.Remove(badge);

            // Save when this happens
            Save();
        }

        /// <summary>
        /// Returns whether or not this profile has the specified badge.
        /// </summary>
        /// <param name="badge"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"/>
        public bool HasBadge(Badge badge)
        {
            if (badge == null) throw new ArgumentNullException("badge");
            return BadgesInternal.Contains(badge);
        }

        /// <summary>
        /// Since methods like <see cref="HasBadge(Badge)"/> return true for badges with identical data (and not necessarily the same instance), this method will acquire the badge on a user's profile and return that instance, which is the instance that should be edited if you wish to update the badge's information.<para/>
        /// Returns null if the user does not have the given badge.
        /// </summary>
        /// <param name="badge"></param>
        /// <returns></returns>
        public Badge GetBadgeFromData(Badge badge)
        {
            if (!HasBadge(badge)) return null;
            foreach (Badge eBadge in BadgesInternal)
            {
                if (eBadge == badge)
                {
                    return eBadge;
                }
            }
            return null;
        }

        /// <summary>
        /// Saves this profile to file.
        /// </summary>
        public void Save()
        {
            try
            {
                string memberProfile = Member.ID.ToString() + ".profile";
                FileInfo file = new FileInfo(Path.Combine(ProfileDirectoryName, memberProfile));
                if (file.Exists)
                {
                    file.Delete();
                }
                if (LatestVersionHandler == null) LatestVersionHandler = GetLatestHandler(this);
                FileStream stream = file.Open(FileMode.CreateNew);
                MemoryStream dataStream = new MemoryStream(LatestVersionHandler.ToBytes());
                dataStream.CopyTo(stream);
                stream.Dispose();
                dataStream.Dispose();

                //ProfileLogger.WriteLine("§3Saved user profile for " + Member.FullName, LogLevel.Trace);
            }
            catch (Exception ex)
            {
                //ProfileLogger.WriteException(ex);
            }
        }

        public static void SaveAll()
        {
            foreach (UserProfile profile in AllProfiles.Values)
            {
                profile.Save();
            }
        }

        public void Reload()
        {
            string memberProfile = Member.ID.ToString() + ".profile";
            FileInfo file = new FileInfo(Path.Combine(ProfileDirectoryName, memberProfile));
            if (file.Exists)
            {
                byte[] data;
                // File exists.
                FileStream stream = file.Open(FileMode.Open);
                MemoryStream dataStream = new MemoryStream();
                stream.CopyTo(dataStream);
                data = dataStream.ToArray();
                stream.Dispose();
                dataStream.Dispose();

                try
                {
                    Title = "";
                    Description = "";
                    Color = null;
                    BadgesInternal.Clear();
                    //LatestVersionHandler.FromBytes(data);
                    ProfileVersion handler = GetProfileVersionHandlerFor(this, data);
                    handler.FromBytes(data);
                    //ProfileLogger.WriteLine("§3Reloaded user profile for " + Member.FullName, LogLevel.Debug);
                }
                catch (Exception exc)
                {
                    //ProfileLogger.WriteException(exc, false);
                }
            }
        }

        public Embed ToEmbed(bool isLite = false)
        {
            EmbedBuilder builder = new EmbedBuilder
            {
                Title = $"User Profile: {Member.FullNickname}"
            };
            if (Color != null && !Member.IsShallow)
            {
                builder.Color = new Color(Color.Value);
            }
            else if (Member.IsShallow)
            {
                builder.Color = new Color(0xFF0000);
            }
            if (!isLite && !Member.IsShallow)
            {
                if (!string.IsNullOrWhiteSpace(Title)) builder.Description = Title;
                if (!string.IsNullOrWhiteSpace(Description)) builder.AddField("User Description", Description);
            }
            else if (Member.IsShallow)
            {
                builder.Description = "Note: This user is not a member of the server.";
            }

            string level;
            string exp = OverallExperience.ToString("N0");
            if (OverallExperience >= MAX_EXPERIENCE || Level >= MAX_LEVEL)
            {
                exp = "∞";
                level = "∞";
            }
            else
            {
                level = Level.ToString("N0");
            }

            DateTimeOffset creation = Member.ID.ToDateTimeOffset();
            string creationTS = creation.AsDiscordTimestamp();
            string creationAge = creation.GetTimeDifferenceFrom();
            DateTimeOffset joinDate = Member.JoinedAt;

            // personal override for my first time in this server.
            if (Member.ID == 114163433980559366)
            {
                joinDate = DateTimeOffset.FromUnixTimeSeconds(1560097303);
            }

            string joinTS = joinDate.AsDiscordTimestamp();
            string joinAge = joinDate.GetTimeDifferenceFrom();
            string permLvlName = Member.GetPermissionLevel().GetFullName(false);

            string joinedTerminology = Member.IsSelf ? "__**Development Started On:**__" : "__**Joined Discord On:**__";
            string serverTerminology = Member.IsSelf ? "__**Authorized For Use On:**__" : "__**Joined This Server On:**__";

            if (!Member.IsShallow)
            {
                builder.AddField("Ranking", $"__**Total Experience:**__ `{exp}`\n__**Level:**__ `{level}`\n__**Permission Level:**__ `{permLvlName}`");
            }
            if (!Member.IsShallow)
            {
                builder.AddField("Age", $"{joinedTerminology} {creationTS}\n({creationAge} ago)\n{serverTerminology} {joinTS}\n({joinAge} ago)");
            }
            else
            {
                builder.AddField("Age", $"__**Joined Discord On:**__ {creationTS}\n({creationAge} ago)");
            }
            builder.AddTimeFormatFooter();

            if (!isLite && !Member.IsShallow)
            {
                if (BadgesInternal.Count > 0)
                {
                    builder.AddField("Badges", "Here's the badges I've unlocked!");
                    foreach (Badge badge in BadgesInternal)
                    {
                        badge.AppendToEmbed(builder, true);
                    }
                }
            }

            // Save when this happens
            Save();

            return builder.Build();
        }

        public int CompareTo(UserProfile other)
        {
            if (other is UserProfile)
            {
                if (other.Experience > Experience)
                {
                    return -1;
                }
                else if (other.Experience < Experience)
                {
                    return 1;
                }
            }
            return 0;
        }
    }
}