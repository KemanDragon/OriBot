using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using EtiLogger.Logging;

using OldOriBot.Data.Persistence;
using OldOriBot.Interaction;
using OldOriBot.Utility.Extensions;

namespace OldOriBot.UserProfiles
{
    internal class ProfileVersionMarshaller
    {
        private static readonly Logger VersionLogger = new Logger("§9[User Profile System :: Version Controller] ")
        {
            NoLevel = true
        };

        /// <summary>
        /// Returns a new instance of the latest profile reader/writer
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public static ProfileVersion GetLatestHandler(UserProfile profile)
        {
            //return new ProfileV4(profile);
            Type profileVersionBindingType = ProfileVersionBindings.Last().Value;
            ConstructorInfo ctor = profileVersionBindingType.GetConstructor(new Type[] { typeof(UserProfile) });
            return (ProfileVersion)ctor.Invoke(new object[] { profile });
        }

        private static Dictionary<int, Type> ProfileVersionBindings => new Dictionary<int, Type>
        {
            /*
			[1] = typeof(ProfileV1),
			[2] = typeof(ProfileV2),
			[3] = typeof(ProfileV3),
			[4] = typeof(ProfileV4),
			[5] = typeof(ProfileV5),
			[6] = typeof(ProfileV6),
			*/
            [7] = typeof(ProfileV7),
            [8] = typeof(ProfileV8)
        };

        /// <summary>
        /// Returns a new instance of the profile reader/writer that corresponds with the given byte data. The input <see cref="UserProfile"/> should be a blank profile object freshly created.
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ProfileVersion GetProfileVersionHandlerFor(UserProfile profile, byte[] data)
        {
            if (data.Length < 8)
            {
                VersionLogger.WriteLine("§3Loading profile V7 -- data.Length < 8!...?", LogLevel.Trace);
                return new ProfileV7(profile)
                {
                    Faulted = true
                };
            }
            if (GetHeader(data) != "PROF")
            {
                VersionLogger.WriteLine("§3Loading profile V7.", LogLevel.Trace);
                return new ProfileV7(profile)
                {
                    Faulted = true
                };
            }
            int version = BitConverter.ToInt32(data, 4);
            if (ProfileVersionBindings.ContainsKey(version))
            {
               // VersionLogger.WriteLine("§3Loading profile version: " + version, LogLevel.Trace);
                ConstructorInfo ctor = ProfileVersionBindings[version].GetConstructor(new Type[] { typeof(UserProfile) });
                return (ProfileVersion)ctor.Invoke(new object[] { profile });
            }
            else
            {
                // throw new InvalidOperationException("The specified version does not have a handler in the current version of this marshaller.");
              //  VersionLogger.WriteWarning($"User attempted to load profile version {version}! This version is obsolete (or corrupt), and so their data will be wiped.");
                return new ProfileV7(profile);
            }
        }

        /// <summary>
        /// Gets a 4 byte header
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GetHeader(byte[] data)
        {
            string head = "";
            for (int i = 0; i < 4; i++)
            {
                head += (char)data[i];
            }
            return head;
        }

        public abstract class ProfileVersion : IByteSerializable
        {
            /// <summary>
            /// A reference to the <see cref="UserProfile"/> that is using this version controller.
            /// </summary>
            public abstract UserProfile Profile { get; }

            /// <summary>
            /// <see langword="true"/> if this profile is faulted, which means the data stored within is corrupt and must be wiped.
            /// </summary>
            public bool Faulted { get; internal set; }

            public abstract int FromBytes(byte[] data);

            public abstract byte[] ToBytes();

            //	public BotContext Context { get; }

            //public DataPersistence Persistence { get; }

            public ProfileVersion(BotContext ctx)
            {
                //Context = ctx;
                //Persistence = DataPersistence.GetPersistence(ctx, "userprofile.cfg");
            }

            /// <summary>
            /// The max length of a user's title.
            /// </summary>
            public int MaxTitleLength => 250;

            /// <summary>
            /// The max length of a user's description.
            /// </summary>
            public int MaxDescriptionLength => 800;

            /// <summary>
            /// The maximum amount of newlines (\n) in a user's title.
            /// </summary>
            public int MaxTitleNewlines => 8;

            /// <summary>
            /// The maximum amount of newlines (\n) in a user's description.
            /// </summary>
            public int MaxDescNewlines => 24;

            public string ClipString(string str, int maxFieldLength = 800)
            {
                if (str == null || str?.Length == 0 || str == default) return str;
                return str.Substring(0, Math.Min(str.Length, maxFieldLength));
            }
        }

        private class ProfileV7 : ProfileVersion
        {
            public const int VERSION = 7;

            public override UserProfile Profile { get; }

            public ProfileV7(UserProfile profile) : base(profile.Context)
            {
                Profile = profile;
            }

            public override int FromBytes(byte[] data)
            {
                Profile.Version = VERSION;
                //data = data.Skip(8).ToArray();

                using MemoryStream memory = new MemoryStream(data);
                long start = memory.Position;
                memory.Position += 8;
                using BinaryReader reader = new BinaryReader(memory);
                // prof and version skipped
                long pos = memory.Position;

                Profile.HasFieldLimitBypass = reader.ReadByte() == 1;
                Profile._Title = reader.ReadStringSL();
                Profile._Description = reader.ReadStringSL();
                Profile.Experience = reader.ReadDouble();
                Profile.MessagesSent = reader.ReadUInt32();

                byte r, g, b;
                r = reader.ReadByte();
                g = reader.ReadByte();
                b = reader.ReadByte();
                Profile._Color = (r << 16) | (g << 8) | b;

                int numBadges = reader.ReadInt32();
                VersionLogger.WriteLine("Position: " + memory.Position, LogLevel.Trace);
                VersionLogger.WriteLine("Badges: " + numBadges, LogLevel.Trace);
                for (int i = 0; i < numBadges; i++)
                {
                    int badgeSize = reader.ReadInt32();
                    VersionLogger.WriteLine("Size: " + badgeSize, LogLevel.Trace);
                    Badge badge = new Badge();
                    badge.FromBytes(reader.ReadBytes(badgeSize));

                    // We need to auto-update badges.
                    if (UserProfile.AutoUpdateBadges)
                    {
                        Badge predefined = BadgeRegistry.GetBadgeFromPredefinedRegistry(badge.Name);
                        if (predefined != null)
                        {
                            badge.Description = predefined.Description;
                            badge.MiniDescription = predefined.MiniDescription;
                            badge.Icon = predefined.Icon;
                        }
                    }

                    Profile.BadgesInternal.Add(badge);
                }

                Profile.UserData.FromBytesInExistingStream(reader);

                // Populate defaultvalues.
                foreach (KeyValuePair<string, object> defaultData in UserProfile.DefaultProfileConfigs)
                {
                    if (!Profile.UserData.ContainsKey(defaultData.Key))
                    {
                        Profile.UserData[defaultData.Key] = defaultData.Value;
                    }
                }

                return (int)(memory.Position - start);
            }

            public override byte[] ToBytes()
            {
                using MemoryStream memory = new MemoryStream(1024);
                using BinaryWriter writer = new BinaryWriter(memory);
                List<byte[]> badges = new List<byte[]>();
                foreach (Badge badge in Profile.BadgesInternal)
                {
                    byte[] dat = badge.ToBytes();
                    badges.Add(dat);
                }

                //writer.Write("PROF");
                writer.WriteAsChars("PROF");
                writer.Write(VERSION);
                writer.Write((byte)(Profile.HasFieldLimitBypass ? 1 : 0));
                if (Profile.HasFieldLimitBypass)
                {
                    writer.WriteStringSL(Profile.Title);
                    writer.WriteStringSL(Profile.Description);
                }
                else
                {
                    writer.WriteStringSL(ClipString(Profile.Title, MaxTitleLength));
                    writer.WriteStringSL(ClipString(Profile.Description, MaxDescriptionLength));
                }

                writer.Write(Profile.Experience);
                writer.Write(Profile.MessagesSent);

                int color = Profile.Color.GetValueOrDefault(0x202225);
                byte r = (byte)((color >> 16) & 0xFF);
                byte g = (byte)((color >> 8) & 0xFF);
                byte b = (byte)(color & 0xFF);
                writer.Write(r);
                writer.Write(g);
                writer.Write(b);

                writer.Write(badges.Count);
                VersionLogger.WriteLine("Writing " + badges.Count + " badges...", LogLevel.Trace);
                foreach (byte[] badge in badges)
                {
                    writer.Write(badge.Length);
                    VersionLogger.WriteLine("Size=" + badge.Length + " bytes", LogLevel.Trace);
                    writer.Write(badge);
                }

                writer.Write(Profile.UserData.ToBytes());

                return memory.ToArray();
            }
        }

        private class ProfileV8 : ProfileVersion
        {
            public const int VERSION = 8;

            public override UserProfile Profile { get; }

            public ProfileV8(UserProfile profile) : base(profile.Context)
            {
                Profile = profile;
            }

            public override int FromBytes(byte[] data)
            {
                Profile.Version = VERSION;
                //data = data.Skip(8).ToArray();

                using MemoryStream memory = new MemoryStream(data);
                long start = memory.Position;
                memory.Position += 8;
                using BinaryReader reader = new BinaryReader(memory);
                // prof and version skipped
                long pos = memory.Position;

                Profile.HasFieldLimitBypass = reader.ReadByte() == 1;
                Profile._Title = reader.ReadStringSL();
                Profile._Description = reader.ReadStringSL();
                Profile.Experience = reader.ReadDouble();
                Profile.MessagesSent = reader.ReadUInt32();

                if (reader.ReadBoolean())
                {
                    byte r, g, b;
                    r = reader.ReadByte();
                    g = reader.ReadByte();
                    b = reader.ReadByte();
                    Profile._Color = (r << 16) | (g << 8) | b;
                }

                int numBadges = reader.ReadInt32();
                //VersionLogger.WriteLine("Position: " + memory.Position, LogLevel.Trace);
              //  VersionLogger.WriteLine("Badges: " + numBadges, LogLevel.Trace);
                for (int i = 0; i < numBadges; i++)
                {
                    int badgeSize = reader.ReadInt32();
                //    VersionLogger.WriteLine("Size: " + badgeSize, LogLevel.Trace);
                    Badge badge = new Badge();
                    badge.FromBytes(reader.ReadBytes(badgeSize));

                    // We need to auto-update badges.
                    if (UserProfile.AutoUpdateBadges)
                    {
                        Badge predefined = BadgeRegistry.GetBadgeFromPredefinedRegistry(badge.Name);
                        if (predefined != null)
                        {
                            badge.Description = predefined.Description;
                            badge.MiniDescription = predefined.MiniDescription;
                            badge.Icon = predefined.Icon;
                            if (badge.ExperienceWorth == 0)
                            {
                                badge.ExperienceWorth = predefined.ExperienceWorth;
                            }
                        }
                    }

                    Profile.BadgesInternal.Add(badge);
                }

                Profile.UserData.FromBytesInExistingStream(reader);

                // Populate defaultvalues.
                foreach (KeyValuePair<string, object> defaultData in UserProfile.DefaultProfileConfigs)
                {
                    if (!Profile.UserData.ContainsKey(defaultData.Key))
                    {
                        Profile.UserData[defaultData.Key] = defaultData.Value;
                    }
                }

                return (int)(memory.Position - start);
            }

            public override byte[] ToBytes()
            {
                using MemoryStream memory = new MemoryStream(1024);
                using BinaryWriter writer = new BinaryWriter(memory);
                List<byte[]> badges = new List<byte[]>();
                foreach (Badge badge in Profile.BadgesInternal)
                {
                    byte[] dat = badge.ToBytes();
                    badges.Add(dat);
                }

                //writer.Write("PROF");
                writer.WriteAsChars("PROF");
                writer.Write(VERSION);
                writer.Write((byte)(Profile.HasFieldLimitBypass ? 1 : 0));
                if (Profile.HasFieldLimitBypass)
                {
                    writer.WriteStringSL(Profile.Title);
                    writer.WriteStringSL(Profile.Description);
                }
                else
                {
                    writer.WriteStringSL(ClipString(Profile.Title, MaxTitleLength));
                    writer.WriteStringSL(ClipString(Profile.Description, MaxDescriptionLength));
                }

                writer.Write(Profile.Experience);
                writer.Write(Profile.MessagesSent);

                if (Profile.Color != null)
                {
                    writer.Write(true);
                    int color = Profile.Color.Value;
                    byte r = (byte)((color >> 16) & 0xFF);
                    byte g = (byte)((color >> 8) & 0xFF);
                    byte b = (byte)(color & 0xFF);
                    writer.Write(r);
                    writer.Write(g);
                    writer.Write(b);
                }
                else
                {
                    writer.Write(false);
                }

                writer.Write(badges.Count);
                VersionLogger.WriteLine("Writing " + badges.Count + " badges...", LogLevel.Trace);
                foreach (byte[] badge in badges)
                {
                    writer.Write(badge.Length);
                    VersionLogger.WriteLine("Size=" + badge.Length + " bytes", LogLevel.Trace);
                    writer.Write(badge);
                }

                writer.Write(Profile.UserData.ToBytes());

                return memory.ToArray();
            }
        }
    }
}