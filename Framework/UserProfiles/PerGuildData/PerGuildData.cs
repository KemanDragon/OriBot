using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using OriBot.Framework.UserProfiles.ProfileConfig;

namespace OriBot.Framework.UserProfiles.PerGuildData
{
    public class PerGuildData
    {
        private static Dictionary<string, object> DefaultData { get; } = new Dictionary<string, object>()
        {
            ["PermissionLevel"] = PermissionLevel.NewUser
        };

        private Action saveAction { get; set; }

        private Dictionary<string, object> _Config { get; set; } = new(DefaultData);

        public IReadOnlyDictionary<string, object> Config
        {
            get { 
                return new Dictionary<string,object>(_Config);
            }
            set {
                _Config = new Dictionary<string, object>(value);
                saveAction();
            }
        }

        private PerGuildData()
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
                if (!_Config.ContainsKey(key))
                {
                    return null;
                }
                return Config[key];
            }
            set
            {
                _Config[key] = value;
                saveAction();
            }
        }

        /// <summary>
        /// This will load a <see cref="PerGuildDataContainer"/> by encoded JSON in the <paramref name="encodedjson"/> parameter.
        /// The function / method / lamda in <paramref name="savefunction"/> will be executed upon any writes to the data. (This is used to automatically save the <see cref="UserProfile"/> along with its <see cref="PerGuildDataContainer"/> upon writing to the data) 
        /// </summary>
        /// <param name="encodedjson"></param>
        /// <param name="savefunction"></param>
        /// <returns></returns>
        public static PerGuildData Load(string encodedjson, Action savefunction)
        {
            if (encodedjson == null)
            {
                return new PerGuildData() { saveAction = savefunction };
            }
            var tmp = new PerGuildData()
            {
                saveAction = savefunction
            };
            var tmp2 = JsonConvert.DeserializeObject<Dictionary<string, object>>(encodedjson);
            foreach (var item in tmp2)
            {
                tmp._Config[item.Key] = item.Value;
            }
            return tmp;
        }
    }
}
