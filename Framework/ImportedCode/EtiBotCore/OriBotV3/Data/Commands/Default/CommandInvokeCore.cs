using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects;
using EtiBotCore.DiscordObjects.Factory;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using OldOriBot.CoreImplementation;
using OldOriBot.CoreImplementation.Handlers;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;
using OldOriBot.Interaction.CommandData;
using OldOriBot.PermissionData;
using OldOriBot.Utility.Arguments;
using OldOriBot.Utility.Formatting;
using OldOriBot.Utility.Music;
using OldOriBot.Utility.Music.FileRepresentation;
using OldOriBot.Utility.Responding;

namespace OldOriBot.Data.Commands.Default {

	public class CommandInvokeCore : Command {
		public override string Name { get; } = "invoke";
		public override string Description { get; } = "Perform low-level control of bot functions.";
		public override ArgumentMapProvider Syntax { get; }
		public override Command[] Subcommands { get; }
		public override bool IsExclusiveBase { get; } = true;
		public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Archon;
		public CommandInvokeCore() : base(null) {
			Subcommands = new Command[] {
				new CommandReloadPersonality(null, this),
				new CommandSendStupid(null, this),
				new CommandTestFail(null, this),
				new CommandSetDevMode(null, this),
				// new CommandEncodeAll(null, this),
				new CommandForceNoEndpoint(null, this),
				new CommandReinitializeApplication(null, this),
				new CommandTestNicknameReplacement(null, this),
				new CommandTestOriTestSet(null, this),
				new CommandInvokeMusicFailure(null, this),
				new CommandGetAvailableMods(null, this),
				new CommandTestGetReply(null, this),
			};
		}

		public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) => throw new NotImplementedException();


		public class CommandReloadPersonality : Command {
			public override string Name { get; } = "reloadpersonality";
			public override string Description { get; } = "Reloads the current personality engine, repopulating all of the response keys from disk.";
			public override ArgumentMapProvider Syntax { get; }
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Archon;
			public CommandReloadPersonality(BotContext ctx, Command parent) : base(ctx, parent) { }
			public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				Personality.Current.Reload();
				return Task.CompletedTask;
			}
		}

		public class CommandSendStupid : Command {
			public override string Name { get; } = "sendstupid";
			public override string Description { get; } = "Sends the meme of the scout saying \"AW CRAP, I AM STUPID\" to test file uploads.";
			public override ArgumentMapProvider Syntax { get; }
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Archon;
			public override bool NoConsole { get; } = true;
			public CommandSendStupid(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				FileInfo stupid = new FileInfo(@"C:\AW CRAP! I AM STUPID.mp4");
				FileInfo butwhy = new FileInfo(@"C:\butwhy.webm");
				await originalMessage.ReplyAsync(null, null, null, attachments: new FileInfo[] { stupid, butwhy });
			}
		}

		public class CommandTestFail : Command {
			public override string Name { get; } = "testfailure";
			public override string Description { get; } = "Attempts to set Eti's nickname to something (which will fail), then relays the status of his member object.";
			public override ArgumentMapProvider Syntax { get; }
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Archon;
			public override bool NoConsole { get; } = true;
			public CommandTestFail(BotContext ctx, Command parent) : base(ctx, parent) { }
			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				Member eti = await executionContext.Server.GetMemberAsync(114163433980559366);
				string nickBefore = eti.Nickname;
				eti.BeginChanges();
				eti.Nickname = "Not Eti";
				await eti.ApplyChanges("Testing a purposeful network failure.");
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, $"Original nickname: {nickBefore}\nNew nickname: {eti.Nickname}\n(These should be identical)");
			}
		}

		public class CommandSetDevMode : Command {
			public override string Name { get; } = "devmode";
			public override string Description { get; } = "Sets whether or not developer mode is active, which changes a number of behaviors.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<bool>("enabled").SetRequiredState(true);
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Archon;
			public override bool NoConsole { get; } = false;
			public CommandSetDevMode(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length == 0) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
				} else if (argArray.Length > 1) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}
				ArgumentMap<bool> args = Syntax.Parse<bool>(argArray[0]);
				bool enable = args.Arg1;
				DiscordClient.Current.DevMode = enable;
				return Task.CompletedTask;
			}
		}

		public class CommandEncodeAll : Command {
			public override string Name { get; } = "encodeall";
			public override string Description { get; } = "Encodes all music files.";
			public override ArgumentMapProvider Syntax { get; }
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Archon;
			public override bool NoConsole { get; } = true;
			public CommandEncodeAll(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				BotContextTestServer ctxTest = BotContextRegistry.GetContext<BotContextTestServer>();
				VoiceChannel musicChannel = ctxTest.Server.GetChannel<VoiceChannel>(794170577413472276);
				TextChannel textChannel = ctxTest.Server.GetChannel<TextChannel>(797601510812287027);
				MusicController music = MusicController.GetOrCreate(musicChannel, textChannel);
				int completed = 0;
				DiscordClient.Current!.DevMode = true;
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "Encoding all music files. The console will relay progress. Dev mode has been set to true to prevent music from playing.");
				IReadOnlyList<MusicFile> files = music.Pool.AllMusicFiles;
				CommandLogger.WriteLine($"Have fun. I'm spawning {files.Count} Tasks.");
				foreach (MusicFile mus in files) {
					_ = Task.Run(() => {
						//TransmitHelper.EncodeParallel(mus.File);
						completed++;
					});
				}
				while (completed < files.Count) {
					await Task.Delay(10000);
				}
				CommandLogger.WriteLine("Done!");
			}
		}

		public class CommandForceNoEndpoint : Command {
			public override string Name { get; } = "endpointfail";
			public override string Description { get; } = "Forces an emulated Error 1001 ENDPOINT_UNAVAILABLE to be received by the bot, triggering a reconnect.";
			public override ArgumentMapProvider Syntax { get; }
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Archon;
			public CommandForceNoEndpoint(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				DiscordClient.ForceNextAsNoEndpoint = true;
				return Task.CompletedTask;
			}
		}

		public class CommandReinitializeApplication : Command {
			public override string Name { get; } = "restartprogram";
			public override string Description { get; } = "Executes a special subroutine that starts a new instance of the bot's application and terminates this one.";
			public override ArgumentMapProvider Syntax { get; }
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Archon;
			public CommandReinitializeApplication(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				DiscordClient.RestartProgram();
				return Task.CompletedTask;
			}
		}

		public class CommandTestNicknameReplacement : Command {
			public override string Name { get; } = "testreplace";
			public override string Description { get; } = "Test the nickname character replacement system.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string>("specialGlyphs").SetRequiredState(true);
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Archon;
			public CommandTestNicknameReplacement(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				if (argArray.Length == 0) {
					throw new CommandException(this, Personality.Get("cmd.err.missingArgs", Syntax.GetArgName(0)));
				} else if (argArray.Length > 1) {
					throw new CommandException(this, Personality.Get("cmd.err.tooManyArgs"));
				}

				ArgumentMap<string> args = Syntax.SetContext(executionContext).Parse<string>(argArray[0]);
				string fancyText = args.Arg1;

				EmbedBuilder builder = new EmbedBuilder() {
					Title = "Corrected Text Output",
				};
				builder.AddField("Original Text", fancyText);
				builder.AddField("Replaced Text", FancyFontMap.Convert(fancyText));
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, null, builder.Build(), AllowedMentions.Reply);
			}
		}

		public class CommandTestOriTestSet : Command {
			public override string Name { get; } = "testsub";
			public override string Description { get; } = "Tests the substitution system for the passive response system.";
			public override ArgumentMapProvider Syntax { get; }
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.BotDeveloper;
			public CommandTestOriTestSet(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				string[] results = HandlerPassiveResponseSystem.GetAllTestResults();
				CommandLogger.WriteLine("ALL VARIATIONS:");
				foreach (string result in results) {
					CommandLogger.WriteLine(result);
				}
				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "The outputs have been placed in the bot's console.");
			}
		}

		public class CommandInvokeMusicFailure : Command {
			public override string Name { get; } = "musicfail";
			public override string Description { get; } = "Emulates a failure in the music connection to test the recovery system.";
			public override ArgumentMapProvider Syntax { get; }
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Archon;
			public CommandInvokeMusicFailure(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {


				await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "Emulating a connection failure...");
				
			}
		}

		public class CommandGetAvailableMods : Command {
			public override string Name { get; } = "getavailablemods";
			public override string Description { get; } = "Returns the list of mods deemed available for the AnyMod role ping's selector.";
			public override ArgumentMapProvider Syntax { get; }
			public override PermissionLevel RequiredPermissionLevel => PermissionLevel.Archon;
			public CommandGetAvailableMods(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {
				BotContextOriTheGame ctxOri = BotContextRegistry.GetContext<BotContextOriTheGame>();
				if (executionContext != ctxOri) {
					throw new CommandException(this, "Cannot use this command in any context other than instance of BotContextOriTheGame");
				}

				HandlerRandomModSelector selector = ctxOri.GetPassiveHandlerInstance<HandlerRandomModSelector>();
				EmbedBuilder builder = new EmbedBuilder {
					Title = "Available Mods"
				};
				string desc = "";
				foreach (Member mbr in selector.AvailableMods) {
					desc += mbr.FullNickname + "\n";
				}
				if (desc == "") {
					desc = "None. Using will ping all mods.";
				}
				builder.Description = desc;
				await originalMessage.ReplyAsync(null, builder.Build(), AllowedMentions.Reply);
			}
		}

		public class CommandTestGetReply : Command {
			public override string Name { get; } = "testgetreply";
			public override string Description { get; } = "Attempts to index a reply that has been deleted. Experimental.";
			public override ArgumentMapProvider Syntax { get; } = new ArgumentMapProvider<string>("jumpLink").SetRequiredState(true);
			public override PermissionLevel RequiredPermissionLevel { get; } = PermissionLevel.Archon;
			public CommandTestGetReply(BotContext ctx, Command parent) : base(ctx, parent) { }

			public override async Task ExecuteCommandAsync(Member executor, BotContext executionContext, Message originalMessage, string[] argArray, string rawArgs, bool isConsole) {

				ArgumentMap<string> args = Syntax.SetContext(executionContext).Parse<string>(argArray[0]);

				string msg = args.Arg1;
				Match match = Regex.Match(msg, @"https://discord.com/channels/(\d+)/(\d+)/(\d+)");
				if (match.Success) {
					Snowflake server = Snowflake.Parse(match.Groups[1].Value);
					Snowflake channelId = Snowflake.Parse(match.Groups[2].Value);
					Snowflake messageId = Snowflake.Parse(match.Groups[3].Value);
					if (server != executionContext.Server.ID) {
						throw new CommandException(this, "The given jump link does not point to the server you are running this command in!");
					}

					Message obj = await executionContext.Server.TextChannels.FirstOrDefault(ch => ch.ID == channelId)?.GetMessageAsync(messageId);
					await ResponseUtil.RespondToAsync(originalMessage, CommandLogger, "Message is replying to " + obj.Reference.ToString());
				} else {
					throw new CommandException(this, "Expecting a jump link.");
				}

			}
		}
	}
}
