using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using EtiBotCore.Data.Structs;
using OriBot.Commands;
using OriBot.Commands.RequirementEngine;
// using OriBot.Framework;
using OriBot.Framework.UserProfiles;
using GroupAttribute = Discord.Interactions.GroupAttribute;

[Requirements(typeof(OricordCommand))]
[Discord.Commands.Group("set")]
public class ProfileSet : ModuleBase<SocketCommandContext>
{
    [Command]
    public async Task Set()
    {
        await this.ReplyAsync("hello Worl");
    }

    [Command("sub")]
    public async Task Set2()
    {
        await this.ReplyAsync("hello Worl");
    }

}

namespace OriBot.Commands
{
    [Requirements(typeof(ProfileModule))]
    [Group("profile", "Profile Commands")]
    public class ProfileModule : OricordCommand
    {
        private Requirements IsUserAMod => new Requirements((context, commandinfo, services) =>
        {
            if (context.Interaction.IsDMInteraction)
            {
                _ = Context.Interaction.RespondAsync("Sorry, please execute this command on a guild.");
                return false;
            }
            else if (ProfileManager.GetUserProfile(context.User.Id).GetPermissionLevel(context.Guild.Id) >= PermissionLevel.Moderator)
            {
                return true;
            }
            return false;
        });


        private string AlwaysString(string target) {
            if (target.Trim() == "")
            {
                return "No value";
            }
            return target;
        }

        [AutocompleteCommand("setting", "settings")]
        public async Task Autocomplete()
        {
            var possiblekeys = new List<AutocompleteResult>() {
                new AutocompleteResult("title (text)", "title"),
                new AutocompleteResult("description (text)", "description"),
                new AutocompleteResult("color (hexcode)", "color"),
                };
            foreach (var item in ProfileManager.GetUserProfile(Context.User.Id).ProfileConfig.Config)
            {
                possiblekeys.Add(new AutocompleteResult($"{item.Key} ({ComplexTypeNameToSimple(item.Value.GetType().Name)})", item.Key));
            }
            string userInput = (Context.Interaction as SocketAutocompleteInteraction).Data.Current.Value.ToString();
            IEnumerable<AutocompleteResult> results = possiblekeys.Where(x => x.Name.StartsWith(userInput, StringComparison.InvariantCultureIgnoreCase));


            // max - 25 suggestions at a time
            await (Context.Interaction as SocketAutocompleteInteraction).RespondAsync(results.Take(25));
        }

        public static ulong GetIdFromString(string mentionorid) {
            if (mentionorid == null)
            {
                return 0;
            }
            if (mentionorid.StartsWith("<@") && mentionorid.EndsWith(">"))
            {
                try
                {
                    return ulong.Parse(Regex.Replace(mentionorid, "[^0-9]",""));
                } catch (Exception)
                {
                    return 0;
                }
                
            }
            else
            {
                try
                {
                    return ulong.Parse(mentionorid);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public static string ComplexTypeNameToSimple(string name) {
            switch (name)
            {
                case "String":
                    return "text";
                case "Int32":
                    return "whole number";
                case "Boolean":
                    return "true/false";
                case "Int64":
                    return "whole number";
                case "UInt32":
                    return "whole number, positive only";
                case "UInt64":
                    return "whole number, positive only";
                default:
                    return name;
            }
        }

        public static object StringToType(string value, Type type) {
            if (type == typeof(int)) {
                return int.Parse(value);
            } else if (type == typeof(uint)) {
                return uint.Parse(value);
            } else if (type == typeof(long)) {
                return long.Parse(value);
            } else if (type == typeof(ulong)) {
                return ulong.Parse(value);
            } else if (type == typeof(float)) {
                return float.Parse(value);
            } else if (type == typeof(double)) {
                return double.Parse(value);
            } else if (type == typeof(bool)) {
                return bool.Parse(value);
            } else if (type == typeof(string)) {
                return value;
            } else {
                return null;
            }
        }

        private string FormatLevel(int level) {
            if (level > 0) {
                return $"__**Lv. {level}**__";
            } else {
                return "";
            }
            
        }

        private string FormatLevel2(int level)
        {
            if (level > 0)
            {
                return $"Level {level}";
            }
            else
            {
                return "";
            }

        }

        [SlashCommand("settings", "Configures or views your profile settings.")]
        public async Task Settings([Discord.Interactions.Summary("setting"), Autocomplete] string setting = null, string value = null, string User = null) {
            try {
                if (setting == null)
                {
                    // Viewing
                    var userprofile = ProfileManager.GetUserProfile(Context.User.Id);
                    var embed = new EmbedBuilder();
                    SocketGuildUser discorduser = null;
                    if (User != null)
                    {
                        ulong? user = GetIdFromString(User);
                        if (!UserProfile.DoesUserProfileExist(user.Value) && (await Context.Guild.GetUserAsync(user.Value)) is null)
                        {
                            await RespondAsync($"Sorry, i could not find a user with ID {User}");
                            await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                                new CommandSuccessLogEntry(Context.User.Id, "profile settings", DateTime.UtcNow, Context.Guild as SocketGuild)
                                .WithAdditonalField("Additional remarks", "Sorry, i could not find a user with ID {User}")
                            );
                            return;
                        }
                        userprofile = ProfileManager.GetUserProfile(user.Value);
                    }
                    if (await Context.Guild.GetUserAsync(userprofile.UserID) is SocketGuildUser discorduser2 && discorduser2 is not null)
                    {
                        embed.WithAuthor(discorduser2);
                        discorduser = discorduser2;
                    }
                    embed.WithTitle("Profile configuration for this user.");
                    embed.AddField("Profile color", userprofile.Color);
                    embed.AddField("Profile description", AlwaysString(userprofile.Description));
                    embed.AddField("Profile title", AlwaysString(userprofile.Title));
                    foreach (var item in userprofile.ProfileConfig.Config)
                    {
                        embed.AddField(item.Key, item.Value);
                    }
                    await RespondAsync(embed: embed.Build());
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandSuccessLogEntry(Context.User.Id, "profile settings", DateTime.UtcNow, Context.Guild as SocketGuild)
                    );
                }
                else
                {
                    var userprofile = ProfileManager.GetUserProfile(Context.User.Id);
                    var embed = new EmbedBuilder();
                    SocketGuildUser discorduser = null;
                    if (User != null)
                    {
                        ulong? user = GetIdFromString(User);
                        if (!UserProfile.DoesUserProfileExist(user.Value) && (await Context.Guild.GetUserAsync(user.Value)) is null)
                        {
                            await RespondAsync($"Sorry, i could not find a user with ID {User}");
                            await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                                new CommandSuccessLogEntry(Context.User.Id, "profile settings", DateTime.UtcNow, Context.Guild as SocketGuild)
                                .WithAdditonalField("Additional remarks", $"Sorry, i could not find a user with ID {User}")
                            );
                            return;
                        }
                        userprofile = ProfileManager.GetUserProfile(user.Value);
                    }
                    if (await Context.Guild.GetUserAsync(userprofile.UserID) is SocketGuildUser discorduser2 && discorduser2 is not null)
                    {
                        embed.WithAuthor(discorduser2);
                        discorduser = discorduser2;
                    }
                    if (!IsUserAMod.CheckRequirements(Context,null,null) && userprofile.UserID != Context.User.Id) {
                        await RespondAsync("Sorry, only moderators can change other people's user profiles.");
                        await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                                new CommandWarningLogEntry(Context.User.Id, "profile settings", DateTime.UtcNow, Context.Guild as SocketGuild, "Sorry, only moderators can change other people's user profiles.")
                            );
                        return;
                    }
                    
                    switch (setting)
                    {
                        case "title":
                            userprofile.Title = value;
                            await RespondAsync("I have successfully changed your profile title.");
                            break;
                        case "description":
                            userprofile.Description = value;
                            await RespondAsync("I have successfully changed your profile description.");
                            break;
                        case "color":
                            if (value == null) {
                                userprofile.Color = 0;
                                await RespondAsync("Profile color cleared.");
                            }
                            try {
                                uint intValue = uint.Parse(value, System.Globalization.NumberStyles.HexNumber);
                                userprofile.Color = intValue;
                            } catch (Exception e) {
                                await RespondAsync("Sorry, that colorcode seems invalid, please check your input again.");
                                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                                    new CommandWarningLogEntry(Context.User.Id, "profile settings", DateTime.UtcNow, Context.Guild as SocketGuild, "Sorry, that colorcode seems invalid, please check your input again.")
                                );
                                return;
                            }
                            await RespondAsync("I have successfully changed your profile color.");
                            break;
                        default:
                            if (value == null)
                            {
                                await RespondAsync("Sorry, `value` cannot be empty, if `setting` is not. Please set `value` to the value you want.");
                                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                                        new CommandWarningLogEntry(Context.User.Id, "profile settings", DateTime.UtcNow, Context.Guild as SocketGuild, "Sorry, `value` cannot be empty, if `setting` is not. Please set `value` to the value you want.")
                                    );
                                return;
                            }
                            if (userprofile.ProfileConfig.Config.ContainsKey(setting))
                            {
                                try
                                {
                                    var parsed = StringToType(value, userprofile.ProfileConfig.Config[setting].GetType());
                                    userprofile.ProfileConfig[setting] = parsed;
                                    await RespondAsync("I have successfully changed your profile value.");
                                }
                                catch (Exception e)
                                {
                                    await RespondAsync("Sorry, the value you entered is the wrong type.");
                                }

                            }
                            else
                            {
                                await RespondAsync("Sorry, i could not find that key in your profile.");
                            }
                            break;
                    }
                    await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandSuccessLogEntry(Context.User.Id, "profile settings", DateTime.UtcNow, Context.Guild as SocketGuild)
                    );


                }
            } catch (Exception e) {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "profile settings", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Setting", setting)
                    .WithAdditonalField("Value", value)
                    .WithAdditonalField("User", User)
                );
            }
        }

        [SlashCommand("color", "Sets your current color")]
        public async Task Color(string colorcode = null, string User = null) {
            await Settings("color", colorcode, User);
        }

        [SlashCommand("description", "Sets your profile description")]
        public async Task Description(string value = null, string User = null)
        {
            await Settings("description", value, User);
        }

        [SlashCommand("title", "Sets your profile title")]
        public async Task Title(string value = null, string User = null)
        {
            await Settings("title", value, User);
        }

        [SlashCommand("get", "Gets your current user profile or someone else's")]
        public async Task Get(string User = null)
        {
            try {
                var userprofile = ProfileManager.GetUserProfile(Context.User.Id);
                var embed = new EmbedBuilder();
                if (User != null)
                {
                    ulong? user = GetIdFromString(User);
                    if (!UserProfile.DoesUserProfileExist(user.Value) && (await Context.Guild.GetUserAsync(user.Value)) is null)
                    {
                        await RespondAsync($"Sorry, i could not find a user with ID {User}");
                        return;
                    }
                    userprofile = ProfileManager.GetUserProfile(user.Value);
                }

                var builtpermissionlevel = "";
                {
                    if (Context.Interaction.IsDMInteraction)
                    {
                        builtpermissionlevel = "Permisison level cannot be obtained in DM.";
                    }
                    else
                    {
                        builtpermissionlevel = $"Permission Level {(int)userprofile.GetPermissionLevel(Context.Guild.Id)} [\"{userprofile.GetPermissionLevel(Context.Guild.Id)}\"]";
                    }
                }
                embed.WithColor(userprofile.Color);
                #region Ranking
                var getuser = await Context.Guild.GetUserAsync(userprofile.UserID);
                if (getuser != null)
                {
                    embed.WithAuthor(getuser);
                }


                if (userprofile.Description != "")
                {
                    embed.AddField("User description", userprofile.Description);
                }
                if (userprofile.Title != "")
                {
                    embed.WithTitle(userprofile.Title);
                }
                embed.AddField("Ranking", string.Format(
               @"
                    __**Total experience**__: `{0}`
                    __**Level**__: `{1}`
                    __**Permission Level**__: `{2}`
                "
                ,
                    userprofile.TotalExperience,
                    userprofile.Level,
                    builtpermissionlevel
                ));
                #endregion
                #region Age
                var parsed = Snowflake.Parse(userprofile.UserID.ToString());


                embed.AddField("Age", string.Format(
                @"
                    __**Joined Discord on**__: <t:{0}> / (<t:{0}:R>)
                    __**Joined Server on**__: <t:{1}> / (<t:{1}:R>)
                "
                ,
                    parsed.ToDateTimeOffset().ToUnixTimeSeconds(),
                    getuser != null ? (getuser as SocketGuildUser).JoinedAt.Value.ToUnixTimeSeconds() : 0
                ));

                #endregion
                #region Badges
                embed.AddField("Badges", $"I have earned {userprofile.Badges.Count} badges so far!");
                foreach (var item in userprofile.Badges)
                {
                    embed.AddField($"{item.Icon} __**{item.DisplayName}**__ {FormatLevel(item.Level)}", $"{FormatLevel2(item.Level)} {item.MiniDescription} \n *{item.Description}*", true);
                }
                #endregion
                await RespondAsync(embed: embed.Build());
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                        new CommandSuccessLogEntry(Context.User.Id, "profile get", DateTime.UtcNow, Context.Guild as SocketGuild)
                );
            } catch (Exception e) {
                await CommandLogger.LogCommandAsync(Context.User.Id, Context.Guild as SocketGuild,
                    new CommandUnhandledExceptionLogEntry(Context.User.Id, "profile get", DateTime.UtcNow, Context.Guild as SocketGuild, e)
                    .WithAdditonalField("Parameter 'User'", $"{User}")
                );
            }
        }

        public override Requirements GetRequirements()
        {
            return new Requirements();
        }
    }
}