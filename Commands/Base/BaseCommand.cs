using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord.Interactions;

using OriBot.Commands.RequirementEngine;
using OriBot.Framework;

namespace OriBot.Commands
{
    public interface IRequirementCheck
    {
        public Requirements GetRequirements();
    }

    /// <summary>
    /// <see cref="BaseCommand"/> is a class that is used to implement all slash commands across all of OriBot
    /// <para>
    /// <see cref="BaseCommand"/> is also used as a base for all of the slash commands that you will write in OriBot.
    /// </para>
    /// <para>
    /// Slash commands also have a <see cref="GetRequirements"/> method in order to determine whether the slash commands should run based on the current circumstances.
    /// During startup Discord.NET will search for any methods containing the <see cref="SlashCommandAttribute"/> in any classes that inherit from <see cref="InteractionModuleBase"/>, And then our code will register them to the global application scope.
    /// When a registered slash command is run, we will first check whether this slash command is meant to run in this context by creating a new <see cref="Requirements"/> instance using <see cref="GetRequirements"/> and then running <see cref="Requirements.CheckRequirements(Discord.IInteractionContext, ICommandInfo, IServiceProvider)"/> to check whether this slash command is meant to run in this current context.
    /// </para>
    /// <para>
    /// Classes implementing <see cref="InteractionModuleBase"/> (which includes <see cref="BaseCommand"/>) can actually do a lot more than just implementing slash commands, please see <see href="https://discordnet.dev/guides/int_framework/intro.html#slash-command"/> and <see href="https://discordnet.dev/guides/int_framework/intro.html#commands"/> For more detailed look at what <see cref="InteractionModuleBase"/> is capable of.
    /// </para>
    /// </summary>
    [Requirements(typeof(BaseCommand))]
    public abstract class BaseCommand : InteractionModuleBase, IRequirementCheck
    {
        /// <summary>
        /// This method will return a new <see cref="Requirements"/> instance for this command.
        /// </summary>
        /// <returns></returns>
        public virtual Requirements GetRequirements()
        {
            return new Requirements();
        }
    }
}