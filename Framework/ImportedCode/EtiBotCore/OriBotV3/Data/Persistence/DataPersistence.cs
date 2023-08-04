using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using EtiBotCore.DiscordObjects;
using EtiLogger.Data.Structs;
using EtiLogger.Logging;
using OldOriBot.Interaction;
using OldOriBot.Utility.Extensions;

namespace OldOriBot.Data.Persistence {

	/// <summary>
	/// A class that offers a crude means of key/value based data persistence.
	/// </summary>
	public class DataPersistence : IEquatable<DataPersistence> {

		/// <summary>
		/// A cache of already-instantiated <see cref="DataPersistence"/> objects.
		/// </summary>
		private static readonly Dictionary<BotContext, Dictionary<string, DataPersistence>> PersistenceCache = new Dictionary<BotContext, Dictionary<string, DataPersistence>>();

		/// <summary>
		/// A cache of already-instantiated global <see cref="DataPersistence"/> objects.
		/// </summary>
		private static readonly Dictionary<string, DataPersistence> GlobalPersistenceCache = new Dictionary<string, DataPersistence>();

		/// <summary>
		/// A list of every domain bound by <see cref="BotContext"/>
		/// </summary>
		public static IReadOnlyDictionary<string, List<string>> Domains => _Domains;
		private static readonly Dictionary<string, List<string>> _Domains = new Dictionary<string, List<string>>();

		/// <summary>
		/// The default config file name.
		/// </summary>
		public const string DEFAULT_NAME = "persistent.cfg";

		/// <summary>
		/// The directory in which data persistence files are stored.
		/// </summary>
		public static DirectoryInfo PersistenceRootFolder { get; set; } = DefaultPersistenceRootFolder;
		public static DirectoryInfo DefaultPersistenceRootFolder {
			get {
				if (DefPRoot == null) {
					if (Directory.Exists("V:\\")) {
						DefPRoot = new DirectoryInfo(@"V:\EtiBotCore\");
					} else {
						DefPRoot = new DirectoryInfo(@"C:\EtiBotCore\");
					}
				}
				return DefPRoot;
			}
		}
		private static DirectoryInfo DefPRoot = null;

		/// <summary>
		/// The default global data persistence file named <c>global.cfg</c>
		/// </summary>
		public static DataPersistence Global {
			get {
				if (_Global == null) _Global = new DataPersistence();
				return _Global;
			}
		}
		private static DataPersistence _Global = null;

		/// <summary>
		/// The storage file.
		/// </summary>
		public FileInfo StorageFile { get; }

		/// <summary>
		/// The logger for this <see cref="DataPersistence"/>
		/// </summary>
		public Logger PersistenceLogger { get; }

		/// <summary>
		/// The values stored in this config.
		/// </summary>
		protected readonly Dictionary<string, string> ConfigValues = new Dictionary<string, string>();

		/// <summary>
		/// Gets all keys in this config by referencing <see cref="ConfigValues.Keys"/>
		/// </summary>
		public IEnumerable<string> Keys => ConfigValues.Keys;

		/// <summary>
		/// Gets all values in this config by referencing <see cref="ConfigValues.Values"/>
		/// </summary>
		public IEnumerable<string> Values => ConfigValues.Values;

		/// <summary>
		/// Gets all keys in this config sorted in alphabetical order.<para/>
		/// Warning: This property may be expensive to index.
		/// </summary>
		public string[] OrderedKeys {
			get {
				string[] keys = Keys.ToArray();
				Array.Sort(keys);
				return keys;
			}
		}

		/// <summary>
		/// The domain of this <see cref="DataPersistence"/>, which is its filename without an extension.
		/// </summary>
		public string Domain { get; }

		/// <summary>
		/// True if the system has unsaved changes.
		/// </summary>
		public bool HasUnsavedChanges { get; protected set; } = false;

		/// <summary>
		/// The <see cref="BotContext"/> this configuration was created for. This will be <see langword="null"/> for the global utility.
		/// </summary>
		public BotContext Context { get; }

		public static void DoStaticInit() {
			_Domains["global"] = new List<string>();
			foreach (FileInfo file in PersistenceRootFolder.GetFiles("*.cfg")) {
				_Domains["global"].Add(file.Name.Replace(file.Extension ?? "", ""));
			}
		}


		protected DataPersistence(BotContext context, string fileName = DEFAULT_NAME) {
			Context = context;
			StorageFile = new FileInfo(Path.Combine(PersistenceRootFolder.FullName, context.DataPersistenceName, fileName));
			PersistenceLogger = new Logger("^#ffb742;[Data Persistence: ^#baa94a;" + context.Name + "^#ffb742;] ");

			// Create root container.
			if (!PersistenceCache.ContainsKey(context)) PersistenceCache[context] = new Dictionary<string, DataPersistence>();
			PersistenceCache[context][StorageFile.FullName] = this;

			Domain = StorageFile.Name.Replace(StorageFile.Extension ?? "", "");
			if (!_Domains.ContainsKey(context.DataPersistenceName)) {
				_Domains[context.DataPersistenceName] = new List<string>();
			}
			if (!_Domains[context.DataPersistenceName].Contains(Domain)) _Domains[context.DataPersistenceName].Add(Domain);
			PersistenceLogger.WriteLine("Created new persistence at " + StorageFile.FullName, LogLevel.Debug);
			Load();
		}

		protected DataPersistence(string fileName = "global.cfg") {
			Context = null;
			StorageFile = new FileInfo(Path.Combine(PersistenceRootFolder.FullName, fileName));
			PersistenceLogger = new Logger("^#ffb742;[Data Persistence: ^#baa94a;GLOBAL^#ffb742;] ");
			GlobalPersistenceCache[StorageFile.FullName] = this;
			Load();
		}

		public delegate void ValueChanged(DataPersistence source, string key, string oldValue, string newValue, bool valueJustCreated);

		public delegate void ValueRemoved(DataPersistence source, string key);

		/// <summary>
		/// An event that fires when the configuration for this context changes.
		/// </summary>
		public event ValueChanged OnValueChanged;

		/// <summary>
		/// An event that fires when a configuration value is removed.
		/// </summary>
		public event ValueRemoved OnValueRemoved;

		/// <summary>
		/// Ambiguous variation of TryParse that attempts to work for any type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="input"></param>
		/// <returns></returns>
		protected static bool TryParse<T>(string input, out T value) {
			try {
				value = (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(input);
				return true;
			} catch {
				value = default;
				return false;
			}
		}

		/// <summary>
		/// Mandates the type of a value by returning the value associated with the key as the specified type, or by writing the default value and returning the default value if the key is malformed or doesn't exist.<para/>
		/// When tying a config value into a property, this method is suggested.<para/>
		/// If you want to display an error message, consider using <see cref="GetAndMandateType{T}(string, T, out T)"/>
		/// </summary>
		/// <typeparam name="T">The type of data that is desired.</typeparam>
		/// <param name="configKey">The config key to search.</param>
		/// <param name="defaultValue">The default value if the config key doesn't exist or if the data is malformed.</param>
		public T TryGetType<T>(string configKey, T defaultValue) {
			string v = GetValue(configKey, defaultValue.ToString(), true, true);
			if (!TryParse(v, out T retn)) {
				retn = defaultValue;

				string defValueStr = defaultValue.ToString();
				if (typeof(T) == typeof(bool)) {
					// This is to ensure it formats properly in >> config list
					defValueStr = defValueStr.ToLower();
				}
				SetValue(configKey, defValueStr);
			}
			return retn;
		}

		/// <summary>
		/// Similar to <see cref="GetAndMandateType{T}(string, T, out T)"/>, but it attempts to populate the given list. This assumes the config value is something separated by a given character (as defined by <paramref name="separator"/>).<para/>
		/// This throws an <see cref="InvalidCastException"/> if not all of the list items could be converted to the given type. Returns an empty list if the config key does not exist and <paramref name="defaultValue"/> is null. An empty value will be populated.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="configKey"></param>
		/// <param name="separator"></param>
		public List<T> GetListOfType<T>(string configKey, char separator = ',', T[] defaultValue = null) {
			if (defaultValue == null) {
				defaultValue = new T[0];
			}
			StringBuilder defValueString = new StringBuilder();
			for (int idx = 0; idx < defaultValue.Length; idx++) {
				defValueString.Append(defaultValue[idx].ToString());
				if (idx != defaultValue.Length - 1) {
					defValueString.Append(separator);
				}
			}
			string v = GetValue(configKey, defValueString.ToString(), true, true);
			if (v == string.Empty || v == "~") {
				return new List<T>(); // Empty list.
			}

			List<T> tArray = new List<T>();
			foreach (string clip in v.Split(separator)) {
				if (TryParse(clip, out T val)) {
					tArray.Add(val);
				} else {
					throw new InvalidCastException("Unable to cast " + val + " to " + typeof(T).FullName);
				}
			}
			return tArray;
		}

		/// <summary>
		/// A stricter variation of <see cref="TryGetType{T}(string, T)"/> that throws a <see cref="InvalidCastException"/> if the config value could not be cast into the target type.<para/>
		/// If the data in the config key is unable to be cast into the target type, the default value will be written before the exception is thrown.
		/// </summary>
		/// <typeparam name="T">The type of data that is desired.</typeparam>
		/// <param name="configKey">The config key to search.</param>
		/// <param name="defaultValue">The default value if the config key doesn't exist or if the data is malformed.</param>
		/// <exception cref="InvalidCastException"/>
		public void GetAndMandateType<T>(string configKey, T defaultValue, out T value) {
			string v = GetValue(configKey, defaultValue.ToString(), true, true);
			if (!TryParse(v, out T retn)) {
				string defValueStr = defaultValue.ToString();
				if (defaultValue.GetType() == typeof(bool)) {
					// This is to ensure it formats properly in >> config list
					defValueStr = defValueStr.ToLower();
				}
				SetValue(configKey, defValueStr);
				value = defaultValue;
				throw new InvalidCastException($"WARNING: Config key `{configKey}` attempted to read the value from the configuration file, but it failed! Reason: Could not cast `{v}` into type {typeof(T).Name}. It has been set to its default value of {defaultValue}");
			}
			value = retn;
		}

		/// <summary>
		/// Set a value in the file.
		/// </summary>
		/// <param name="key">The key associated with the value</param>
		/// <param name="value">The desired value to assign</param>
		/// <param name="dontSaveOnWrite">If this is true, the system will skip saving after setting this value. The user must call <see cref="Save"/> manually if this is done. Generally speaking, this should be used if you are writing massive amounts of config data and want to avoid constantly opening and closing the file handle.</param>
		public void SetValue(string key, object value, bool dontSaveOnWrite = false) {
			string oldValue = GetValue(key);
			string valueStr = value.ToString();
			ConfigValues[key] = valueStr;
			if (!dontSaveOnWrite) {
				Save();
			} else {
				HasUnsavedChanges = true;
			}

			OnValueChanged?.Invoke(this, key, oldValue, valueStr, oldValue == null);
		}

		/// <summary>
		/// Automatically encodes an array of items into data persistence. If these items are <see cref="DiscordObject"/>s, their IDs will be written. Otherwise, <see cref="object.ToString()"/> will be used.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="values"></param>
		/// <param name="dontSaveOnWrite"></param>
		public void SetArray<T>(string key, T[] values, bool dontSaveOnWrite = false) {
			string tx = "";
			if (values.Length == 0) {
				SetValue(key, "~", dontSaveOnWrite);
				return;
			}
			for (int idx = 0; idx < values.Length; idx++) {
				T obj = values[idx];
				if (obj is DiscordObject dObj) {
					tx += dObj.ID;
				} else {
					tx += obj.ToString();
				}
				if (idx != values.Length - 1) {
					tx += ",";
				}
			}
			SetValue(key, tx, dontSaveOnWrite);
		}

		/// <summary>
		/// Set a list value in the file, separating the entries with a comma <c>,</c>
		/// </summary>
		/// <param name="key">The key associated with the value</param>
		/// <param name="value">The desired value to assign</param>
		/// <param name="dontSaveOnWrite">If this is true, the system will skip saving after setting this value. The user must call <see cref="Save"/> manually if this is done. Generally speaking, this should be used if you are writing massive amounts of config data and want to avoid constantly opening and closing the file handle.</param>
		public void SetValue(string key, List<string> value, bool dontSaveOnWrite = false) {
			string oldValue = GetValue(key);
			string newValue = "";
			for (int idx = 0; idx < value.Count; idx++) {
				newValue += value[idx].ToString();
				if (idx != value.Count - 1) {
					newValue.Append(',');
				}
			}
			ConfigValues[key] = newValue;
			if (!dontSaveOnWrite) {
				Save();
			} else {
				HasUnsavedChanges = true;
			}

			OnValueChanged?.Invoke(this, key, oldValue, newValue, oldValue == null);
		}

		/// <summary>
		/// Internal method to set the config value from a default. Contains args to fire the event.
		/// </summary>
		/// <param name="key">The key of the config value</param>
		/// <param name="oldValue">The value before being changed</param>
		/// <param name="newValue">The value after being changed</param>
		/// <param name="wasJustCreated">Whether or not the value is now registered in config and wasn't regsitered before this.</param>
		protected void SetValue(string key, string oldValue, string newValue, bool wasJustCreated) {
			ConfigValues[key] = newValue;
			Save();

			OnValueChanged?.Invoke(this, key, oldValue, newValue, wasJustCreated);
		}

		/// <summary>
		/// Returns a value from the file, or <paramref name="defaultValue"/> if it does not exist.
		/// </summary>
		/// <param name="key">The key associated with the value</param>
		/// <param name="defaultValue">The default value to get if it doesn't exist</param>
		/// <param name="writeIfDoesntExist">Write the default value if it doesn't exist.</param>
		/// <param name="reloadConfigFile">If true, any unsaved data will be scrapped, and the file will be re-read. This allows hand-made edits to the config file to be reflected.</param>
		/// <returns></returns>
		public string GetValue(string key, string defaultValue = null, bool writeIfDoesntExist = false, bool reloadConfigFile = false) {
			if (reloadConfigFile) {
				Reload();
			}
			bool valueExists = ConfigValues.TryGetValue(key, out string value);
			if (!valueExists) {
				if (writeIfDoesntExist) {
					SetValue(key, value, defaultValue, true);
				}
				return defaultValue;
			}
			if (value == "True" || value == "False") {
				value = value.ToLower();
			}
			return value;
		}

		/// <summary>
		/// Removes a config value from the config array. Returns true if the key existed and was removed, and false if the key did not exist prior to calling this.
		/// </summary>
		/// <param name="key">The key to remove</param>
		public bool RemoveValue(string key) {
			if (ContainsKey(key)) {
				ConfigValues.Remove(key);
				Save();
				OnValueRemoved?.Invoke(this, key);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Returns whether or not the specified key has been defined in this <see cref="XConfiguration"/>
		/// </summary>
		/// <param name="key">The key to search for</param>
		/// <returns></returns>
		public bool ContainsKey(string key) {
			return ConfigValues.ContainsKey(key);
		}

		/// <summary>
		/// Saves the config file. This should only be used if <see cref="SetValue(string, string, bool)"/> was called with its dontSaveOnWrite parameter set to true. This is automatically called if the parameter is false.
		/// </summary>
		public void Save() {
			if (StorageFile.Exists) {
				StorageFile.MoveToBackup();
			}
			StreamWriter dataStream = CreateNewFile(StorageFile);
			string[] keys = OrderedKeys;
			foreach (string key in keys) {
				string value = ConfigValues[key];
				dataStream.WriteLine(key + " " + value);
			}
			dataStream.Flush();
			dataStream.Close();
			HasUnsavedChanges = false;
		}

		/// <summary>
		/// Loads the config file.
		/// </summary>
		protected void Load() {
			if (HasUnsavedChanges) {
				PersistenceLogger.WriteWarning("§4A request to load the config file was made, but there were unsaved manual changes in the config file made via code. The config file will not be loaded, which may have adverse effects!");
				return;
			}

			Directory.CreateDirectory(StorageFile.Directory.FullName);
			if (!StorageFile.Exists) File.WriteAllText(StorageFile.FullName, "");
			string[] lines = File.ReadAllLines(StorageFile.FullName);
			string[] keys = new string[lines.Length];
			string[] values = new string[lines.Length];
			int idx = 0;
			foreach (string line in lines) {
				string[] valueSplit = line.Split(new char[] { ' ' }, 2);
				if (valueSplit.Length != 2) continue;
				string index = valueSplit[0];
				string stringValue = valueSplit[1];
				// ConfigValues[index] = stringValue;
				keys[idx] = index;
				values[idx] = stringValue;
				idx++;
			}

			// New behavior: Organize config data alphabetically.
			Array.Sort(keys, values);
			for (idx = 0; idx < keys.Length; idx++) {
				ConfigValues[keys[idx]] = values[idx];
			}
		}

		/// <summary>
		/// Reloads this <see cref="XConfiguration"/> by scrapping all pending changes then re-reading the file this <see cref="XConfiguration"/> points to.
		/// </summary>
		public void Reload() {
			if (HasUnsavedChanges) {
				PersistenceLogger.WriteWarning("A request to reload the config file was made, but there were unsaved manual changes in the config file made via code. The config file will not be reloaded, which may have adverse effects!");
				return;
			}
			ConfigValues.Clear();
			Load();
		}

		/// <summary>
		/// Creates a new <see cref="DataPersistence"/> (or gets an existing one) from the specified <see cref="BotContext"/>. This caches the object.
		/// </summary>
		/// <param name="context">The <see cref="BotContext"/> that this <see cref="XConfiguration"/> should target.</param>
		/// <returns></returns>
		public static DataPersistence GetPersistence(BotContext context, string cfgFileName = DEFAULT_NAME) {
			if (PersistenceCache.TryGetValue(context, out Dictionary<string, DataPersistence> persistenceLookup)) {
				FileInfo storage = new FileInfo(Path.Combine(PersistenceRootFolder.FullName, context.DataPersistenceName, cfgFileName));
				if (persistenceLookup.TryGetValue(storage.FullName, out DataPersistence persistence)) {
					return persistence;
				}
			}
			return new DataPersistence(context, cfgFileName); // Populates cache on its own.
		}

		/// <summary>
		/// Returns an existing <see cref="DataPersistence"/> in the context with the given filename, or <see langword="null"/> if it does not exist. This will not create the <em>file</em>, but may create a new <see cref="DataPersistence"/> if an appropriately named file exists.
		/// </summary>
		/// <param name="context">The target context. If null, this searches the common container.</param>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public static DataPersistence GetDataPersistenceNoCreate(BotContext context, string fileName = DEFAULT_NAME) {
			if (context == null) {
				if (fileName == null || fileName == "global.cfg") return Global;
				if (GlobalPersistenceCache.TryGetValue(new FileInfo(Path.Combine(PersistenceRootFolder.FullName, fileName)).FullName, out DataPersistence persistence)) {
					return persistence;
				}
				return null;
			}

			FileInfo storage = new FileInfo(Path.Combine(PersistenceRootFolder.FullName, context.DataPersistenceName, fileName));
			if (PersistenceCache.TryGetValue(context, out Dictionary<string, DataPersistence> persistenceLookup)) {
				if (persistenceLookup.TryGetValue(storage.FullName, out DataPersistence persistence)) {
					return persistence;
				}
			}

			// Another thing
			if (storage.Exists) {
				// well that's OK
				return new DataPersistence(context, fileName);
			}

			return null;
		}

		/// <summary>
		/// Returns a global <see cref="DataPersistence"/> (that is, not associated with any given <see cref="BotContext"/>) with the specified name.<para/>
		/// If the name is <see langword="null"/> or equal to <c>global.cfg</c>, it will return <see cref="Global"/> (in which case you should probably just reference it directly)
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public static DataPersistence GetGlobalPersistence(string fileName = null) {
			if (fileName == null || fileName == "global.cfg") return Global;
			if (GlobalPersistenceCache.TryGetValue(new FileInfo(Path.Combine(PersistenceRootFolder.FullName, fileName)).FullName, out DataPersistence persistence)) {
				return persistence;
			}
			return new DataPersistence(fileName);  // Populates cache on its own.
		}

		public bool Equals([AllowNull] DataPersistence other) {
			if (other is null) return false;
			return StorageFile == other.StorageFile;
		}

		private StreamWriter CreateNewFile(FileInfo at) {
			if (at.Exists) at.Delete();
			return at.CreateText();
		}

		/// <summary>
		/// Iterates through the data persistence folder of the given <see cref="BotContext"/> and registers all config files inside ahead of time as possible options for the config command to choose from.
		/// </summary>
		/// <param name="context"></param>
		public static void RegisterDomains(BotContext context) {
			if (context.DataPersistenceName == "global") throw new InvalidOperationException("global is a reserved data persistence name.");
			DirectoryInfo storage = new DirectoryInfo(Path.Combine(PersistenceRootFolder.FullName, context.DataPersistenceName));
			foreach (FileInfo file in storage.GetFiles()) {
				if (!_Domains.ContainsKey(context.DataPersistenceName)) {
					_Domains[context.DataPersistenceName] = new List<string>();
				}
				string domain = file.Name.Replace(file.Extension ?? "", "");
				if (!_Domains[context.DataPersistenceName].Contains(domain)) {
					_Domains[context.DataPersistenceName].Add(domain);
				}
			}
		}
	}
}
