using System;
using System.Collections.Generic;

using Discord.WebSocket;

namespace OriBot.PassiveHandlers.RequirementEngine
{
    /// <summary>
    /// <see cref="Requirements"/> Is a class that is used in the commands and passive handler system, to check whether the requirements for the commands / passive handler is meant in order to run.
    /// <para>The way that <see cref="Requirements"/> works is By checking all of the functions that is stored in <see cref="_requirement"/> and running them all to make sure that all of them results in true, If all functions return true then the requirement is met but if one function returns false and the entire requirement is considered not met.</para>
    /// </summary>
    public class Requirements
    {
        private List<Func<DiscordSocketClient, SocketMessage, bool>> _requirement = new();

        /// <summary>
        /// Execute this method with the current <see cref="DiscordSocketClient"/> and the current <see cref="SocketMessage"/> that's being processed to check whether the client and message fufils this requirement.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Use this function to add another condition to the current <see cref="Requirements"/> object.
        /// If you're not planning to add anymore conditions after instantiation / construction in your command / passive handler , then please add conditions using the constructor instead.
        /// </summary>
        /// <param name="requirement"></param>
        public void AddRequirement(Func<DiscordSocketClient, SocketMessage, bool> requirement)
        {
            _requirement.Add(requirement);
        }

        /// <summary>
        /// Use this function to clear all of the conditions in the current <see cref="Requirements"/> object.
        /// If there are no conditions in a <see cref="Requirements"/> object then <see cref="CheckRequirements(DiscordSocketClient, SocketMessage)"/> will always return true.
        /// </summary>
        public void ClearRequirements()
        {
            _requirement.Clear();
        }

        /// <summary>
        /// Remove a specific condition by its item in this <see cref="Requirements"/> object.
        /// </summary>
        /// <param name="requirement"></param>
        public void RemoveRequirement(Func<DiscordSocketClient, SocketMessage, bool> requirement)
        {
            _requirement.Remove(requirement);
        }

        /// <summary>
        /// Create a new <see cref="Requirements"/ object with the following conditions in its arguments.
        /// You may pass in the conditions just like parameters in a function.
        /// </summary>
        /// <param name="requirements"></param>
        public Requirements(params Func<DiscordSocketClient, SocketMessage, bool>[] requirements)
        {
            foreach (var requirement in requirements)
            {
                _requirement.Add(requirement);
            }
        }
    }
}