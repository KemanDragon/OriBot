using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace OriBot.Framework.UserProfiles
{
    public class TicketManager {

        [JsonProperty]
        private List<ulong> TicketChannelID = new();

        [JsonProperty]
        private Dictionary<ulong, List<ulong>> GuildIDtoTicketChannelIDs = new();

        [JsonIgnore]
        private Action saveAction = new Action(() => {});

        public TicketManager(Action saveAction)
        {

        }

        [JsonConstructor]
        private TicketManager() { }

        public bool CanOpenTicket(ulong guildid) {
            if (!GuildIDtoTicketChannelIDs.ContainsKey(guildid)) {
                return true;
            }
            var list = GuildIDtoTicketChannelIDs[guildid];
            var guild = main.Program.Client.GetGuild(guildid);
            var filtered = !guild.ThreadChannels
            .Where(x => list.Contains(x.Id))
            .Any(x => !x.IsLocked);
            return filtered;
        }

        public SocketThreadChannel GetLatestOpenTicket(ulong guildid) {
            var list = GuildIDtoTicketChannelIDs[guildid];
            var guild = main.Program.Client.GetGuild(guildid);
            return guild.ThreadChannels
            .Where(x => list.Contains(x.Id))
            .FirstOrDefault(x => !x.IsLocked, null);
        }

        public bool CanOpenTicket(SocketGuild guild) {
            return CanOpenTicket(guild.Id);
        }

        public void SetTicketChannel(ulong guildid, ulong channelid) 
        {
            if (!GuildIDtoTicketChannelIDs.ContainsKey(guildid)) {
                GuildIDtoTicketChannelIDs.Add(guildid, new List<ulong>());
            }
            if (GuildIDtoTicketChannelIDs[guildid].Contains(channelid)) {
                return;
            }
            GuildIDtoTicketChannelIDs[guildid].Add(channelid);
            saveAction();
            return;
        }

        public static TicketManager Load(Action saveAction, string encoded)
        {
            if (encoded == null)
            {
                return new TicketManager(saveAction);
            }
            var manager = JsonConvert.DeserializeObject<TicketManager>(encoded);
            manager.saveAction = saveAction;
            return manager;
        }

        [JsonIgnore]
        public string Serialized => JsonConvert.SerializeObject(this);
    }
}