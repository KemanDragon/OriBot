using System;
using System.Collections.Generic;
using System.Linq;
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


        [AutocompleteCommand("config", "set")]
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

        [AutocompleteCommand("config", "get")]
        public async Task Autocomplete2()
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
                    return ulong.Parse(mentionorid.Replace("<@", "").Replace(">", ""));
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

        [SlashCommand("set", "Sets a value for a user profile")]
        public async Task Set([Discord.Interactions.Summary("config"), Autocomplete] string key, string value, SocketGuildUser user = null)
        {
            if (user == null)
            {
                user = Context.User as SocketGuildUser;
            }
            var userprofile = ProfileManager.GetUserProfile(user.Id);
            switch (key)
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
                    uint intValue = uint.Parse(value, System.Globalization.NumberStyles.HexNumber);
                    userprofile.Color = intValue;
                    await RespondAsync("I have successfully changed your profile color.");
                    break;
                default:
                    if (userprofile.ProfileConfig.Config.ContainsKey(key))
                    {
                        try {
                            var parsed = StringToType(value, userprofile.ProfileConfig.Config[key].GetType());
                            userprofile.ProfileConfig[key] = parsed;
                            await RespondAsync("I have successfully changed your profile value.");
                        } catch (Exception e)
                        {
                            await RespondAsync("Sorry, the value you entered is the wrong type.");
                        }
                        
                    } else {
                        await RespondAsync("Sorry, i could not find that key in your profile.");
                    }
                    break;
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

        [SlashCommand("get", "Gets your current user profile or a value")]
        public async Task Get(string User = null, [Discord.Interactions.Summary("config"), Autocomplete] string value = null)
        {
            ulong? user = GetIdFromString(User);
            if (!UserProfile.DoesUserProfileExist(user.Value) && (await Context.Guild.GetUserAsync(user.Value)) is null) {
                await RespondAsync($"Sorry, i could not find a user with ID {User}");
                return;
            }
            var userprofile = ProfileManager.GetUserProfile(user.Value);
            if (value == null)
            {
                var embed = new EmbedBuilder();
                
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
                var getuser = await Context.Guild.GetUserAsync(user.Value);
                if (getuser != null) {
                    embed.WithAuthor(getuser);
                }
                
                
                if (userprofile.Description != "")
                {
                    embed.AddField("User description", userprofile.Description);
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
                var parsed = Snowflake.Parse(user.Value.ToString());


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
            } else
            {
                var embed = new EmbedBuilder();
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
                var getuser = await Context.Guild.GetUserAsync(user.Value);
                if (getuser != null)
                {
                    embed.WithAuthor(getuser);
                }
                switch (value)
                {
                    case "title":
                        embed.AddField($"User title", $"{userprofile.Title}");
                        break;
                    case "description":
                        embed.AddField($"User description", $"{userprofile.Description}");
                        break;
                    case "color":
                        embed.AddField($"User embed color", $"{userprofile.Color}");
                        break;
                    default:
                        if (userprofile.ProfileConfig.Config.ContainsKey(value))
                        {
                            embed.AddField($"{value}", $"{userprofile.ProfileConfig[value]}");
                        }
                        else
                        {
                            await RespondAsync("Sorry, i could not find that key in your profile.");
                        }
                        break;
                }
                embed.WithColor(userprofile.Color);
                
                await RespondAsync("",embed: embed.Build());
               // embed.AddField($"{value}", $"{userprofile.P.GetValue(value)}");
            }
        }

        public override Requirements GetRequirements()
        {
            return new Requirements();
        }
    }
}