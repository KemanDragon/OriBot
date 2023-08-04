using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using EtiBotCore.Utility.Attributes;
using OldOriBot.PermissionData;

namespace OldOriBot.Utility.Extensions {
	public static class PermissionLevelExtensions {

		/// <summary>
		/// Returns just the display name of this permission level.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		public static string GetName(this PermissionLevel level) {
			Type enumObj = level.GetType();

			string name = Enum.GetName(enumObj, level);
			if (name == null) {
				return ((int)level).ToString();
			}

			FieldInfo field = enumObj.GetField(name);
			if (field == null) {
				return ((int)level).ToString();
			}

			EnumConversionNameAttribute assocDef = field.GetCustomAttribute<EnumConversionNameAttribute>();
			if (assocDef == null) {
				return SplitByPascalCase(field.Name);
			}

			return assocDef.Name;
		}

		/// <summary>
		/// Returns the OG format: <c>Permission Level # ["Name"]</c>. This does not include the surrounding backticks if told not to.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="includeSurroundGraves">If true, <c>`</c> will be appended to either side of the return string.</param>
		/// <returns></returns>
		public static string GetFullName(this PermissionLevel level, bool includeSurroundGraves = true) {
			Type enumObj = level.GetType();

			string baseName = $"Permission Level {(int)level}";

			string name = Enum.GetName(enumObj, level);
			if (name == null) {
				return baseName;
			}

			FieldInfo field = enumObj.GetField(name);
			if (field == null) {
				return baseName;
			}

			EnumConversionNameAttribute assocDef = field.GetCustomAttribute<EnumConversionNameAttribute>();
			if (assocDef == null) {
				name = $"{baseName} [\"{SplitByPascalCase(field.Name)}\"]";
			} else {
				name = $"{baseName} [\"{SplitByPascalCase(assocDef.Name)}\"]";
			}

			if (includeSurroundGraves) return "`" + name + "`";
			return name;
		}

		/// <summary>
		/// Returns the OG format: <c>Permission Level # ["Name"]</c>. This does not include the surrounding backticks if told not to.
		/// This is formatted for the console.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="includeSurroundGraves">If true, <c>`</c> will be appended to either side of the return string.</param>
		/// <returns></returns>
		public static string GetFullNameConsole(this PermissionLevel level) {
			Type enumObj = level.GetType();

			string baseName = $"§2Permission Level {(int)level}";

			string name = Enum.GetName(enumObj, level);
			if (name == null) {
				return baseName;
			}

			FieldInfo field = enumObj.GetField(name);
			if (field == null) {
				return baseName;
			}

			EnumConversionNameAttribute assocDef = field.GetCustomAttribute<EnumConversionNameAttribute>();
			if (assocDef == null) {
				name = $"{baseName} [§6\"{SplitByPascalCase(field.Name)}\"§2]";
			} else {
				name = $"{baseName} [§6\"{SplitByPascalCase(assocDef.Name)}\"§2]";
			}

			return name;
		}

		private static string SplitByPascalCase(string name) {
			string res = "";
			bool hasReadFirst = false;
			foreach (char c in name) {
				if (hasReadFirst && char.IsUpper(c)) {
					res += " " + c;
				} else {
					res += c;
				}
				hasReadFirst = true;
			}
			return res;
		}
	}
}
