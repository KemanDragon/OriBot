using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord.WebSocket;

namespace OriBot.EventHandlers.Base
{
    /// <summary>
    /// <see cref="BaseEventHandler"/> is the base for all event handlers that you will write in OriBot.
    /// The event handler system is designed to handle events that fall outside of slash commands and sent messages.
    /// You are expected to handle events such as reaction being added to a message (<see cref="DiscordSocketClient.ReactionAdded"/>), a message being edited (<see cref="DiscordSocketClient.MessageUpdated"/>), a message being deleted (<see cref="DiscordSocketClient.MessageDeleted"/>) and other such events using <see cref="BaseEventHandler"/> and the event handler system.
    ///
    /// <para>
    /// To make an event handler, simply inherit this class and implement <see cref="RegisterEventHandler(DiscordSocketClient)"/> to register any event handler that you would like to the <see cref="DiscordSocketClient"/> using the += syntax in C#
    /// </para>
    ///
    /// </summary>
    public abstract class BaseEventHandler
    {
        public BaseEventHandler()
        { }

        /// <summary>
        /// To create an event handler , simply inherit this class and implement <see cref="RegisterEventHandler(DiscordSocketClient)"/> to register any events that you would like onto <paramref name="client"/>
        /// </summary>
        /// <param name="client"></param>
        public abstract void RegisterEventHandler(DiscordSocketClient client);
    }
}