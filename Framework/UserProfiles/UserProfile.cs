using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Discord;
using Discord.WebSocket;
using EtiBotCore.DiscordObjects.Universal;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;

using OldOriBot.Data.MemberInformation;

using OriBot.Framework.UserBehaviour;
using OriBot.Framework.UserProfiles;
using OriBot.Framework.UserProfiles.Badges;
using OriBot.Framework.UserProfiles.BehaviourLogContainer;
using OriBot.Framework.UserProfiles.PerGuildData;
using OriBot.Framework.UserProfiles.ProfileConfig;
using OriBot.Utilities;

namespace OriBot.Framework.UserProfiles
{
    /// <summary>
    /// <see cref="UserProfile"/> represents a single user's data on Oricord.
    /// </summary>
    public class UserProfile
    {
        #region Constants

        /// <summary>
        /// This is the absolute maximum amount of levels that a user can have.
        /// </summary>
        [JsonIgnore]
        private const int MAX_LEVEL = 10000;

        /// <summary>
        /// This is the amount that <see cref="LEVEL_1_THRESHOLD"/> is multiplied by for the next level
        ///
        /// <para>Level 1 = <see cref="LEVEL_1_THRESHOLD"/> * (<see cref="PER_LEVEL_MULT"/> * 1) XP</para>
        /// <para>Level 2 = <see cref="LEVEL_1_THRESHOLD"/> * (<see cref="PER_LEVEL_MULT"/> * 2) XP</para>
        /// <para>Level 3 = <see cref="LEVEL_1_THRESHOLD"/> * (<see cref="PER_LEVEL_MULT"/> * 3) XP</para>
        ///
        /// etc.
        /// </summary>
        [JsonIgnore]
        private const double PER_LEVEL_MULT = 1.0025;

        /// <summary>
        /// This is the amount of experience that you are required to get to Level 1.
        /// If you are below <see cref="LEVEL_1_THRESHOLD"/>, you are Level 0
        /// </summary>
        [JsonIgnore]
        private const double LEVEL_1_THRESHOLD = 40;

        /// <summary>
        /// This array maps the required amount of experience to get to a certain level, for the calculation details please see the comment at <see cref="PER_LEVEL_MULT"/>.
        /// </summary>
        [JsonIgnore]
        public static readonly double[] LevelToExperience = new double[MAX_LEVEL + 1];

        /// <summary>
        /// <para>This is the maximum amount of experience a single user profile can have, this value is calculated from <see cref="MAX_LEVEL"/>.</para>
        /// <para>See calculations at <see cref="UserProfile"/> private constructor static UserProfile()</para>
        /// </summary>
        [JsonIgnore]
        public static readonly double MAX_EXPERIENCE;

        /// <summary>
        /// This field is used by <see cref="BaseStorageDir"/>, to determine after $CWD/Data/ what the folder name will be.
        /// </summary>
        [JsonIgnore]
        private static string StorageFolderName = Config.properties["userProfileFolderName"];

        #endregion Constants

        #region Variable Fields & User Data

        /// <summary>
        /// The amount of messages sent by this user profile, setting this property will also *NOT* automatically save the user profile.
        /// </summary>

        [JsonIgnore]
        public long MessagesSent
        {
            get => _MessagesSent;
            set
            {
                _MessagesSent = value;
            }
        }

        [JsonIgnore]
        public bool IsMuted => _MutedTimerID != "";

        [JsonIgnore]
        public string MutedTimerID {
            get => _MutedTimerID;
            set {
                _MutedTimerID = value;
                Save();
            }
        }

        [JsonIgnore]
        public bool IsBanned {
            get => _IsBanned;
            set {
                _IsBanned = value;
                Save();
            }
        }

        [JsonProperty]
        private bool _IsBanned = false;

        [JsonProperty]
        private long _MessagesSent = 0;

        [JsonProperty]
        private string _Title = "";

        [JsonProperty]
        private double _BaseExperience = 0;

        [JsonProperty]
        private string _Description = "";

        [JsonProperty]
        private uint _Color = 0;

        [JsonProperty]
        private string _MutedTimerID = "";

        [JsonIgnore]
        private ProfileConfigs _ProfileConfig = ProfileConfigs.Load(null, () =>
        {
        });

        [JsonIgnore]
        private PerGuildDataContainer _PerGuildDataContainer = null;

        [JsonIgnore]
        private UserBehaviourLogContainer _BehaviourLogs = null;

        [JsonIgnore]
        private DiagnosticLogContainer _DiagnosticLogs = null;

        [JsonIgnore]
        private TicketManager _TicketManager = null;

        /// <summary>
        /// This is the profile config for this user.
        /// To read this property as a dictionary run: <see cref="ProfileConfigs.Config"/>, which is read only.
        /// To modify / write to this property, use the [] accessor on <see cref="ProfileConfigs"/>.
        /// Any writes or modifications will save this user profile immediately.
        /// </summary>
        [JsonIgnore]
        public ProfileConfigs ProfileConfig
        {
            get
            {
                if (_ProfileConfig == null)
                {
                    _ProfileConfig = ProfileConfigs.Load(null, () =>
                    {
                        Save();
                    });
                }
                return _ProfileConfig;
            }

            private set
            {
                _ProfileConfig = value;
            }
        }

        [JsonIgnore]
        public PerGuildDataContainer PerGuildData
        {
            get
            {
                if (_PerGuildDataContainer == null)
                {
                    _PerGuildDataContainer = PerGuildDataContainer.Load(null, () =>
                    {
                        Save();
                    });
                    Save();
                }
                return _PerGuildDataContainer;
            }

            private set
            {
                _PerGuildDataContainer = value;
            }
        }

        [JsonIgnore]
        public UserBehaviourLogContainer BehaviourLogs
        {
            get
            {
                if (_BehaviourLogs == null)
                {
                    _BehaviourLogs = UserBehaviourLogContainer.Load(null, () =>
                    {
                        Save();
                    });
                    Save();
                }
                return _BehaviourLogs;
            }

            private set
            {
                _BehaviourLogs = value;
            }
        }

        [JsonIgnore]
        public DiagnosticLogContainer DiagnosticLogs
        {
            get
            {
                if (_DiagnosticLogs == null)
                {
                    _DiagnosticLogs = DiagnosticLogContainer.Load(null, () =>
                    {
                        Save();
                    });
                    Save();
                }
                return _DiagnosticLogs;
            }

            private set
            {
                _DiagnosticLogs = value;
            }
        }

        [JsonIgnore]
        public TicketManager TicketManager
        {
            get
            {
                if (_TicketManager == null)
                {
                    _TicketManager = new TicketManager(() => {
                        Save();
                    });
                    Save();
                }
                return _TicketManager;
            }

            private set
            {
                _TicketManager = value;
            }
        }

        /// <summary>
        /// The public title of this user. Setting this property will also automatically save the user profile.
        /// </summary>
        [JsonIgnore]
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

        /// <summary>
        /// The color of this user's embed. A null color means to use Discord's default. Setting this will save the profile.
        /// </summary>
        [JsonIgnore]
        public uint Color
        {
            get => _Color;
            set
            {
                _Color = value;
                Save();
            }
        }

        /// <summary>
        /// The description this user has currently. Setting this will save the profile.
        /// </summary>

        [JsonIgnore]
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

        /// <summary>
        /// The total amount of experience this user has.
        /// Calculated by adding <see cref="MessagesSent"/> and <see cref="BaseExperience"/> together, along with all <see cref="Badge.RankedWorth"/> in <see cref="Badges"/>.
        ///
        /// </summary>
        [JsonIgnore]
        public double TotalExperience => Badges.Aggregate<Badge, double>(MessagesSent + _BaseExperience,
       (accumulator, badge) =>
        {
            return accumulator + badge.RankedWorth;
        });

        /// <summary>
        /// The amount of base experience with user profile has before <see cref="MessagesSent"/>.
        /// </summary>
        [JsonIgnore]
        public double BaseExperience
        {
            get => _BaseExperience;
            set
            {
                _BaseExperience = value;
                Save();
            }
        }

        #endregion Variable Fields & User Data

        #region Fixed Properties

        /// <summary>
        /// This is the folder where all user profiles will be stored, use <see cref="StorageFolderName"/> to adjust what the directory will be called after $CWD/Data/
        /// </summary>
        [JsonIgnore]
        private static string BaseStorageDir => Path.Combine(Environment.CurrentDirectory, "Data", StorageFolderName);

        /// <summary>
        /// This property determines what the user profile file will be called.
        /// The file name ends with a .json to indicate that this is the new user profile file format.
        /// </summary>
        [JsonIgnore]
        public string CurrentFileName => Path.Combine(BaseStorageDir, $"{UserID}.json");

        /// <summary>
        /// This property determines what the user profile file will be called.
        /// The file name ends with a .profile to indicate that this is the old user profile file format.
        /// This property should only be used during the migration process to the new user profile file format ending with .json
        /// </summary>
        [JsonIgnore]
        public string LegacyFileName => Path.Combine(BaseStorageDir, $"{UserID}.profile");

        #endregion Fixed Properties

        #region Loaders and Unloaders

        /// <summary>
        /// <para>This property handles loading and unloading of all <see cref="Badge"/>s , To serialize / save all <see cref="Badge"/>s in this user profile simply read from this property.</para>
        /// <para>To load all <see cref="Badge"/>s into this user profile simply set this property and this class will handle all the rest. </para>
        /// </summary>
        [JsonProperty]
        public string SerializedBadges
        {
            get
            {
                List<string> strings = new List<string>();
                foreach (var badge in BadgesInternal)
                {
                    strings.Add(badge.Save());
                }
                return JsonConvert.SerializeObject(strings, Formatting.None);
            }

            set
            {
                List<string> strings = JsonConvert.DeserializeObject<List<string>>(value);
                List<Badge> result = new List<Badge>();
                foreach (var badge in strings)
                {
                    result.Add(BadgeRegistry.LoadBadgeFromString(badge));
                }
                BadgesInternal = result;
            }
        }

        /// <summary>
        /// <para>This property handles loading and unloading of <see cref="ProfileConfig"/>, To serialize / save <see cref="ProfileConfig"/> in this user profile, simply read from this property.</para>
        /// <para>To load <see cref="ProfileConfig"/> into this user profile simply set this property and this class will handle all the rest. </para>
        /// </summary>
        [JsonProperty]
        public string SerializedProfileConfig
        {
            get
            {
                return JsonConvert.SerializeObject(ProfileConfig.Config, Formatting.None);
            }

            set
            {
                ProfileConfig = ProfileConfigs.Load(value, () =>
                {
                    Save();
                });
            }
        }

        [JsonProperty]
        public string SerializedPerGuildData
        {
            get
            {
                return PerGuildData.Serialized;
            }

            set
            {
                PerGuildData = PerGuildDataContainer.Load(value, () => { Save(); });
            }
        }

        [JsonProperty]
        public string SerializedUserBehaviourLogs
        {
            get
            {
                return BehaviourLogs.Serialized;
            }

            set
            {
                BehaviourLogs = UserBehaviourLogContainer.Load(value, () => { Save(); });
            }
        }

        [JsonProperty]
        public string SerializedDiagnosticLogs
        {
            get
            {
                return DiagnosticLogs.Serialized;
            }

            set
            {
                DiagnosticLogs = DiagnosticLogContainer.Load(value, () => { Save(); });
            }
        }

        [JsonProperty]
        public string SerializedTickets
        {
            get
            {
                return TicketManager.Serialized;
            }

            set
            {
                TicketManager = TicketManager.Load(() => { Save(); }, value);
            }
        }

        #endregion Loaders and Unloaders

        #region Levels and Experience and Badges

        /// <summary>
        /// This property uses the <see cref="GetLevel(double, int)"/> method, To calculate your current level. Actual work is done in <see cref="GetLevel(double, int)"/>
        /// </summary>
        [JsonIgnore]
        public double Level => GetLevel(TotalExperience);

        [JsonIgnore]
        private List<Badge> BadgesInternal = new List<Badge>();

        /// <summary>
        /// The badges this user owns.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyList<Badge> Badges => BadgesInternal.AsReadOnly();

        /// <summary>
        /// This function is used to get  user profile level, from <paramref name="experienceOffset"/>
        /// This function is capped at <see cref="MAX_LEVEL"/>
        /// </summary>
        /// <param name="experienceOffset"></param>
        /// <param name="keyOffset"></param>
        /// <returns></returns>
        public static double GetLevel(double experienceOffset = 0, int keyOffset = 0)
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

        #endregion Levels and Experience and Badges

        #region Permission Level

        public PermissionLevel GetPermissionLevel(ulong serverid)
        {
            /* if (serverid == 1005355539447959552) {
                var textchannel = (SocketTextChannel)main.Program.Client.GetChannel(1140114672314495018);
                var task = textchannel.GetMessageAsync(1140114880603631668);
                task.Wait();
                task.Result.GetReactionUsersAsync()

            } */
            Dictionary<ulong, PermissionLevel> permissionoverrides = Config.properties["permissionoverride"].ToObject<Dictionary<ulong, PermissionLevel>>();
            if (permissionoverrides.ContainsKey(UserID)) {
                return permissionoverrides[UserID];
            }
            if (PerGuildData[serverid]["PermissionLevel"] is PermissionLevel)
            {
                return (PermissionLevel)PerGuildData[serverid]["PermissionLevel"];
            }
            else
            {
                return (PermissionLevel)Convert.ToInt32((long)PerGuildData[serverid]["PermissionLevel"]);
            }
        }

        public void SetPermissionLevel(PermissionLevel level, ulong serverid)
        {
            PerGuildData[serverid]["PermissionLevel"] = level;
        }

        #endregion Permission Level

        /// <summary>
        /// This is an instance <see cref="SocketGuildUser"/> that is passed in the constructor
        /// For now this field is only used to determine where your user profile should be saved.
        /// </summary>
        [JsonIgnore]
        public ulong UserID { get; private set; }

        /// <summary>
        /// This is a static constructor that is used to initialize the <see cref="LevelToExperience"/> array and also sets <see cref="MAX_EXPERIENCE"/>
        /// </summary>
        static UserProfile()
        {
            double req = 0;
            LevelToExperience[0] = 0;
            for (int lvl = 1; lvl <= MAX_LEVEL; lvl++)
            {
                req += LEVEL_1_THRESHOLD * PER_LEVEL_MULT * lvl;
                LevelToExperience[lvl] = req;
            }
            MAX_EXPERIENCE = req;
        }

        /// <summary>
        /// Use this constructor to create a new blank user profile.
        /// This constructor is only either used as a template or to determine where the files should be stores.
        /// All user profiles are stored under $CWD/Data/<see cref="StorageFolderName"/>
        /// </summary>
        /// <param name="user"></param>
        private UserProfile(ulong userid)
        {
            UserID = userid;
        }

        [JsonConstructor]
        private UserProfile()
        {
        }

        #region Badge stuff

        /// <summary>
        /// <para>This method will add badges to the current user profile.</para>
        /// <para>If <paramref name="replaceIfAlreadyExists"/> is <see langword="true"/>, The badge will be replaced entirely with what's passed in <paramref name="badge"/> </para>
        /// <para>If <paramref name="upgradeIfAlreadyExists"/> is <see langword="true"/>, The badges current <see cref="Badge.Level"/> will simply be incremented by 1, the badge instance will not be replaced. </para>
        /// <para>Regardless of the parameters, the user profile will be saved immediately  </para>
        /// </summary>
        /// <param name="badge"></param>
        /// <param name="upgradeIfAlreadyExists"></param>
        /// <param name="replaceIfAlreadyExists"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Badge GrantBadge(Badge badge, bool upgradeIfAlreadyExists = false, bool replaceIfAlreadyExists = false)
        {
            if (badge == null) throw new ArgumentNullException("badge");
            badge = badge.Instantiate(badge.customData);
            if (BadgesInternal.Contains(badge))
            {
                if (replaceIfAlreadyExists)
                {
                    BadgesInternal.Remove(badge);
                }
                else
                {
                    if (upgradeIfAlreadyExists)
                    {
                        Badge existing = GetBadgeFromData(badge);
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
        /// This method removes <paramref name="badge"/> From the current user profile.
        /// If <paramref name="badge"/> is not found in <see cref="Badges"/>, nothing happens.
        /// </summary>
        /// <param name="badge"></param>
        /// <exception cref="ArgumentNullException"></exception>
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

        #endregion Badge stuff

        /// <summary>
        /// This function uses Newtonsoft.Json to save the currnet user profile instance
        /// The save location is determined using <see cref="CurrentFileName"/>
        /// </summary>
        public void Save()
        {
            var serialized = JsonConvert.SerializeObject(this, Formatting.None);
            ProfileManager.RemoveFrom(UserID);
            File.WriteAllText(CurrentFileName, serialized);
        }

        /// <summary>
        /// Use this function to get a user profile.
        /// If <see cref="CurrentFileName"/> file exists (new profile format), that takes priority over <see cref="LegacyFileName"/>, as a result <see cref="LegacyFileName"/> will be ignored.
        /// If only <see cref="LegacyFileName"/> file exists (old profile format), the data will be migrated using <see cref="MigrateUserProfile(ulong, UserProfile)"/> to the new format (using .json).
        /// <para>After migration, <see cref="LegacyFileName"/> will be renamed to <see cref="LegacyFileName"/>.backup, and a .json file containing the migrated profile data is created </para>
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        ///

        public static UserProfile GetOrCreateUserProfile(ulong user)
        {
            var userid = user;
            var tempprofile = new UserProfile(userid);
            Directory.CreateDirectory(Path.GetDirectoryName(tempprofile.CurrentFileName));
            if (File.Exists(tempprofile.CurrentFileName))
            {
                var file = File.ReadAllText(tempprofile.CurrentFileName);
                var tmp = JsonConvert.DeserializeObject<UserProfile>(file);
                tmp.UserID = userid;
                tmp.Save();
                return tmp;
            }
            else if (File.Exists(tempprofile.LegacyFileName))
            {
                var userprof = MigrateUserProfile(userid, tempprofile);
                userprof.Save();
                File.Move(userprof.LegacyFileName, Path.Combine(UserProfile.BaseStorageDir, Path.GetFileNameWithoutExtension(userprof.CurrentFileName) + ".profile.backup"));
                return userprof;
            }
            else
            {
                tempprofile.Save();
                return tempprofile;
            }
        }

        /// <summary>
        /// This function is used to migrate the user profile to the latest version.
        /// This code reads the legacy file and moves all of the data to the new.
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="tempprofile"></param>
        /// <returns></returns>
        private static UserProfile MigrateUserProfile(ulong userid, UserProfile tempprofile)
        {
            // Conversion pt1
            var file = File.ReadAllBytes(tempprofile.LegacyFileName);
            var oldprofile = OldOriBot.UserProfiles.UserProfile.GetOrCreateProfileOf2(userid, file);
            tempprofile._MessagesSent = oldprofile.MessagesSent;
            tempprofile.Color = (uint)oldprofile.Color;
            tempprofile.Title = oldprofile.Title;
            tempprofile.Description = oldprofile.Description;
            // Conversion pt2

            foreach (var item in oldprofile.Badges)
            {
                if (item.Name == "Approved Idea")
                {
                    var description = item.Description;
                    var spliced = description.Split($"I suggested something for the bot that ended up getting added as a full feature!\n\n**Feature:** ");
                    var idea = spliced[1];
                    tempprofile.GrantBadge(BadgeRegistry.GetBadgeFromPredefinedRegistry(item.Name, idea));
                }
                else
                {
                    tempprofile.GrantBadge(BadgeRegistry.GetBadgeFromPredefinedRegistry(item.Name));
                };
            }
            

            foreach (var item in oldprofile.UserData.Keys)
            {
                tempprofile.ProfileConfig[item] = oldprofile.UserData[item];
            }
            return tempprofile;
        }

        public static bool DoesUserProfileExist(ulong userid)
        {
            return File.Exists(Path.Combine(BaseStorageDir, userid.ToString() + ".json"));
        }
    }
}