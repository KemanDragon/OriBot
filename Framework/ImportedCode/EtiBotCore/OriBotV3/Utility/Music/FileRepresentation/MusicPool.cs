using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Utility.Extension;
using OldOriBot.Utility.Enumerables;

namespace OldOriBot.Utility.Music.FileRepresentation {

	/// <summary>
	/// Represents a large pool of songs from a collection of <see cref="MusicDirectory"/> instances.
	/// </summary>
	public class MusicPool {

		/// <summary>
		/// Every <see cref="MusicDirectory"/> stored in this pool.
		/// </summary>
		public IReadOnlyList<MusicDirectory> MusicDirectories => _MusicDirectories.AsReadOnly();

		/// <summary>
		/// Every single <see cref="MusicFile"/> available in this pool. This includes disabled files. Please check for this.
		/// </summary>
		public IReadOnlyList<MusicFile> AllMusicFiles => _AllMusicFiles.AsReadOnly();

		/// <summary>
		/// A list of <see cref="MusicFile"/>s that have recently played and cannot be selected.
		/// </summary>
		public LimitedSpaceArray<MusicFile> RecentlySelectedSongs { get; }

		/// <summary>
		/// Every <see cref="MusicDirectory"/> stored in this pool.
		/// </summary>
		private readonly List<MusicDirectory> _MusicDirectories = new List<MusicDirectory>();

		/// <summary>
		/// Every single <see cref="MusicFile"/> available in this pool. This includes disabled files. Please check for this.
		/// </summary>
		private readonly List<MusicFile> _AllMusicFiles = new List<MusicFile>();

		/// <summary>
		/// Construct a new <see cref="MusicPool"/> from the given <see cref="MusicDirectory"/> instances.
		/// </summary>
		/// <param name="directories"></param>
		public MusicPool(int recentSongCacheSize, params MusicDirectory[] directories) {
			_MusicDirectories = directories.ToList();
			foreach (MusicDirectory dir in directories) {
				foreach (MusicFile file in dir.AllMusicFiles) {
					_AllMusicFiles.Add(file);
				}
				dir.ParentPool = this;
			}
			RecentlySelectedSongs = new LimitedSpaceArray<MusicFile>(recentSongCacheSize);
		}

		public MusicDirectory this[int index] {
			get {
				return _MusicDirectories[index];
			}
		}

		/// <summary>
		/// Returns the <see cref="MusicDirectory"/> that corresponds to the given <see cref="DirectoryInfo"/>
		/// </summary>
		/// <param name="targetDir"></param>
		/// <returns></returns>
		public MusicDirectory GetMusicDirectory(DirectoryInfo targetDir) {
			return _MusicDirectories.Where(dir => dir.Location.FullName == targetDir.FullName).FirstOrDefault(predicate: null);
		}

		/// <summary>
		/// Adds the given <see cref="MusicFile"/> to the list of recently played tracks, even if it's already in the list.
		/// </summary>
		/// <param name="file"></param>
		public void AddToExclusionList(MusicFile file) {
			RecentlySelectedSongs.Add(file);
		}

		/// <summary>
		/// Returns a random <see cref="MusicFile"/> from the pool, respecting the Enabled property of the music files.
		/// </summary>
		/// <returns></returns>
		public MusicFile GetRandomFileFromPool(bool addToExclusionList = true) {
			IEnumerable<MusicFile> files = _AllMusicFiles.Where(music => music.Enabled == true).Where(music => !RecentlySelectedSongs.Contains(music));
			MusicFile selection = files.Random();
			if (addToExclusionList) RecentlySelectedSongs.Add(selection);
			return selection;
		}

		/// <summary>
		/// Returns a random <see cref="MusicFile"/> from the given <see cref="MusicDirectory"/>.<para/>
		/// This will attempt to avoid returning tracks that have already played, but if this is not possible (for instance, the category has less tracks than the number of unique tracks required, such as the WotW Trailer category), then it will return one of the songs that has played already anyway.
		/// </summary>
		/// <param name="targetDirectory">The <see cref="MusicDirectory"/> to search.</param>
		/// <param name="addToExclusionList">If true, this song will be added to the exclusion list.</param>
		/// <param name="allowAllMusicFiles">If true, all music files (even disabled ones) can be picked.</param>
		/// <returns></returns>
		public MusicFile GetRandomFileFromDirectory(MusicDirectory targetDirectory, bool addToExclusionList = true, bool allowAllMusicFiles = false) {
			//IEnumerable<MusicFile> enabledFiles = targetDirectory.MusicFiles.Where(music => music.Enabled == true);

			IEnumerable<MusicFile> files;
			if (allowAllMusicFiles) {
				files = targetDirectory.AllMusicFiles.Where(music => !RecentlySelectedSongs.Contains(music));
			} else {
				files = targetDirectory.MusicFiles.Where(music => !RecentlySelectedSongs.Contains(music));
			}

			MusicFile selection = null;
			if (files.Count() == 0) {
				if (targetDirectory.MusicFiles.Length == 0) {
					return null;
				}
				selection = targetDirectory.MusicFiles.Random();
			} else {
				selection = files.Random();
			}
			if (selection != null && addToExclusionList) RecentlySelectedSongs.Add(selection);
			return selection;
		}

		/// <summary>
		/// Attempts to locate the given file's associated <see cref="MusicFile"/> if it exists in this pool. Returns a new instance of <see cref="MusicFile"/> if it cannot be found.<para/>
		/// This does not check if the music file is disabled and may return a disabled music file.
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public MusicFile FindOrCreateNew(FileInfo file) {
			foreach (MusicFile musFile in _AllMusicFiles) {
				if (musFile.File.FullName == file.FullName) {
					return musFile;
				}
			}
			return new MusicFile(file);
		}

		public MusicFile TryGetFromTitleOrName(string titleOrFilename, out bool isLimitedTrack, out bool hasMoreThanOnePossible) {
			// PercentageLevenshteinDistance.GetSimilarityPercentage
			titleOrFilename = titleOrFilename.ToLower();
			Dictionary<MusicFile, double> similarityComparisons = new Dictionary<MusicFile, double>();

			// So first things first I want to try a direct match.
			foreach (MusicFile musFile in _AllMusicFiles) {
				string fileNameLower = musFile.File.Name.ToLower().Substring(0, musFile.File.Name.Length - musFile.File.Extension.Length);
				string titleLower = musFile.Metadata.Title.ToLower();
				string lTitleOrFile = titleOrFilename;
				if (titleOrFilename.EndsWith(musFile.File.Extension.ToLower())) {
					lTitleOrFile = titleOrFilename.Substring(0, titleOrFilename.Length - musFile.File.Extension.Length);
				}
				if (fileNameLower == lTitleOrFile || titleLower == lTitleOrFile) {
					// This is a direct match to an exact filename or title.
					// Just return it directly.
					if ((!musFile.Enabled && MusicFile.AllowManualSelectionOfDisabledTracks) || musFile.Enabled) {
						isLimitedTrack = false;
						hasMoreThanOnePossible = false;
						return musFile;
					}
				}
			}

			foreach (MusicFile musFile in _AllMusicFiles) {
				string fileNameLower = musFile.File.Name.ToLower().Replace(musFile.File.Extension.ToLower(), "");
				string titleLower = musFile.Metadata.Title.ToLower();
				if (fileNameLower.Contains(titleOrFilename)) {
					//possibleMusic.Add(musFile);
					similarityComparisons[musFile] = PercentageLevenshteinDistance.GetSimilarityPercentage(fileNameLower, titleOrFilename);
				} else if (titleLower.Contains(titleLower)) {
					similarityComparisons[musFile] = PercentageLevenshteinDistance.GetSimilarityPercentage(titleLower, titleOrFilename);
				}
			}

			double max = 0;
			MusicFile selection = null;
			foreach (KeyValuePair<MusicFile, double> data in similarityComparisons) {
				if (data.Value > max) {
					max = data.Value;
					selection = data.Key;
				}
			}

			isLimitedTrack = false;
			hasMoreThanOnePossible = false;
			return selection;
		}

		/// <summary>
		/// Attempts to get a <see cref="MusicFile"/> from the given title or filename. It is not case sensitive.
		/// </summary>
		/// <returns></returns>
		public MusicFile TryGetFromTitleOrName_OLD(string titleOrFilename, out bool isLimitedTrack, out bool hasMoreThanOnePossible) {
			titleOrFilename = titleOrFilename.ToLower();
			List<MusicFile> possibleMusic = new List<MusicFile>();

			// So first things first I want to try a direct match.
			foreach (MusicFile musFile in _AllMusicFiles) {
				string fileNameLower = musFile.File.Name.ToLower().Substring(0, musFile.File.Name.Length - musFile.File.Extension.Length);
				string titleLower = musFile.Metadata.Title.ToLower();
				string lTitleOrFile = titleOrFilename;
				if (titleOrFilename.EndsWith(musFile.File.Extension.ToLower())) {
					lTitleOrFile = titleOrFilename.Substring(0, titleOrFilename.Length - musFile.File.Extension.Length);
				}
				if (fileNameLower == lTitleOrFile || titleLower == lTitleOrFile) {
					// This is a direct match to an exact filename or title.
					// Just return it directly.
					if ((!musFile.Enabled && MusicFile.AllowManualSelectionOfDisabledTracks) || musFile.Enabled) {
						isLimitedTrack = false;
						hasMoreThanOnePossible = false;
						return musFile;
					}
				}
			}

			foreach (MusicFile musFile in _AllMusicFiles) {
				string fileNameLower = musFile.File.Name.ToLower().Replace(musFile.File.Extension.ToLower(), "");
				string titleLower = musFile.Metadata.Title.ToLower();
				if (fileNameLower.Contains(titleOrFilename) || titleLower.Contains(titleOrFilename)) {
					possibleMusic.Add(musFile);
				}
			}

			if (possibleMusic.Count == 1) {
				// Definitely one track.
				MusicFile track = possibleMusic.First();
				if (!track.Enabled && !MusicFile.AllowManualSelectionOfDisabledTracks) {
					isLimitedTrack = true;
					hasMoreThanOnePossible = false;
					return null;
				} else {
					isLimitedTrack = false;
					hasMoreThanOnePossible = false;
					return track;
				}
			} else if (possibleMusic.Count > 1) {
				// More than one.
				// BUT: Are some of them disabled (and uncounted anyway)?
				IEnumerable<MusicFile> possibleMusic2 = possibleMusic.Where(music => music.Enabled || MusicFile.AllowManualSelectionOfDisabledTracks);

				if (possibleMusic2.Count() == 1) {
					isLimitedTrack = false;
					hasMoreThanOnePossible = false;
					return possibleMusic2.First();
				} else if (possibleMusic2.Count() > 1) {
					isLimitedTrack = false;
					hasMoreThanOnePossible = true;
					return null;
				}
			}
			isLimitedTrack = false;
			hasMoreThanOnePossible = false;
			return null;
		}
	}
}
