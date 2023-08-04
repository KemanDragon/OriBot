using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Utility.Extension;

namespace OldOriBot.Utility.Music.FileRepresentation {
	/// <summary>
	/// Represents a directory on Xan's filesystem storing music for Ori.
	/// </summary>
	public class MusicDirectory : IEquatable<MusicDirectory> {

		/*
		public const string ORI_CLASSIC_OST = "https://store.steampowered.com/app/465980/Ori_and_the_Blind_Forest_Original_Soundtrack/";
		public const string ORI_CLASSIC_OST_STEAM = "steam://store/465980";

		public const string ORI_DE_OST = "https://store.steampowered.com/app/466390/Ori_and_the_Blind_Forest_Additional_Soundtrack/";
		public const string ORI_DE_OST_STEAM = "steam://store/466390";

		public const string ORI_WOTW_OST = "https://store.steampowered.com/app/1258740/Ori_and_the_Will_of_the_Wisps_Soundtrack/";
		public const string ORI_WOTW_OST_STEAM = "steam://store/1258740";
		*/

		/// <summary>
		/// True if a <see cref="MusicPool"/> is allowed to select music from this <see cref="MusicDirectory"/>, false if it should be excluded.
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// The parent <see cref="MusicPool"/>, which will be set if this is passed into a <see cref="MusicPool"/>'s constructor.
		/// </summary>
		public MusicPool ParentPool { get; set; } = null;
		
		/// <summary>
		/// The directory this references.
		/// </summary>
		public DirectoryInfo Location { get; set; }

		/// <summary>
		/// The extension of all media files in this directory, including the preceeding period.
		/// </summary>
		public string Extension { get; set; }

		/// <summary>
		/// The name of the place the user can download or buy this music.
		/// </summary>
		public string Source { get; }

		/// <summary>
		/// The identity or identities of this <see cref="MusicDirectory"/> that is used on the [music votenext] command.
		/// </summary>
		public string[] Identities { get; }

		/// <summary>
		/// The user-friendly display name of this <see cref="MusicDirectory"/>
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Where the user can download or buy this music.
		/// </summary>
		public string Download { get; }

		/// <summary>
		/// A reference to every allowed music file in this directory.<para/>
		/// "Allowed" refers to a file that is Enabled, or, if OriBotIgnoreList.txt exists in the directory, it must not be listed in this text document (If it is listed here, it is disabled, and not included in this array)<para/>
		///  This ignores <see cref="MusicFile.AllowManualSelectionOfDisabledTracks"/>, so tracks that are disabled will still not be a part of this list regardless of the config value.
		/// </summary>
		public MusicFile[] MusicFiles {
			get {
				if (_Music == null) {
					_Music = AllMusicFiles.Where(music => music.Enabled == true).ToArray();
				}
				return _Music;
			}
		}

		/// <summary>
		/// Returns every <see cref="MusicFile"/> in this directory, even ones that are disabled.
		/// </summary>
		public MusicFile[] AllMusicFiles {
			get {
				if (_AllMusicEvenExcluded == null) {
					FileInfo[] files = Location.GetFiles("*" + Extension);
					string[] oriBotIgnoreList = null;
					if (File.Exists(Location.FullName + "\\OriBotIgnoreList.txt")) {
						oriBotIgnoreList = File.ReadAllLines(Location.FullName + "\\OriBotIgnoreList.txt");
					}

					if (oriBotIgnoreList != null) {
						//List<FileInfo> newFiles = new List<FileInfo>(files.Length);
						List<MusicFile> newMusic = new List<MusicFile>(files.Length);
						foreach (FileInfo file in files) {
							bool shouldAdd = true;
							foreach (string fileName in oriBotIgnoreList) {
								if (file.Name == fileName) {
									shouldAdd = false;
									break;
								}
							}
							//if (shouldAdd) newFiles.Add(file);
							newMusic.Add(new MusicFile(file, this, shouldAdd));
						}
						_AllMusicEvenExcluded = newMusic.ToArray();
					} else {
						_AllMusicEvenExcluded = MusicFile.FromFiles(files, this);
					}
				}
				return _AllMusicEvenExcluded;
			}
		}

		private MusicFile[] _Music = null;

		private MusicFile[] _AllMusicEvenExcluded = null;

		/// <summary>
		/// Construct a new <see cref="MusicDirectory"/> at the given location and with the given IDs.
		/// </summary>
		/// <param name="location">The directory that the music is stored in.</param>
		/// <param name="ids">The IDs of this category, used for voting on categories.</param>
		/// <param name="name">The display name of this category.</param>
		/// <param name="targetExtension">The extension used by files of this directory.</param>
		/// <param name="enabled">Whether or not this <see cref="MusicDirectory"/> can be used in the selection of music.</param>
		public MusicDirectory(DirectoryInfo location, string[] ids, string name, string targetExtension = ".mp3", bool enabled = true) {
			Location = location;
			Extension = targetExtension;
			Identities = ids;
			Name = name;
			FileInfo dirInfo = new FileInfo(Location.FullName + @"\" + "__INFO.TXT");
			if (dirInfo.Exists) {
				string[] lines = File.ReadAllLines(dirInfo.FullName);
				if (lines.Length >= 1) {
					Source = lines[0];
				} else {
					Source = Location.Name + " (Local Filesystem)";
				}
				if (lines.Length >= 2) {
					Download = "**Option 1:** " + lines[1];
				} else {
					Download = "None";
				}
				if (lines.Length >= 3) {
					Download += "\n";
					if (!string.IsNullOrEmpty(lines[2])) {
						Download += "**Option 2:** " + lines[2];
					}
				}
				if (lines.Length >= 4) {
					string all = "";
					foreach (string line in lines.Skip(2)) {
						all += line + "\n";
					}
					foreach (MusicFile file in MusicFiles) {
						if (file.Metadata.ExtraNotes != null) {
							file.Metadata.ExtraNotes = all;
						}
					}
				}
			} else {
				Source = Location.Name + " (Local Filesystem)";
				Download = "None";
			}
			Enabled = enabled;
		}

		/// <summary>
		/// Construct a new <see cref="MusicDirectory"/> at the given location and with a singular ID.
		/// </summary>
		/// <param name="location">The directory that the music is stored in.</param>
		/// <param name="id">The ID of this category, used for voting on categories.</param>
		/// <param name="name">The display name of this category.</param>
		/// <param name="targetExtension">The extension used by files of this directory.</param>
		/// <param name="enabled">Whether or not this <see cref="MusicDirectory"/> can be used in the selection of music.</param>
		public MusicDirectory(DirectoryInfo location, string id, string name, string targetExtension = ".mp3", bool enabled = true)
			: this(location, new string[] { id }, name, targetExtension, enabled) { }
	
		/// <summary>
		/// Returns a random song from this <see cref="MusicDirectory"/>. If <see cref="ParentPool"/> is not null, it will call <see cref="MusicPool.GetRandomFileFromDirectory(MusicDirectory, bool)"/> with this <see cref="MusicDirectory"/> as its input parameter.
		/// </summary>
		/// <returns></returns>
		public MusicFile GetRandomSong() {
			if (ParentPool != null) {
				return ParentPool.GetRandomFileFromDirectory(this, true, MusicFile.AllowManualSelectionOfDisabledTracks);
			}
			return MusicFiles.Random();
		}

		public static bool operator ==(MusicDirectory left, MusicDirectory right) {
			if (left is MusicDirectory) return left.Equals(right);
			if (right is MusicDirectory) return right.Equals(left);
			return ReferenceEquals(left, right);
		}

		public static bool operator !=(MusicDirectory left, MusicDirectory right) => !(left == right);

		public bool Equals(MusicDirectory other) {
			if (other == null) return false;
			if (ReferenceEquals(this, other)) return true;
			if (Location == other.Location) return true;
			return false;
		}

		public override bool Equals(object obj) => obj is MusicDirectory dir ? Equals(dir) : ReferenceEquals(this, obj);

		public override int GetHashCode() => HashCode.Combine(Location, Extension);
	}
}
