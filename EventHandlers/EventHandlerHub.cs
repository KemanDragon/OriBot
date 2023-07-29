using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Discord.WebSocket;

using OriBot.EventHandlers.Base;
using OriBot.PassiveHandlers;

namespace OriBot.EventHandlers
{
    /// <summary>
    /// <see cref="EventHandlerHub"/> Is the class that is responsible for registering all <see cref="BaseEventHandler"/> implementations to the OriBot.
    /// </summary>
    public static class EventHandlerHub
    {
        private static readonly List<Type> _eventHandlers = new();

        /// <summary>
        /// This method will register all <see cref="BaseEventHandler"/> implementations to the OriBot.
        /// </summary>
        /// <param name="client"></param>
        public static void RegisterEventHandlers(DiscordSocketClient client)
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(BaseEventHandler)) && !type.IsAbstract)
                {
                    _eventHandlers.Add(type);
                    var eventhandler = (BaseEventHandler)Activator.CreateInstance(type);
                    eventhandler.RegisterEventHandler(client);
                }
            }
        }
    }
}