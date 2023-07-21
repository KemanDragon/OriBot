using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace OriBot.Framework
{
    public class Attributes
    {
        public class RequireCorrectServerAttribute : PreconditionAttribute
        {
            private ulong[] ID = { };

            public RequireCorrectServerAttribute(params ulong[] id)
            {
                ID = id;
            }

            public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context2, CommandInfo command, IServiceProvider services)
            {
                
                if (ID.Contains(context2.Guild.Id))
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                } else {
                    return Task.FromResult(PreconditionResult.FromError("You are not in the correct server"));
                }
            }
        }
    }
}
