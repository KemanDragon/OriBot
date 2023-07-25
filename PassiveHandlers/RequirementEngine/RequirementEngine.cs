using System;
using System.Collections.Generic;
using Discord.WebSocket;

namespace OriBot.PassiveHandlers
{
    public class Requirements
    {
        private List<Func<DiscordSocketClient, SocketMessage, EventType, bool>> _requirement = new();

        public bool CheckRequirements(DiscordSocketClient client, SocketMessage message, EventType type)
        {
            foreach (var requirement in _requirement)
            {
                if (!requirement(client, message, type))
                {
                    return false;
                }
            }
            return true;
        }

        public void AddRequirement(Func<DiscordSocketClient, SocketMessage, EventType, bool> requirement)
        {
            _requirement.Add(requirement);
        }

        public void ClearRequirements()
        {
            _requirement.Clear();
        }

        public void RemoveRequirement(Func<DiscordSocketClient, SocketMessage, EventType, bool> requirement)
        {
            _requirement.Remove(requirement);
        }

        public Requirements(params Func<DiscordSocketClient, SocketMessage, EventType, bool>[] requirements)
        {
            foreach (var requirement in requirements)
            {
                _requirement.Add(requirement);
            }
        }

    }

}