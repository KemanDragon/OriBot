using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace OriBot.Framework.UserProfiles.ProfileConfig
{
    

    /// <summary>
    /// This class represents a user profile's configuration.
    /// This class uses a dictionary to store user configuration.
    /// Default values are found in: <see cref="ProfileConfigs.DefaultProfileConfigs"/>
    /// </summary>
    public class ProfileConfigs
    {
        private static Dictionary<string, object> DefaultProfileConfigs { get; } = new Dictionary<string, object>()
        {
            ["DefaultDenyPins"] = false,
            ["DMWhenArtPinned"] = false,
            ["HasMadeFirstGalleryPost"] = false,
            ["HasSeenEmojiSubmissionReminder"] = false,
            ["PingOnReply"] = true,
        };

        private Action saveAction { get; set; }

        private Dictionary<string, object> _Config { get; set; } = DefaultProfileConfigs.ToDictionary(entry => entry.Key, entry => entry.Value);

        public IReadOnlyDictionary<string, object> Config
        {
            get { return _Config.ToDictionary(entry => entry.Key, entry => entry.Value); }
            set { 
                _Config = value.ToDictionary(entry => entry.Key, entry => entry.Value);
                saveAction();
            }
        }

        private ProfileConfigs()
        {
        }

        /// <summary>
        /// Use this accessor to write or read data by key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object this[string key]
        {
            get
            {
                return _Config[key];
            }
            set
            {
                _Config[key] = value;
                saveAction();
            }
        }

        /// <summary>
        /// This will load a <see cref="ProfileConfigs"/>by encoded JSON in the <paramref name="encodedjson"/> parameter.
        /// The function / method / lamda in <paramref name="savefunction"/> will be executed upon any writes to the data. (This is used to automatically save the <see cref="UserProfile"/> along with its <see cref="ProfileConfigs"/> upon writing to the data) 
        /// </summary>
        /// <param name="encodedjson"></param>
        /// <param name="savefunction"></param>
        /// <returns></returns>
        public static ProfileConfigs Load(string encodedjson, Action savefunction)
        {
            if (encodedjson == null)
            {
                return new ProfileConfigs() { saveAction = savefunction };
            }
            var tmp = new ProfileConfigs()
            {
                saveAction = savefunction
            };
            tmp._Config = JsonConvert.DeserializeObject<Dictionary<string, object>>(encodedjson);
            return tmp;
        }
    }
}