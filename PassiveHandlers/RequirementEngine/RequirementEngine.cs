using System;
using System.Collections.Generic;
using Discord.WebSocket;

namespace OriBot.PassiveHandlers
{
    public class Requirements
    {
        private List<Func<DiscordSocketClient, SocketMessage, bool>> _requirement = new();

        public bool CheckRequirements(DiscordSocketClient client, SocketMessage message)
        {
            foreach (var requirement in _requirement)
            {
                if (!requirement(client, message))
                {
                    return false;
                }
            }

            return true;
        }

        public void AddRequirement(Func<DiscordSocketClient, SocketMessage, bool> requirement)
        {
            _requirement.Add(requirement);
        }

        public void ClearRequirements()
        {
            _requirement.Clear();
        }

        public void RemoveRequirement(Func<DiscordSocketClient, SocketMessage, bool> requirement)
        {
            _requirement.Remove(requirement);
        }

        public Requirements(params Func<DiscordSocketClient, SocketMessage, bool>[] requirements)
        {
            foreach (var requirement in requirements)
            {
                _requirement.Add(requirement);
            }
        }

    }

}