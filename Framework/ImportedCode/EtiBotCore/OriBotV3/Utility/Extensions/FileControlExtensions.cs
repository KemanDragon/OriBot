using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EtiBotCore.Data.Structs;

namespace OldOriBot.Utility.Extensions {
	public static class FileControlExtensions {

		/// <summary>
		/// Moves this <see cref="FileInfo"/> to a backup file of the given <paramref name="newName"/>. If <paramref name="newName"/> is <see langword="null"/>, this will simply add "-bak" onto the end of the file's name.<para/>
		/// If a backup file with the given name exists already, it will be deleted and replaced by this one.<para/>
		/// Returns a reference to the new backup file.
		/// </summary>
		/// <param name="file">The file to backup.</param>
		/// <param name="newName">The name of the backup file. Note that this is a literal name, <strong>not a path.</strong></param>
		/// <returns>A reference to the backup file.</returns>
		/// <exception cref="ArgumentNullException">If the file is null.</exception>
		/// <inheritdoc cref="FileInfo"/>
		/// <inheritdoc cref="File.ReadAllBytes(string)"/>
		/// <inheritdoc cref="File.WriteAllBytes(string, byte[])"/>
		public static FileInfo MoveToBackup(this FileInfo file, string newName = null) {
			if (file == null) throw new ArgumentNullException(nameof(file));
			newName ??= file.Name + "-bak";
			FileInfo newTarget = new FileInfo(Path.Combine(file.Directory.FullName, newName));
			File.WriteAllBytes(newTarget.FullName, File.ReadAllBytes(file.FullName));
			return newTarget;
		}

		/// <summary>
		/// Returns whether or not this <see cref="DirectoryInfo"/> contains a file wih the given name.
		/// </summary>
		/// <param name="directory"></param>
		/// <param name="file"></param>
		/// <returns>Whether or not this <see cref="DirectoryInfo"/> contains a file wih the given name.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="DirectoryNotFoundException"></exception>
		public static bool Contains(this DirectoryInfo directory, string file) {
			if (directory == null) throw new ArgumentNullException(nameof(directory));
			if (file == null) throw new ArgumentNullException(nameof(file));
			/*
			FileInfo[] files = directory.GetFiles();
			for (int idx = 0; idx < files.Length; idx++) {
				if (files[idx].Name.ToLower() == file.ToLower()) {
					return true;
				}
			}
			return false;
			*/
			return new FileInfo(Path.Combine(directory.FullName, file)).Exists;
		}

		/// <summary>
		/// Assuming this file is named as snowflake-name (a snowflake, a dash, and its intended name), this will return its snowflake.<para/>
		/// If the file does not follow this naming convention, it will return <see cref="Snowflake.Invalid"/>
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public static Snowflake GetFileID(this FileInfo file) {
			if (file.Name.Contains('-')) {
				string[] splitData = file.Name.Split(new char[] { '-' }, 2);
				if (Snowflake.TryParse(splitData[0], out Snowflake result)) {
					return result;
				}
			}
			return Snowflake.Invalid;
		}

		/// <summary>
		/// Given a file whose name is formatted as snowflake-name (a snowflake, a dash, and its intended name), this will find the file in the given directory. If multiple files are found, this returns the first one.
		/// </summary>
		/// <param name="dir"></param>
		/// <param name="id"></param>
		public static FileInfo[] FindFilesByID(this DirectoryInfo dir, Snowflake id) {
			FileInfo[] files = dir.GetFiles($"{id}-*", SearchOption.TopDirectoryOnly);
			if (files.Length == 0) {
				return null;
			}
			return files;
		}

		/// <summary>
		/// Returns all of the bytes in this file.
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public static byte[] ReadAllBytes(this FileInfo file) => File.ReadAllBytes(file.FullName);

		/// <summary>
		/// Writes all of the bytes to this file.
		/// </summary>
		/// <param name="file"></param>
		/// <param name="bytes"></param>
		public static void WriteAllBytes(this FileInfo file, byte[] bytes) => File.WriteAllBytes(file.FullName, bytes);
	}
}
