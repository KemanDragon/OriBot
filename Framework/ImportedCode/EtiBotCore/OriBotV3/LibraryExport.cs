using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

//using CustomConsole.Hooks;
using EtiBotCore.Client;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Utility.Extension;
using EtiLogger.Logging;
using OldOriBot.Data;
using OldOriBot.Data.Commands;
using EtiBotCore.DiscordObjects.Guilds.MemberData;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.Specialized;
using OldOriBot.Data.Persistence;


namespace OldOriBot
{
    public class LibraryExport
    {
        public static readonly Activity[] Activities = new Activity[] {
            Activity.CreateActivityForBot("the leaves fly by", ActivityType.Watching),
            Activity.CreateActivityForBot("the waves in the water", ActivityType.Watching),
            Activity.CreateActivityForBot("the forest", ActivityType.Watching),

            Activity.CreateActivityForBot("Naru's stories", ActivityType.Listening),
            Activity.CreateActivityForBot("the wind and the leaves", ActivityType.Listening),
            Activity.CreateActivityForBot("The Spirit Tree", ActivityType.Listening),
            Activity.CreateActivityForBot("the birds by the lake", ActivityType.Listening),

            Activity.CreateActivityForBot("with Naru", ActivityType.Playing),
            Activity.CreateActivityForBot("with Gumo", ActivityType.Playing),
            Activity.CreateActivityForBot("with Ku", ActivityType.Playing),
            Activity.CreateActivityForBot("with friends", ActivityType.Playing),

            Activity.CreateActivityForBot("the Spirit Trials", ActivityType.Competing),
            Activity.CreateActivityForBot("Combat", ActivityType.Competing),
        };

        /// <summary>The activity used for when developer mode is active.</summary>
        private static readonly Activity DeveloperModeActivity = Activity.CreateActivityForBot("Keman carefully -- Shutting down for upgrades soon!", ActivityType.Listening);

        /// <summary>The activity used when debug mode is active.</summary>
        private static readonly Activity DebugModeActivity = Activity.CreateActivityForBot("my step -- Debug Mode is active!", ActivityType.Watching);

        /// <summary>The activity for March 11.</summary>
        private static readonly Activity BirthdayActivity = Activity.CreateActivityForBot("with friends - I'm 7 years old today!", ActivityType.Playing);

        public LogLevel ProgramLogLevel { get; }

        public bool IsDebug { get; }

#if DEBUG
        public bool IsEnvironmentDebug { get; } = true;
#else
		public bool IsEnvironmentDebug { get; } = false;
#endif

        public Logger SystemLog { get; } = new Logger(new LogMessage.MessageComponent("[Bot Interface] ", EtiLogger.Data.Structs.Color.SPIRIT_BLUE, new EtiLogger.Data.Structs.Color(0x080808)));

        public const string LOG_CLEARED_MESSAGE = "\n-- Cleared console buffer. See the log file for full detail before this point. --\n";

        public void BotRun()
        {
            //	SoundPlayer = new SoundPlayer(@".\ws_npc_datachron_chat.wav");
            //SoundPlayer.Load();

           // ProgramLog.SelectionBackColor = ProgramLog.BackColor;
            //ProgramLog.SelectionColor = Color.White;

           // Logger.DefaultTarget = new CustomConsoleRelay(ProgramLog, this);
          //  Logger.SetAllLoggerTargetsTo(Logger.DefaultTarget);

           // IsDebug = Environment.GetCommandLineArgs().Contains("debug") || IsEnvironmentDebug;
           // ProgramLogLevel = IsDebug ? LogLevel.Debug : LogLevel.Info;

            if (Environment.MachineName.ToLower() == "xan" && Directory.Exists(@"V:\"))
            {
                Logger.Default.WriteLine("Using V:\\ net drive.");
                //DataPersistence.PersistenceRootFolder = new DirectoryInfo(@"V:\EtiBotCore");
                Personality.Current = new Personality(@"V:\EtiBotCore\Ori.personality");
            }
            else
            {
                Logger.Default.WriteLine("Using C:\\ drive.");
                //DataPersistence.PersistenceRootFolder = new DirectoryInfo(@"C:\EtiBotCore");
                Personality.Current = new Personality(@"C:\EtiBotCore\Ori.personality");
            }

            DataPersistence.DoStaticInit();

#if DEBUG
            Logger.LoggingLevel = LogLevel.Trace;
            //BoxVerboseLog.CheckState = CheckState.Checked;
#else
			Logger.LoggingLevel = ProgramLogLevel;
			//BoxVerboseLog.CheckState = ProgramLogLevel == LogLevel.Debug ? CheckState.Indeterminate : CheckState.Unchecked;
#endif
            _ = MainAsync();
        }

        public async Task MainAsync()
        {
            DiscordClient sys = null;
            try
            {

                await DiscordClient.Setup();

                // what da bot gonna do fo today ####
                GatewayIntent intents =
                    GatewayIntent.DIRECT_MESSAGES |
                    GatewayIntent.GUILDS |
                    GatewayIntent.GUILD_PRESENCES |
                    GatewayIntent.GUILD_BANS |
                    GatewayIntent.GUILD_MEMBERS |
                    GatewayIntent.GUILD_MESSAGES |
                    GatewayIntent.GUILD_MESSAGE_REACTIONS |
                    GatewayIntent.GUILD_VOICE_STATES;

                DiscordClient.Log.Target = Logger.DefaultTarget;
                DiscordClient.LoggingLevel = LogLevel.Trace;
                string token;
                if (Directory.Exists("V:\\"))
                {
                    token = File.ReadAllText(@"V:\EtiBotCore\token.txt");
                }
                else
                {
                    token = File.ReadAllText(@"C:\EtiBotCore\token.txt");
                }

                sys = new DiscordClient(token, intents)
                {
                    ReconnectOnFailure = true,
                    DevMode = IsEnvironmentDebug
                };

                BotContextRegistry.InitializeBotContexts();

                CommandMarshaller.Initialize();
               // sys.Events.MessageEvents.OnMessageCreated += OnMessageCreated;

                await sys.ConnectAsync();

                DiscordClient.RefreshActivity = UpdateStatus;
                _ = Task.Run(async () => {
                    await Task.Delay(5000);
                    while (true)
                    {
                        if (sys.Connected)
                        {
                            UpdateStatus();
                        }
                        await Task.Delay(TimeSpan.FromMinutes(2.5));
                    }
                });
            }
            catch (Exception genericExc)
            {
                await sys?.DisconnectAsync();
                Logger.Default.WriteException(genericExc);
            }
        }

        private void UpdateStatus()
        {
            if (DiscordClient.Current.DevMode)
            {
                DiscordClient.Current.SetActivity(DeveloperModeActivity, StatusType.DoNotDisturb);
            }
            else if (IsDebug)
            {
                DiscordClient.Current.SetActivity(DebugModeActivity, StatusType.Idle);
            }
            else
            {
                DateTime now = DateTime.Now;
                if (now.Day == 11 && now.Month == 3)
                {
                    DiscordClient.Current.SetActivity(BirthdayActivity, StatusType.Online);
                }
                else
                {
                    DiscordClient.Current.SetActivity(Activities.Random(), StatusType.Online);
                }
            }
        }

        //private Task OnMessageCreated(EtiBotCore.DiscordObjects.Guilds.ChannelData.Message message, bool? pinned)
        //{
        //    if (message.Channel is DMChannel) return Task.CompletedTask;
        //    if (message.Mentions.Length == 0) return Task.CompletedTask;
        //    if (message.Mentions.Where(user => user.ID == 114163433980559366).Count() > 0)
        //    {
        //        if (PingSoundOption_Me.Checked) SoundPlayer.Play();
        //    }
        //    else if (message.Mentions.Where(user => user.IsSelf).Count() > 0)
        //    {
        //        if (PingSoundOption_Bot.Checked) SoundPlayer.Play();
        //    }
        //    return Task.CompletedTask;
        //}

        //private void SubmitCommand()
        //{
        //    // Enter was pressed.
        //    string cmd = CommandEntryBox.Text;
        //    cmd = cmd.Replace("\r", "");
        //    while (cmd.StartsWith("\n"))
        //    {
        //        cmd = cmd[1..];
        //    }
        //    cmd = cmd.Replace("\n", " ").Trim();
        //    if (string.IsNullOrWhiteSpace(cmd)) return;
        //    CommandEntryBox.Text = "";

        //    SystemLog.WriteLine($"§6CONSOLE ISSUED >> §2{cmd}");
        //    _ = CommandMarshaller.ParseConsole(cmd);

        //    if (BoxScrollAlways.Checked)
        //    {
        //        ProgramLog.SelectionStart = ProgramLog.TextLength;
        //        ProgramLog.ScrollToCaret();
        //    }
        //}

        //private void OnKeyDownCmdEntry(object sender, KeyEventArgs e)
        //{
        //    if (e.KeyCode == Keys.Return)
        //    {
        //        SubmitCommand();
        //    }
        //}

        //private void BtnSendCommand_Click(object sender, EventArgs e)
        //{
        //    SubmitCommand();
        //}

        //private void BtnClearLog_Click(object sender, EventArgs e)
        //{
        //    ProgramLog.Clear();
        //    ProgramLog.SelectionStart = 0;
        //    ProgramLog.SelectionBackColor = ProgramLog.BackColor;
        //    ProgramLog.SelectionColor = Color.White;
        //}

        //private void ProgramLog_LinkClicked(object sender, LinkClickedEventArgs e)
        //{
        //    System.Diagnostics.Process.Start(e.LinkText);
        //}

        #region Scroll Hooks
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
        private const int WM_VSCROLL = 277;
        private const int SB_PAGEBOTTOM = 7;

        //internal static void ScrollToBottom(RichTextBox richTextBox)
        //{
        //    SendMessage(richTextBox.Handle, WM_VSCROLL, (IntPtr)SB_PAGEBOTTOM, IntPtr.Zero);
        //    richTextBox.SelectionStart = richTextBox.Text.Length;
        //}
        #endregion

        //private void ProgramLog_TextChanged(object sender, EventArgs e)
        //{
        //    if (ProgramLog.Text == LOG_CLEARED_MESSAGE) return;
        //    if (ProgramLog.TextLength > ushort.MaxValue)
        //    {
        //        ProgramLog.Text = LOG_CLEARED_MESSAGE;
        //    }
        //    if (BoxScrollAlways.Checked) ScrollToBottom(ProgramLog);
        //}

        //private void BoxVerboseLog_CheckStateChanged(object sender, EventArgs e)
        //{
        //    //Logger.LoggingLevel =  ? LogLevel.Trace : ProgramLogLevel;
        //    if (BoxVerboseLog.CheckState == CheckState.Unchecked)
        //    {
        //        Logger.LoggingLevel = LogLevel.Info;
        //        SystemLog.WriteLine("§fNow writing §8INFO §flevel logs (and above)");
        //    }
        //    else if (BoxVerboseLog.CheckState == CheckState.Indeterminate)
        //    {
        //        Logger.LoggingLevel = LogLevel.Debug;
        //        SystemLog.WriteLine("§fNow writing §6DEBUG §flevel logs (and above)");
        //    }
        //    else
        //    {
        //        Logger.LoggingLevel = LogLevel.Trace;
        //        SystemLog.WriteLine("§fNow writing §dTRACE §flevel logs (and above)");
        //    }
        //}
    }
}
