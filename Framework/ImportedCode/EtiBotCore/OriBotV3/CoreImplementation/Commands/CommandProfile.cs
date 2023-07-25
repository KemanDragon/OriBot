using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Marshalling;
using OldOriBot.Data;
using OldOriBot.Data.Commands.ArgData;
using OldOriBot.Data.MemberInformation;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.PermissionData;
using OldOriBot.UserProfiles;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Responding;

namespace OldOriBot.CoreImplementation.Commands {
	public partial class CommandProfile : Command {
		public override string Name { get; } = "profile";
		public override string Description { get; } = "View your profile or someone else's profile, or modify your profile's display information.";
		public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Person>("person").SetRequiredState(false);
		public override Command[] Subcommands { get; }
		public override bool RequiresContext { get; } = true;
		public CommandProfile(BotContext ctx) : base(ctx) {
			Subcommands = new Command[] {
				new CommandProfileSetData(ctx, this),
				new CommandProfileReload(ctx, this),
				new CommandProfileConfig(ctx, this),
				new CommandProfileMini(ctx, this),
				new CommandProfileBadge(ctx, this),
			};
		}

		public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
			await ResponseUtil.StartTypingAsync(originalMessage);
			ArgumentMap<Person> args = Syntax.SetContext(executionContext).Parse<Person>(argArray.ElementAtOrDefault(0));
			if (args.Arg1 == null) {
				UserProfile profile = UserProfile.GetOrCreateProfileOf(executor);
				Embed embed = profile.ToEmbed();
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, embed, AllowedMentions.Reply);
			} else {
				Member target = args.Arg1.Member;
				if (target == null) throw new CommandException(this, Personality.Get("cmd.err.noMemberFound"));
				UserProfile profile = UserProfile.GetOrCreateProfileOf(target);
				Embed embed = profile.ToEmbed();
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, embed, AllowedMentions.Reply);
			}
		}

		public class CommandProfileMini : Command {
			public override string Name { get; } = "mini";
			public override string Description { get; } = "Display a minimal variant of your profile without your title, description, or badges.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Person>("person").SetRequiredState(false);
			public CommandProfileMini(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				await ResponseUtil.StartTypingAsync(originalMessage);
				ArgumentMap<Person> args = Syntax.SetContext(executionContext).Parse<Person>(argArray.ElementAtOrDefault(0));
				if (args.Arg1 == null) {
					UserProfile profile = UserProfile.GetOrCreateProfileOf(executor);
					Embed embed = profile.ToEmbed(true);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, embed, AllowedMentions.Reply);
				} else {
					Member target = args.Arg1.Member;
					if (target == null) throw new CommandException(this, Personality.Get("cmd.err.noMemberFound"));
					UserProfile profile = UserProfile.GetOrCreateProfileOf(target);
					Embed embed = profile.ToEmbed(true);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, embed, AllowedMentions.Reply);
				}
			}
		}

		public class CommandMiniProfileDeprecated : DeprecatedCommand {
			public override string Name { get; } = "miniprofile";
			public CommandMiniProfileDeprecated(CommandProfileMini miniCmd) : base(miniCmd) { }
		}

		public class CommandProfileSetData : Command {

			#region Set Data Base Container

			public override string Name { get; } = "set";
			public override string Description { get; } = "Sets certain display information about your profile.";
			public override ArgumentMapProvider Syntax { get; }
			public override bool IsExclusiveBase { get; } = true;
			public override Command[] Subcommands { get; }
			public override bool RequiresContext { get; } = true;

			public CommandProfileSetData(BotContext ctx, Command parent) : base(ctx, parent) {
				Subcommands = new Command[] {
					new CommandProfileSetTitle(ctx, this),
					new CommandProfileSetDescription(ctx, this),
					new CommandProfileSetColor(ctx, this),
				};
			}

			public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) => throw new NotImplementedException();

			#endregion

			public class CommandProfileSetTitle : Command {
				public override string Name { get; } = "title";
				public override string Description { get; } = "Modify the title of your profile, which is a (relatively) short brief description about yourself. Enter nothing to remove your title.";
				public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string>("title").SetRequiredState(false);
				public override bool RequiresContext { get; } = true;

				public CommandProfileSetTitle(BotContext ctx, Command parent) : base(ctx, parent) { }

				public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
					if (argArray.Length > 1) {
						throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
					}
					await ResponseUtil.StartTypingAsync(originalMessage);
					UserProfile profile = UserProfile.GetOrCreateProfileOf(executor);
					ArgumentMap<string> args = Syntax.SetContext(executionContext).Parse<string>(argArray.ElementAtOrDefault(0));
					profile.Title = args.Arg1;
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.profile.elementChanged", "title"), null, AllowedMentions.Reply);
				}
			}

			public class CommandProfileSetDescription : Command {
				public override string Name { get; } = "description";
				public override string Description { get; } = "Modify the description of your profile, which is a longer and more informative section about yourself. Enter nothing to remove your description.";
				public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string>("description").SetRequiredState(false);
				public override bool RequiresContext { get; } = true;

				public CommandProfileSetDescription(BotContext ctx, Command parent) : base(ctx, parent) { }

				public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
					if (argArray.Length > 1) {
						throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
					}
					await ResponseUtil.StartTypingAsync(originalMessage);
					UserProfile profile = UserProfile.GetOrCreateProfileOf(executor);
					ArgumentMap<string> args = Syntax.SetContext(executionContext).Parse<string>(argArray.ElementAtOrDefault(0));
					profile.Description = args.Arg1;
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.profile.elementChanged", "description"), null, AllowedMentions.Reply);
				}
			}

			public class CommandProfileSetColor : Command {
				public override string Name { get; } = "color";
				public override string Description { get; } = "Modify the color of your profile, which determines the display of the sidebar on the embed.";
				public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Color>("color").SetRequiredState(true);
				public override string[] Aliases { get; } = {
					"colour"
				};
				public override bool RequiresContext { get; } = true;

				public CommandProfileSetColor(BotContext ctx, Command parent) : base(ctx, parent) { }

				public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
					UserProfile profile = UserProfile.GetOrCreateProfileOf(executor); 
					if (argArray.Length == 0) {
						profile.Color = null;
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.profile.elementChanged", "color"), null, AllowedMentions.Reply);
						//throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
					} else if (argArray.Length > 1) {
						throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
					}
					await ResponseUtil.StartTypingAsync(originalMessage);
					ArgumentMap<Color> args = Syntax.SetContext(executionContext).Parse<Color>(argArray.ElementAtOrDefault(0));
					profile.Color = args.Arg1?.Value;
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.profile.elementChanged", "color"), null, AllowedMentions.Reply);
				}
			}
			
			public class CommandProfileSetExperience : Command {
				public override string Name { get; } = "experience";
				public override string Description { get; } = "Modify the experience of the given profile.";
				public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Person, long>("user", "amount").SetRequiredState(false, true);
				public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
				public override bool RequiresContext { get; } = true;

				public CommandProfileSetExperience(BotContext ctx, Command parent) : base(ctx, parent) { }

				public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
					if (argArray.Length == 0) {
						throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
					} else if (argArray.Length > 2) {
						throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
					}

					ArgumentMap<Person, long> args;
					if (argArray.Length == 1) {
						args = Syntax.SetContext(executionContext).Parse<Person, long>(null, argArray[0]);
					} else {
						args = Syntax.SetContext(executionContext).Parse<Person, long>(argArray[0], argArray[1]);
					}

					Member target = args.Arg1?.Member ?? executor;
					long amount = args.Arg2;
					
					if (amount == 0) throw new CommandException(this, Personality.Get("cmd.ori.profile.err.zeroValue"));

					await ResponseUtil.StartTypingAsync(originalMessage);
					UserProfile profile = UserProfile.GetOrCreateProfileOf(target);
					if (profile == null) throw new CommandException(this, Personality.Get("cmd.ori.profile.err.failedToGetProfile"));
					if (amount == -1) {
						profile.Experience = double.PositiveInfinity;
					} else {
						profile.Experience = amount;
					}
					profile.Save();
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.profile.elementChanged", "experience"), null, AllowedMentions.Reply);
				}
			}

		}

		public class CommandProfileReload : Command {

			public override string Name { get; } = "reload";
			public override string Description { get; } = "Forcefully reloads this profile from file.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Person>("user").SetRequiredState(false);
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override bool RequiresContext { get; } = true;

			public CommandProfileReload(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length > 1) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}
				if (argArray.Length == 0) {
					await ResponseUtil.StartTypingAsync(originalMessage);
					UserProfile.GetOrCreateProfileOf(executor).Reload();
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.profile.reload"), null, AllowedMentions.Reply);
				} else {
					ArgumentMap<Person> args = Syntax.SetContext(executionContext).Parse<Person>(argArray[0]);
					Member target = args.Arg1.Member;
					if (target == null) throw new CommandException(this, Personality.Get("cmd.err.noMemberFound"));
					await ResponseUtil.StartTypingAsync(originalMessage);
					UserProfile.GetOrCreateProfileOf(target).Reload();
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.profile.reloadOther"), null, AllowedMentions.Reply);

				}
			}
		}

		public class CommandProfileConfig : Command {
			public override string Name { get; } = "config";
			public override string Description { get; } = "Change or view the settings you can edit on your profile.";
			public override ArgumentMapProvider Syntax { get; }
			public override bool IsExclusiveBase { get; } = true;
			public override Command[] Subcommands { get; }
			public CommandProfileConfig(BotContext ctx, Command parent) : base(ctx, parent) {
				Subcommands = new Command[] {
					new CommandProfileConfigGet(ctx, this),
					new CommandProfileConfigSet(ctx, this),
				};
			}

			public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) => throw new NotImplementedException();

			public class CommandProfileConfigSet : Command {
				public override string Name { get; } = "set";
				public override string Description { get; } = "Change configuration information on your profile. The editable values can be seen by using `get`. Values in the list are formatted as `key=value`. Take the `PingOnReply` setting, for instance. If you wish to disable this feature, use `>> profile config set PingOnReply false` (where `PingOnReply` is the `key` and `false` is the `value`).";
				public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string, Variant<bool, double, string>>("key", "value").SetRequiredState(true, false);
				public CommandProfileConfigSet(BotContext ctx, Command parent) : base(ctx, parent) { }

				public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
					if (argArray.Length < 2) {
						throw new CommandException(this, Personality.Get("cmd.err.missingArgs", $"{Syntax.GetArgName(1)} and/or {Syntax.GetArgName(0)}"));
					} else if (argArray.Length > 2) {
						throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
					}
					await ResponseUtil.StartTypingAsync(originalMessage);
					UserProfile profile = UserProfile.GetOrCreateProfileOf(executor);
					ArgumentMap<string, Variant<bool, double, string>> args = Syntax.SetContext(executionContext).Parse<string, Variant<bool, double, string>>(argArray[0], argArray[1]);

					string key = args.Arg1;
					Variant<bool, double, string> value = args.Arg2;
					bool canSetAnyData = executor.GetPermissionLevel() >= PermissionLevel.Operator;

					if (!profile.UserData.ContainsKey(key) && !canSetAnyData) {
						throw new CommandException(this, Personality.Get("cmd.ori.profile.arbData"));
					}

					if (value.ArgIndex == 3 && value.Value3.ToLower() == "null") {
						if (canSetAnyData && profile.UserData.ContainsKey(key)) {
							profile.UserData[key] = null;
							await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.profile.cfgRemoved"), null, AllowedMentions.Reply);
							return;
						} else {
							if (!canSetAnyData) {
								throw new CommandException(this, Personality.Get("cmd.ori.profile.arbData"));
							} else {
								throw new CommandException(this, Personality.Get("cmd.ori.profile.alreadyNull"));
							}
						}
					}

					object existingValue = profile.UserData[key];
					Type existingType = existingValue.GetType();
					if (existingType == typeof(bool)) {
						if (value.ArgIndex == 1) {
							profile.UserData[key] = value.Value1;
						} else if (value.ArgIndex == 2) {
							throw new CommandException(this, Personality.Get("cmd.ori.profile.invalidType", "a boolean value", "a number"));
						} else {
							throw new CommandException(this, Personality.Get("cmd.ori.profile.invalidType", "a boolean value", "text"));
						}
					} else if (existingType.IsNumericType()) {
						if (value.ArgIndex == 1) {
							throw new CommandException(this, Personality.Get("cmd.ori.profile.invalidType", "a number", "a boolean value"));
						} else if (value.ArgIndex == 2) {
							profile.UserData[key] = value.Value2;
						} else {
							throw new CommandException(this, Personality.Get("cmd.ori.profile.invalidType", "a number", "text"));
						}
					} else if (existingType == typeof(string)) {
						if (value.ArgIndex == 1) {
							throw new CommandException(this, Personality.Get("cmd.ori.profile.invalidType", "text", "a boolean value"));
						} else if (value.ArgIndex == 2) {
							throw new CommandException(this, Personality.Get("cmd.ori.profile.invalidType", "text", "a number"));
						} else {
							profile.UserData[key] = value.Value3;
						}
					} else {
						throw new CommandException(this, Personality.Get("cmd.ori.profile.unkType"));
					}
					profile.Save();
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.profile.cfgUpdated"), null, AllowedMentions.Reply);
				}
			}

			public class CommandProfileConfigGet : Command {
				public override string Name { get; } = "get";
				public override string Description { get; } = "View configuration information on your profile. These values are sorted as `key=value`, that is, the word before the equals sign is the key - a unique name used to refer to that option, and the value is what the setting actually *is*.";
				public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string>("key").SetRequiredState(false);
				public CommandProfileConfigGet(BotContext ctx, Command parent) : base(ctx, parent) { }

				public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
					if (argArray.Length > 1) {
						throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
					}
					await ResponseUtil.StartTypingAsync(originalMessage);
					UserProfile profile = UserProfile.GetOrCreateProfileOf(executor);
					ArgumentMap<string> args = Syntax.SetContext(executionContext).Parse<string>(argArray.ElementAtOrDefault(0));
					string key = args.Arg1;
					if (key != null) {
						// list specific
						if (profile.UserData.ContainsKey(key)) {
							await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"[`{key}`]={profile.UserData[key]}", null, AllowedMentions.Reply);
						} else {
							await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.profile.noData"), null, AllowedMentions.Reply);
						}
					} else {
						// list all
						string message = profile.UserData.ToDiscordMessageString(false);
						await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, message, null, AllowedMentions.Reply);
					}
				}
			}

		}

		public class CommandProfileBadge : Command {
			public override string Name { get; } = "badge";
			public override string Description { get; } = "Manipulate the badges present on a user's profile.";
			public override ArgumentMapProvider Syntax { get; }
			public override bool IsExclusiveBase { get; } = true;
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
			public override Command[] Subcommands { get; }
			public CommandProfileBadge(BotContext ctx, Command parent) : base(ctx, parent) {
				Subcommands = new Command[] {
					new CommandProfileBadgeAdd(ctx, this),
					new CommandProfileBadgeAddCustom(ctx, this),
					new CommandProfileBadgeRemove(ctx, this),
					new CommandProfileBadgeSetLevel(ctx, this),
					new CommandProfileBadgeAddIdea(ctx, this)
				};
			}

			public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) => Task.CompletedTask;

			public class CommandProfileBadgeAdd : Command {
				public override string Name { get; } = "add";
				public override string Description { get; } = "Adds a predefined badge to the user's badges.";
				public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Person, string, ushort>("user", "badgeName", "badgeLevel").SetRequiredState(false, true, false);
				public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
				public CommandProfileBadgeAdd(BotContext ctx, Command parent) : base(ctx, parent) { }

				public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
					if (argArray.Length == 0) {
						throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(1)));
					} else if (argArray.Length > 3) {
						throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
					}

					ArgumentMap<Person, string, ushort> args;
					if (argArray.Length == 3) {
						args = Syntax.SetContext(executionContext).Parse<Person, string, ushort>(argArray[0], argArray[1], argArray[2]);
					} else if (argArray.Length == 2) {
						if (ushort.TryParse(argArray[1], out ushort _)) {
							// name, level
							args = Syntax.SetContext(executionContext).Parse<Person, string, ushort>(null, argArray[0], argArray[1]);
						} else {
							// person, name
							args = Syntax.SetContext(executionContext).Parse<Person, string, ushort>(argArray[0], argArray[1], null);
						}
					} else {
						// name
						args = Syntax.SetContext(executionContext).Parse<Person, string, ushort>(null, argArray[0], null);
					}

					Member target = args.Arg1?.Member ?? executor;
					string badgeName = args.Arg2;
					ushort level = args.Arg3;

					await ResponseUtil.StartTypingAsync(originalMessage);
					UserProfile profile = UserProfile.GetOrCreateProfileOf(target);
					if (profile == null) throw new CommandException(this, Personality.Get("cmd.ori.profile.err.failedToGetProfile"));
					Badge badge = BadgeRegistry.GetBadgeFromPredefinedRegistry(badgeName);
					if (badge == null) throw new CommandException(this, Personality.Get("cmd.ori.profile.err.noBadgeFound", badgeName));
					badge = badge.Clone();
					badge.Level = level;
					profile.GrantBadge(badge, false, true);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.profile.success.badgeAdded", badge.Name));
				}
			}

			public class CommandProfileBadgeRemove : Command {
				public override string Name { get; } = "remove";
				public override string Description { get; } = "Removes a badge from the user's badges.";
				public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Person, string>("user", "badgeName").SetRequiredState(false, true);
				public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
				public CommandProfileBadgeRemove(BotContext ctx, Command parent) : base(ctx, parent) { }

				public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
					if (argArray.Length == 0) {
						throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(1)));
					} else if (argArray.Length > 2) {
						throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
					}

					ArgumentMap<Person, string> args;
					if (argArray.Length == 1) {
						args = Syntax.SetContext(executionContext).Parse<Person, string>(null, argArray[0]);
					} else {
						args = Syntax.SetContext(executionContext).Parse<Person, string>(argArray[0], argArray[1]);
					}

					Member target = args.Arg1?.Member ?? executor;
					string badgeName = args.Arg2;

					await ResponseUtil.StartTypingAsync(originalMessage);
					UserProfile profile = UserProfile.GetOrCreateProfileOf(target);
					if (profile == null) throw new CommandException(this, Personality.Get("cmd.ori.profile.err.failedToGetProfile"));
					Badge badge = profile.Badges.FirstOrDefault(badge => badge.Name.ToLower() == badgeName.ToLower());
					if (badge == null) throw new CommandException(this, Personality.Get("cmd.ori.profile.err.noBadgeFound"));
					profile.RemoveBadge(badge);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.profile.success.badgeRemoved", badge.Name));
				}
			}

			public class CommandProfileBadgeSetLevel : Command {
				public override string Name { get; } = "setlevel";
				public override string Description { get; } = "Sets the level of a badge. Input no level to set the level to 0 (or just input 0).";
				public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Person, string, ushort>("user", "badgeName", "newLevel").SetRequiredState(false, true, false);
				public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
				public CommandProfileBadgeSetLevel(BotContext ctx, Command parent) : base(ctx, parent) { }
				public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
					if (argArray.Length == 0) {
						throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(1)));
					} else if (argArray.Length > 3) {
						throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
					}

					ArgumentMap<Person, string, ushort> args;
					if (argArray.Length == 3) {
						args = Syntax.SetContext(executionContext).Parse<Person, string, ushort>(argArray[0], argArray[1], argArray[2]);
					} else if (argArray.Length == 2) {
						if (ushort.TryParse(argArray[1], out ushort _)) {
							// name, level
							args = Syntax.SetContext(executionContext).Parse<Person, string, ushort>(null, argArray[0], argArray[1]);
						} else {
							// person, name
							args = Syntax.SetContext(executionContext).Parse<Person, string, ushort>(argArray[0], argArray[1], null);
						}
					} else {
						// name
						args = Syntax.SetContext(executionContext).Parse<Person, string, ushort>(null, argArray[0], null);
					}

					Member target = args.Arg1?.Member ?? executor;
					string badgeName = args.Arg2;
					ushort level = args.Arg3;

					await ResponseUtil.StartTypingAsync(originalMessage);
					UserProfile profile = UserProfile.GetOrCreateProfileOf(target);
					if (profile == null) throw new CommandException(this, Personality.Get("cmd.ori.profile.err.failedToGetProfile"));
					Badge badge = profile.Badges.FirstOrDefault(badge => badge.Name.ToLower() == badgeName.ToLower());
					if (badge == null) throw new CommandException(this, Personality.Get("cmd.ori.profile.err.noBadgeFound", badgeName));
					badge.Level = level;
					profile.Save();
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.profile.success.badgeEdited", badge.Name));
				}
			}

			public class CommandProfileBadgeAddCustom : Command {
				public override string Name { get; } = "addcustom";
				public override string Description { get; } = "Adds a custom badge to the user's badges.";
				public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Person, string, string, string, string>("user", "badgeName", "badgeMinidesc", "badgeDesc", "badgeIconEmoji").SetRequiredState(true, true, true, true, true);
				public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
				public CommandProfileBadgeAddCustom(BotContext ctx, Command parent) : base(ctx, parent) { }

				public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
					if (argArray.Length == 0) {
						throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(1)));
					} else if (argArray.Length > 5) {
						throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
					}

					ArgumentMap<Person, string, string, string, string> args = Syntax.SetContext(executionContext).Parse<Person, string, string, string, string>(argArray[0], argArray[1], argArray[2], argArray[3], argArray[4]);


					Member target = args.Arg1?.Member ?? executor;
					string badgeName = args.Arg2;
					string badgeMiniDesc = args.Arg3;
					string badgeDesc = args.Arg4;
					string badgeIcon = args.Arg5;

					await ResponseUtil.StartTypingAsync(originalMessage);
					UserProfile profile = UserProfile.GetOrCreateProfileOf(target);
					if (profile == null) throw new CommandException(this, Personality.Get("cmd.ori.profile.err.failedToGetProfile"));

					Badge custom = new Badge(badgeName, badgeDesc, badgeMiniDesc, badgeIcon, 1, 0);
					profile.GrantBadge(custom, false, true);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.profile.success.badgeAdded", custom.Name));
				}
			}

			public class CommandProfileBadgeAddIdea : Command {
				public override string Name { get; } = "addidea";
				public override string Description { get; } = "A highly specialzied subcommand that adds the predefined \"Approved Idea\" badge to the user's badges.";
				public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<Person, string>("user", "idea").SetRequiredState(true, true);
				public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Operator;
				public CommandProfileBadgeAddIdea(BotContext ctx, Command parent) : base(ctx, parent) { }

				public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
					if (argArray.Length == 0) {
						throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(1)));
					} else if (argArray.Length > 3) {
						throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
					}

					ArgumentMap<Person, string> args = Syntax.SetContext(executionContext).Parse<Person, string>(argArray[0], argArray[1]);
					Member target = args.Arg1.Member;
					string idea = args.Arg2;

					await ResponseUtil.StartTypingAsync(originalMessage);
					UserProfile profile = UserProfile.GetOrCreateProfileOf(target);
					if (profile == null) throw new CommandException(this, Personality.Get("cmd.ori.profile.err.failedToGetProfile"));
					profile.GrantBadge(BadgeRegistry.ConstructApprovedIdeaBadge(idea), false, true);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, Personality.Get("cmd.ori.profile.success.badgeAdded", "Approved Idea"));
				}
			}
		}
	}
}
