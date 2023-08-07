using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Timers;

using Discord.WebSocket;
using Newtonsoft.Json;

namespace OriBot.Framework.UserProfiles.SaveableTimer
{
    public class ExampleTimer : SaveableTimer {
        [JsonProperty]
        public override string Name { get; protected set; } = "exampletimer";

        public ExampleTimer(string uid, DateTime endtime, bool started) : base(endtime, uid, started) {

        }

        [JsonConstructor]
        
        public ExampleTimer() : base() {

        }

        public override SaveableTimer Load(string jsonstring)
        {
            var loaded = JsonConvert.DeserializeObject<ExampleTimer>(jsonstring);
            if (loaded != null)
            {
                loaded._template = false;
                if (loaded.Started)
                {
                    loaded.Start();
                }
                return loaded;
            }
            return null;
        }

        public override SaveableTimer Instantiate(bool autostart = true, DateTime target = default)
        {
            DateTime tmp2 = target;
            if (target == default)
            {
                tmp2 = Target;
            }
            var tmp = new ExampleTimer(InstanceUID, tmp2, Started || autostart);
            tmp._template = false;
            if (autostart || Started) {
                tmp.Start();
            }
            return tmp;
        }

        public override string Format()
        {
            return $"";
        }

        public override void OnTarget()
        {
            Console.WriteLine("ExampleTimer.OnTarget()");
        }
    }
}