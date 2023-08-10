using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace OriBot.Framework.UserProfiles.PerGuildData
{
    public class PerGuildDataContainer
    {

        private static Dictionary<ulong, PerGuildData> DefaultData { get; } = new();

        private Action saveAction { get; set; }

        private Dictionary<ulong, PerGuildData> _Config { get; set; } = new(DefaultData);

        public IReadOnlyDictionary<ulong, PerGuildData> Config
        {
            get { return new Dictionary<ulong, PerGuildData>(_Config); }
            set { 
                _Config = new Dictionary<ulong, PerGuildData>(value); 
                saveAction();
            }
        }

        private PerGuildDataContainer()
        {
        }

        /// <summary>
        /// Use this accessor to write or read data by key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public PerGuildData this[ulong key]
        {
            get
            {
                if (!_Config.ContainsKey(key))
                {
                    var tmp = PerGuildData.Load(null, saveAction);
                    this[key] = tmp;
                }
                return _Config[key];
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
        public static PerGuildDataContainer Load(string encodedjson, Action savefunction)
        {
            if (encodedjson == null)
            {
                return new PerGuildDataContainer() { saveAction = savefunction };
            }
            var tmp = new PerGuildDataContainer();
            tmp.saveAction = () => { };
            var tmp2 = JsonConvert.DeserializeObject<Dictionary<ulong, string>>(encodedjson);
            foreach (var item in tmp2)
            {
                tmp[item.Key] = PerGuildData.Load(item.Value, savefunction);
            }
            tmp.saveAction = savefunction;
            
            return tmp;
        }

        public string Serialized
        {
            get
            {
                Dictionary<ulong, string> serialized = new();
                foreach (var item in _Config)
                {
                    serialized[item.Key] = JsonConvert.SerializeObject(item.Value.Config, Formatting.None);
                }
                return JsonConvert.SerializeObject(serialized, Formatting.None);
            }
        }
    }
}