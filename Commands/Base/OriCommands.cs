using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using OriBot.Commands.RequirementEngine;
using OriBot.Framework;
using OriBot.Framework.UserProfiles;
using OriBot.Utilities;

namespace OriBot.Commands
{
    /// <summary>
    /// <see cref="OricordCommand"/> is a class that is used to implement all slash commands across Oricord.
    /// By default the commands registered in the implementations of this class will only run on Oricord servers.
    /// Override <see cref="GetRequirements"/> to change what conditions need to be met in order for slash commands registered in this class to run.
    /// <see cref="OricordCommand"/> implementations also contain a <see cref="DataContext"/> property to lets you access the global shared <see cref="OricordContext"/> across all <see cref="OricordCommand"/> implementations and all <see cref="OriBot.PassiveHandlers.OricordPassiveHandler"/> implementations.
    /// </summary>
    [Requirements(typeof(OricordCommand))]
    public class OricordCommand : BaseCommand
    {
        /// <summary>
        /// This property returns a singleton instance of <see cref="OricordContext"/> that is shared across implementations of <see cref="OriBot.PassiveHandlers.OricordPassiveHandler"/> and all <see cref="OriBot.Commands.OricordCommand"/> implemenations
        /// </summary>
        public OricordContext DataContext
        {
            get
            {
                return (OricordContext)main.Memory.ContextStorage["oricord"];
            }
        }

        /// <summary>
        /// <see cref="BaseCommand.GetRequirements"/> Is overridden here, because <see cref="OricordCommand"/> implementations are designed to run on slash commands that come from Oricord servers.
        /// <para>You may override this method in your own implementations by adding the <see langword="override"/> keyword , but please make sure that you still check that the slash commands only comes from Oricord servers to ensure that the context of the messages are what you think they are. </para>
        /// </summary>
        /// <returns></returns>
        public override Requirements GetRequirements()
        {
            return new Requirements((context, commandinfo, services) =>
            {
                
                return Config.properties["oricordServers"].Contains(context.Guild.Id);
            });
        }
    }
}