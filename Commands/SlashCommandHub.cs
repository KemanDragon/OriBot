using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using OriBot.Framework;

namespace OriBot.Commands
{
    public class RequireCorrectServerAttribute : PreconditionAttribute
    {
        private ulong[] ID = { };

        public RequireCorrectServerAttribute(params ulong[] id)
        {
            ID = id;
        }

        public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {

            if (ID.Contains(context.Guild.Id))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }
            else
            {
                return Task.FromResult(PreconditionResult.FromError("You are not in the correct server"));
            }
        }
    }
}