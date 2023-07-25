using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OldOriBot.Utility.Music.FileRepresentation {

	/// <summary>
	/// Represents a music file.
	/// </summary>
	public class MusicFile : IEquatable<MusicFile> {

		/// <summary>
		/// The configuration value determining if the votenext command can be used to select disabled songs. If true, <see cref="Enabled"/> is ignored and the track will always play so long as it's manually selected.
		/// </summary>
		public static bool AllowManualSelectionOfDisabledTracks => MusicController.Configuration.TryGetType("AllowManualSelectionOfDisabledTracks", false);

		/// <summary>
		/// A reference to the file itself.
		/// </summary>
		public FileInfo File { get; }

		/// <summary>
		/// The metadata for this music file.
		/// </summary>
		public AudioMetadata Metadata { get; }
		
		/// <summary>
		/// The <see cref="MusicDirectory"/> that contains this <see cref="MusicFile"/>, or null if one was not specified.
		/// </summary>
		public MusicDirectory ParentMusicDirectory { get; }

		/// <summary>
		/// True if this music file can be played in the radio channel. This will be false if <see cref="DirectoryEnabled"/> is false, or if the song is in the exclusion list.
		/// </summary>
		public bool Enabled => DirectoryEnabled && _EnabledFromIgnoreList;

		/// <summary>
		/// When instantiated, an enabled property is passed in. This is that value.
		/// </summary>
		private readonly bool _EnabledFromIgnoreList;

		/// <summary>
		/// True if <see cref="ParentMusicDirectory"/> is not null and its Enabled property is true.<para/>
		/// Always true if <see cref="ParentMusicDirectory"/> is null.
		/// </summary>
		public bool DirectoryEnabled => ParentMusicDirectory?.Enabled ?? true;

		/// <summary>
		/// Construct a new <see cref="MusicFile"/> from the given <see cref="FileInfo"/>, acquiring its metadata.
		/// </summary>
		/// <param name="audioFile"></param>
		/// <exception cref="FileNotFoundException">If the file does not exist.</exception>
		public MusicFile(FileInfo audioFile, MusicDirectory parentDir = null, bool isEnabled = true) {
			if (audioFile == null) throw new ArgumentException("Audio File was null!");
			if (!audioFile.Exists) throw new FileNotFoundException();

			File = audioFile;
			Metadata = AudioMetadata.For(audioFile);
			ParentMusicDirectory = parentDir;
			_EnabledFromIgnoreList = isEnabled;
		}

		/// <summary>
		/// An alias method that convers a <see cref="FileInfo"/> array into a <see cref="MusicFile"/> array
		/// </summary>
		/// <param name="files"></param>
		/// <returns></returns>
		public static MusicFile[] FromFiles(FileInfo[] files, MusicDirectory parentDir = null) {
			MusicFile[] music = new MusicFile[files.Length];
			for (int idx = 0; idx < files.Length; idx++) {
				music[idx] = new MusicFile(files[idx], parentDir);
			}
			return music;
		}

		public static bool operator ==(MusicFile left, MusicFile right) {
			if (left is MusicFile) return left.Equals(right);
			if (right is MusicFile) return right.Equals(left);
			return ReferenceEquals(left, right);
		}

		public static bool operator !=(MusicFile left, MusicFile right) => !(left == right);

		public bool Equals(MusicFile other) {
			if (other == null) return false;
			if (ReferenceEquals(this, other)) return true;
			return File == other.File;
		}

		public override bool Equals(object obj) => obj is MusicFile musFile ? Equals(musFile) : ReferenceEquals(this, obj);

		public override int GetHashCode() => HashCode.Combine(File);
	}
}
