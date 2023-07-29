using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using OriBot.Commands.RequirementEngine;
using OriBot.Framework;
using OriBot.Utilities;

namespace OriBot.Commands
{
    public class RequirementsAttribute : PreconditionAttribute
    {
        public Type classtarget;

        public RequirementsAttribute(Type requirementsengine)
        {
            classtarget = requirementsengine;
        }

        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            var tmp = Activator.CreateInstance(classtarget);
            if (!(tmp is IRequirementCheck engine))
            {
                Logger.Error($"PLEASE FIX: {tmp.GetType().Name} does not implement IPermissionCheck.");
                return Task.FromResult(PreconditionResult.FromError("PLEASE FIX: " + tmp.GetType().Name + " does not implement IPermissionCheck."));
            } else {
                if (engine.GetRequirements().CheckRequirements(context,commandInfo,services)) {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                } else {
                    return Task.FromResult(PreconditionResult.FromError("You do not meet the requirements"));
                }
            }
            
        }
    }
}