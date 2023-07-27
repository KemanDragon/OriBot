using System;
using System.Collections.Generic;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace OriBot.Commands.RequirementEngine
{
    public class Requirements
    {
        private List<Func<IInteractionContext, ICommandInfo, IServiceProvider, bool>> _requirement = new();

        public bool CheckRequirements(IInteractionContext context, ICommandInfo message, IServiceProvider services)
        {
            foreach (var requirement in _requirement)
            {
                if (!requirement(context, message, services))
                {
                    return false;
                }
            }
            return true;
        }

        public void AddRequirement(Func<IInteractionContext, ICommandInfo, IServiceProvider, bool> requirement)
        {
            _requirement.Add(requirement);
        }

        public void ClearRequirements()
        {
            _requirement.Clear();
        }

        public void RemoveRequirement(Func<IInteractionContext, ICommandInfo, IServiceProvider, bool> requirement)
        {
            _requirement.Remove(requirement);
        }

        public Requirements(params Func<IInteractionContext, ICommandInfo, IServiceProvider, bool>[] requirements)
        {
            foreach (var requirement in requirements)
            {
                _requirement.Add(requirement);
            }
        }

    }

}