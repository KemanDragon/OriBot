using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using main;
using Newtonsoft.Json;
using OriBot.Commands;

namespace OriBot.Framework.UserProfiles.SaveableTimer
{
    public class MuteTimer : SaveableTimer
    {
        [JsonProperty]
        public override string Name { get; protected set; } = "mutetimer";

        [JsonProperty]
        private ulong GuildID = 0;

        [JsonProperty]
        private ulong UserID = 0;

        public MuteTimer(string uid, DateTime endtime, bool started, ulong guildid, ulong userid) : base(endtime, uid, started)
        {
            GuildID = guildid;
            UserID = userid;
        }
        [JsonConstructor]
        public MuteTimer() : base()
        {
        }

        public override SaveableTimer Load(string jsonstring)
        {
            var loaded = JsonConvert.DeserializeObject<MuteTimer>(jsonstring);
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
            var tmp = new MuteTimer(InstanceUID, tmp2, Started || autostart, GuildID, UserID);
            tmp._template = false;
            if (autostart || Started)
            {
                tmp.Start();
            }
            return tmp;
        }

        public void SetData(ulong guildid, ulong userid) {
            GuildID = guildid;
            UserID = userid;
        }

    public override string Format()
        {
            return $"";
        }

        public override void OnTarget()
        {
            var guild = main.Program.Client.GetGuild(GuildID);
            
            var mutedrole = guild.Roles.Where(x => x.Name == ModerationModule.MutedRoleName).FirstOrDefault();
            var normalrole = guild.Roles.Where(x => x.Name == ModerationModule.NormalRoleName).FirstOrDefault();
            
            if (guild.GetUser(UserID) is null) {
                return;
            }
            guild.GetUser(UserID).RemoveRoleAsync(mutedrole).Wait();
            guild.GetUser(UserID).AddRoleAsync(normalrole).Wait();
            var userprofile = UserProfile.GetOrCreateUserProfile(UserID);
            try {
                guild.GetUser(UserID).SendMessageAsync("You have been unmuted.").Wait();
            } catch (Exception) {}
            userprofile.MutedTimerID = "";
            GlobalTimerStorage.GetTimerByID(InstanceUID, true).Stop();
            
        }
    }
}