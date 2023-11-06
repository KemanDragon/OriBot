using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;

using Discord.WebSocket;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;

namespace OriBot.Framework.UserProfiles.SaveableTimer
{
    public class SaveableTimer {

        [JsonProperty]
        public string InstanceUID { get; protected set; } = Guid.NewGuid().ToString();

        [JsonProperty] 
        public virtual string Name { get; protected set; } = "default";

        [JsonProperty]
        public bool Started { get; protected set; } = false;

        [JsonProperty]
        protected DateTime _target = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime Target {
            get => _target; set
            {
                if (Started) {
                    throw new Exception("Cannot change target of started timer");
                } else {
                    _target = value;
                }
            }
        }

        protected Timer timer;

        public virtual SaveableTimer Instantiate(bool autostart = true, DateTime dateTime = default)
        {
            throw new NotImplementedException();
        }

        public virtual SaveableTimer Load(string jsonstring)
        {
            throw new NotImplementedException();
        }

        public virtual string Save()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }

        public virtual string Format()
        {
            throw new NotImplementedException();
        }

        public virtual void Start()
        {
            timer = new Timer(Math.Max((Target - DateTime.UtcNow).TotalMilliseconds,1));
            timer.Elapsed += (sender, args) =>
            {
                OnTarget();
            };
            timer.AutoReset = false;
            Started = true;
            timer.Start();
        }

        public virtual void Stop(bool permanent = false)
        {
            timer.Stop();
            if (permanent)
            {
                Started = false;
            }
        }

        [JsonProperty]
        public bool IsTemplate
        {
            get { return _template; }
        }

        [JsonIgnore]
        protected bool _template = true;

        protected SaveableTimer(DateTime target, string uid = "", bool started = false)
        {
            Target = target;
            InstanceUID = uid;
            Started = started;
        
        }

        [JsonConstructor]
        protected SaveableTimer()
        {
        }

        public virtual void OnTarget() {
            throw new NotImplementedException();
        }
    }

    public class SaveableTimerRegistry {
        private static List<SaveableTimer> _timers = new();

        private static List<SaveableTimer> TimerCache = new() { 
            new ExampleTimer(),
            new MuteTimer(),
        };

        public static T CreateTimer<T>(DateTime target, bool autostart = true) where T : SaveableTimer
        {
            T timer = Activator.CreateInstance<T>();
            foreach (SaveableTimer item in TimerCache)
            {
                if (timer.Name == item.Name)
                {
                    var tmp = item.Instantiate(autostart, target);
                    return (T)tmp;
                }
            }
            return null;
        }

        public static SaveableTimer LoadTimerFromString(string data)
        {
            var tmp = JsonConvert.DeserializeObject<SaveableTimer>(data);
            foreach (SaveableTimer log in TimerCache)
            {
                if (log.Name.ToLower() == tmp.Name.ToLower())
                {
                    return log.Load(data);
                }
            }
            return null;
        }
    }
}