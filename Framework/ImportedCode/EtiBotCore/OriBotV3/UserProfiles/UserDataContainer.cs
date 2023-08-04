using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OldOriBot.Data.Persistence;
using OldOriBot.Utility.Extensions;

namespace OldOriBot.UserProfiles {
	/// <summary>
	/// A container object that stores userdata
	/// </summary>
	public class UserDataContainer : IByteSerializable {

		private readonly UserProfile Creator;

		public UserDataContainer(UserProfile creator) {
			Creator = creator;
		}

		private readonly Dictionary<string, object> Data = new Dictionary<string, object>();

		private static readonly Dictionary<Type, UserDataType> DataTypeBindings = new Dictionary<Type, UserDataType>() {
			[typeof(bool)] = UserDataType.Boolean,
			[typeof(byte)] = UserDataType.Byte,
			[typeof(short)] = UserDataType.Short,
			[typeof(int)] = UserDataType.Int,
			[typeof(long)] = UserDataType.Long,
			[typeof(ulong)] = UserDataType.UnsignedLong,
			[typeof(string)] = UserDataType.String
		};

		/// <summary>
		/// A dictionary where a string corresponds to a type, e.g. "bool" corresponds to the type System.Boolean
		/// </summary>
		public static readonly Dictionary<string, Type> StringToTypeBindings = new Dictionary<string, Type>() {
			["bool"] = typeof(bool),
			["byte"] = typeof(byte),
			["short"] = typeof(short),
			["int"] = typeof(int),
			["long"] = typeof(long),
			["ulong"] = typeof(ulong),
			["string"] = typeof(string)
		};

		/// <summary>
		/// Returns true if this <see cref="UserDataContainer"/> has the given key registered within it, and false if it does not.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool ContainsKey(string key) => Data.ContainsKey(key);

		public Dictionary<string, object>.KeyCollection Keys => Data.Keys;

		/// <summary>
		/// Get or set data in this data object. Setting to null will remove the key/value pair.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public object this[string index] {
			get => Data[index];
			set {
				if (value != null) {
					Data[index] = value;
				} else {
					if (Data.ContainsKey(index)) Data.Remove(index);
				}
			}
		}

		/// <summary>
		/// Attempts to get the value similarly to that of any other TryGetValue method, returning true if it could get the value and false if it could not.<para/>
		/// A critical distinction to make is that this uses ref instead of out. Rather than setting the value to a default, it will simply be unchanged, meaning you may define a default value before calling this.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool TryGetValue(string key, ref object value) {
			if (ContainsKey(key)) {
				value = this[key];
				return true;
			}
			return false;
		}

		/// <summary>
		/// Attempts to get a value from this <see cref="UserDataContainer"/>, populating <paramref name="value"/> with the stored data.<para/>
		/// This uses <see langword="ref"/> so that a default can be specified ahead of time in case the data doesn't exist.<para/>
		/// Returns <see langword="true"/> if the value both exists, and the type of <paramref name="value"/> is the same type as the stored data.<para/>
		/// Returns <see langword="false"/> if the value does not exist, or if it does exist but the type of the data stored is different than the type of <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool TryGetValue<T>(string key, ref T value) {
			// Specifically use ref instead of out since I want the ability to define default values ahead of time.
			if (ContainsKey(key)) {
				object dVal = this[key];
				if (dVal is T retVal) {
					value = retVal;
					return true;
				}
			}
			return false;
		}

		public int FromBytes(byte[] data) {
			using MemoryStream memStr = new MemoryStream(data);
			using BinaryReader reader = new BinaryReader(memStr);
			FromBytesInExistingStream(reader);
			return 0;
		}

		public void FromBytesInExistingStream(BinaryReader reader) {
			int elements = reader.ReadInt32();
			for (int i = 0; i < elements; i++) {
				string key = reader.ReadStringSL(Encoding.ASCII);
				UserDataType dataType = (UserDataType)reader.ReadByte();
				bool isArray = reader.ReadBoolean();

				object value;
				if (isArray) {
					int length = reader.ReadInt32();
					object[] values = new object[length];
					for (int j = 0; j < length; j++) {
						if (dataType == UserDataType.Boolean) {
							values[j] = reader.ReadBoolean();
						} else if (dataType == UserDataType.Byte) {
							values[j] = reader.ReadByte();
						} else if (dataType == UserDataType.Short) {
							values[j] = reader.ReadInt16();
						} else if (dataType == UserDataType.Int) {
							values[j] = reader.ReadInt32();
						} else if (dataType == UserDataType.Long) {
							values[j] = reader.ReadInt64();
						} else if (dataType == UserDataType.UnsignedLong) {
							values[j] = reader.ReadUInt64();
						} else if (dataType == UserDataType.String) {
							values[j] = reader.ReadStringSL();
						}
					}
					value = values;
				} else {
					if (dataType == UserDataType.Boolean) {
						value = reader.ReadBoolean();
					} else if (dataType == UserDataType.Byte) {
						value = reader.ReadByte();
					} else if (dataType == UserDataType.Short) {
						value = reader.ReadInt16();
					} else if (dataType == UserDataType.Int) {
						value = reader.ReadInt32();
					} else if (dataType == UserDataType.Long) {
						value = reader.ReadInt64();
					} else if (dataType == UserDataType.UnsignedLong) {
						value = reader.ReadUInt64();
					} else if (dataType == UserDataType.String) {
						value = reader.ReadStringSL();
					} else {
						value = null;
					}
				}

				this[key] = value;
			}
		}

		public byte[] ToBytes() {
			using MemoryStream memory = new MemoryStream(1024);
			using BinaryWriter writer = new BinaryWriter(memory);
			// Go through all userdata
			int nonNullCount = 0;
			foreach (string key in Data.Keys) {
				if (Data[key] != null) nonNullCount++;
			}
			writer.Write(nonNullCount);
			foreach (string key in Data.Keys) {
				// Get value of an unknown type.
				object val = Data[key];
				if (val != null) {
					writer.WriteStringSL(key, Encoding.ASCII);
					Type valT = val.GetType();
					Type elementT = valT;
					if (valT.IsArray) elementT = valT.GetElementType();

					// Write type signature by ID then write 1 if it's a list of that type and 0 if it's just an actual instance of that type
					// e.g. list of numbers vs. a number
					UserDataType dataType;
					if (DataTypeBindings.ContainsKey(elementT)) {
						dataType = DataTypeBindings[elementT];
					} else {
						throw new InvalidOperationException("Could not convert type " + elementT.FullName + " into a serializable form.");
					}
					writer.Write((byte)dataType);
					writer.Write(valT.IsArray);

					// Now populate the data.
					if (valT.IsArray) {
						object[] vals = (object[])val;
						writer.Write(vals.Length);
						foreach (object value in vals) {
							if (dataType == UserDataType.Boolean) {
								writer.Write((bool)value);
							} else if (dataType == UserDataType.Byte) {
								writer.Write((byte)value);
							} else if (dataType == UserDataType.Short) {
								writer.Write((short)value);
							} else if (dataType == UserDataType.Int) {
								writer.Write((int)value);
							} else if (dataType == UserDataType.Long) {
								writer.Write((long)value);
							} else if (dataType == UserDataType.UnsignedLong) {
								writer.Write((ulong)value);
							} else if (dataType == UserDataType.String) {
								writer.WriteStringSL((string)value);
							}
						}
					} else {
						if (dataType == UserDataType.Boolean) {
							writer.Write((bool)val);
						} else if (dataType == UserDataType.Byte) {
							writer.Write((byte)val);
						} else if (dataType == UserDataType.Short) {
							writer.Write((short)val);
						} else if (dataType == UserDataType.Int) {
							writer.Write((int)val);
						} else if (dataType == UserDataType.Long) {
							writer.Write((long)val);
						} else if (dataType == UserDataType.UnsignedLong) {
							writer.Write((ulong)val);
						} else if (dataType == UserDataType.String) {
							writer.WriteStringSL((string)val);
						}
					}
				}
			}
			return memory.ToArray();
		}

		public string ToDiscordMessageString(bool isPrivate = false) {
			string inter = "";
			if (isPrivate) inter = "Internal ";
			string msg = $"**All {inter}Values:** ```";
			foreach (string key in Keys) {
				msg += key + "=" + this[key] + "\n";
			}
			msg += "```";
			return msg;
		}
	}

	enum UserDataType : byte {
		Boolean = 0,
		Byte = 1,
		Short = 2,
		Int = 3,
		Long = 4,
		UnsignedLong = 5,
		String = 6
	}
}
