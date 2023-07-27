using System;

using Discord.WebSocket;

using OriBot.Framework;
using OriBot.PassiveHandlers.RequirementEngine;

namespace OriBot.PassiveHandlers
{
    /// <summary>
    /// <see cref="OricordPassiveHandler"/> Is a passive handler that inherits from <see cref="BasePassiveHandler"/>, which is configured to only execute its methods for messages that come from Oricord
    /// </summary>
    public abstract class OricordPassiveHandler : BasePassiveHandler
    {
        public OricordPassiveHandler(DiscordSocketClient client, SocketMessage message) : base(client, message)
        {
        }

        /// <summary>
        /// This property returns a singleton instance of <see cref="OricordContext"/> that is shared across implementations of <see cref="OricordPassiveHandler"/> and all <see cref="OriBot.Commands.OricordCommand"/> implemenations
        /// </summary>
        public OricordContext DataContext
        {
            get
            {
                return (OricordContext)main.Memory.ContextStorage["oricord"];
            }
        }

        /// <summary>
        /// <see cref="BasePassiveHandler.GetRequirements"/> Is overridden here, because <see cref="OricordPassiveHandler"/> implementations are designed to run on messages that come only from the Oricord servers 
        /// <para>You may override this method by adding the <see langword="override"/> keyword , but please make sure that you still check that the messages only comes from Oricord servers to ensure that the context of the messages are what you think they are. </para>
        /// </summary>
        /// <returns></returns>
        public override Requirements GetRequirements()
        {
            return new Requirements(
                (client, messageParam) =>
                {
                    var message = messageParam as SocketUserMessage;
                    if (message == null) return false;
                    var user = message.Author as SocketGuildUser;
                    if (user == null) return false;
                    if (user.Guild.Id != 1005355539447959552) return false;
                    return true;
                }
            );
        }
    }
}