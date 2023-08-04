using Discord.WebSocket;

using OriBot.PassiveHandlers.RequirementEngine;

namespace OriBot.PassiveHandlers
{
    public interface IRequirementCheck
    {
        public Requirements GetRequirements();
    }

    /// <summary>
    /// Passive handlers are like commands that always run on every single message,
    /// passive handlers have no arguments because they are not commands Where each command has a set of arguments that it needs to take in in order to run properly.
    ///
    /// <para>
    /// <see cref="BasePassiveHandler"/> is used as a base for all of the passive handlers that you will write in OriBot.
    /// Inside of a passive handler there is a <see cref="client"/> field and a <see cref="message"/> field, Which you read in your method(s) in order to determine the message.
    /// </para>
    /// <para>
    /// Passive handlers also have a <see cref="GetRequirements"/> method in order to determine whether the passive handlers should run based on the current circumstances.
    /// When any message in any server is sent, if <see cref="Requirements.CheckRequirements(DiscordSocketClient, SocketMessage)"/> returns true, then all methods in this class that are marked with the <see cref="PassiveHandler"/> attribute will be executed.
    /// </para>
    /// </summary>
    public abstract class BasePassiveHandler : IRequirementCheck
    {
        internal readonly DiscordSocketClient client;

        internal readonly SocketMessage message;

        /// <summary>
        /// You are not meant to directly construct passive handlers, because all passive handlers are automatically registered by <see cref="PassiveHandlerHub"/>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        internal BasePassiveHandler(DiscordSocketClient client, SocketMessage message)
        {
            this.client = client;
            this.message = message;
        }

        /// <summary>
        /// This method will return a new <see cref="Requirements"/> instance for this passive handler
        /// </summary>
        /// <returns></returns>
        public abstract Requirements GetRequirements();
    }
}