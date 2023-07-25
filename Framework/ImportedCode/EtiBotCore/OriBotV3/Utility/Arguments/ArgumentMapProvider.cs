using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.Utility.Extension;
using EtiBotCore.Utility.Marshalling;
using OldOriBot.Interaction;
using OldOriBot.Utility.Extensions;

namespace OldOriBot.Utility.Arguments {

	/// <summary>
	/// A generic <see cref="ArgumentMapProvider"/>
	/// </summary>
	public class ArgumentMapProvider {

		/// <summary>
		/// Whether or not the type names are included in the argument sequence by default.
		/// </summary>
		public const bool INCLUDE_TYPE_NAMES_BY_DEFAULT = true;

		/// <summary>
		/// If true, a markdown link will be used as the type name for snowflakes.
		/// </summary>
		public const bool USE_MD_LINK_FOR_SNOWFLAKE = true;

		/// <summary>
		/// The text displayed for the URL to Discord's page defining snowflakes.
		/// </summary>
		public const string SNOWFLAKE_MD_LINK_NAME = "ID";

		/// <summary>
		/// A markdown link going to Discord's page defining snowflakes.
		/// </summary>
		// NOTE: If you change this, remember to change the GetTypeName function down below too!
		public const string SNOWFLAKE_MD_LINK = "[" + SNOWFLAKE_MD_LINK_NAME + "](https://support.discord.com/hc/en-us/articles/206346498-Where-can-I-find-my-User-Server-Message-ID-)";

		/// <summary>
		/// If true, numeric types will be changed to "Number" instead of their type name, string will be changed to "Text", and Snowflakes will be changed to "UserID"
		/// </summary>
		public const bool MAKE_TYPES_USER_FRIENDLY = true;

		/// <summary>
		/// Required arguments are surrounded in these two characters (the first on the left and second on the right).
		/// </summary>
		public const string REQUIRED_SURROUND = "<>";

		/// <summary>
		/// Optional arguments are surrounded in these two characters (the first on the left and second on the right).
		/// </summary>
		public const string OPTIONAL_SURROUND = "[]";

		/// <summary>
		/// Subcommand options are surrounded in the first and last characters, and separated by the middle character.
		/// </summary>
		public const string SUBCOMMAND_SURROUND = "{|}";

		/// <summary>
		/// The names of each argument in order.
		/// </summary>
		private string[] ArgNames { get; }

		/// <summary>
		/// A list in sync with <see cref="ArgNames"/> that contains whether or not the argument at its given position is required or not.
		/// </summary>
		private bool[] RequiredArgs { get; set; }

		/// <summary>
		/// The types for each argument.
		/// </summary>
		protected Type[] ArgTypes { get; set; }

		/// <summary>
		/// The amount of arguments in this map.
		/// </summary>
		/// <returns></returns>
		public int ArgCount => ArgTypes.Length;

		/// <summary>
		/// The context this map exists in.
		/// </summary>
		protected BotContext Context { get; set; }

		/// <summary>
		/// Construct a new <see cref="ArgumentMapProvider"/> with the given argument names. No names can be <see langword="null"/> or <see cref="string.Empty"/>.
		/// </summary>
		/// <param name="argNames"></param>
		/// <exception cref="ArgumentNullException">If any names are null.</exception>
		protected ArgumentMapProvider(params string[] argNames) {
			for (int argIdx = 0; argIdx < argNames.Length; argIdx++) {
				if (string.IsNullOrEmpty(argNames[argIdx])) throw new ArgumentNullException($"{nameof(argNames)}[{argIdx}]");
			}
			ArgNames = argNames;
		}

		/// <summary>
		/// Modifies the required state of the arguments at the given positions in order.
		/// </summary>
		/// <param name="reqStates"></param>
		public ArgumentMapProvider SetRequiredState(params bool[] reqStates) {
			RequiredArgs = reqStates;
			return this;
		}

		/// <summary>
		/// Sets the target <see cref="BotContext"/>
		/// </summary>
		/// <param name="ctx"></param>
		/// <returns></returns>
		public ArgumentMapProvider SetContext(BotContext ctx) {
			Context = ctx;
			return this;
		}

		// NOTE: If you change this, remember to change it in CommandTypeInfo too!
		/// <summary>
		/// Returns a user-friendly name for the given type, or just the type's name if <see cref="MAKE_TYPES_USER_FRIENDLY"/> is <see langword="false"/>.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public string GetUserFriendlyTypeName(Type type) {
			if (!MAKE_TYPES_USER_FRIENDLY) {
				return type.Name;
			}

			if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) {
				return "Decimal";
			}
			if (type.IsNumericType()) {
				return "Number";
			}
			if (type == typeof(string)) {
				return "Text";
			}
			if (type == typeof(char)) {
				return "Character";
			}
			if (typeof(Variant).IsAssignableFrom(type)) {
				return "Either";
			}
			if (type == typeof(Snowflake)) {
				return USE_MD_LINK_FOR_SNOWFLAKE ? SNOWFLAKE_MD_LINK : SNOWFLAKE_MD_LINK_NAME;
			}
			return type.Name;
		}

		public ArgumentMap<T1> Parse<T1>(params object[] args) {
			return ((ArgumentMapProvider<T1>)this).Parse(args);
		}

		public ArgumentMap<T1, T2> Parse<T1, T2>(params object[] args) {
			return ((ArgumentMapProvider<T1, T2>)this).Parse(args);
		}

		public ArgumentMap<T1, T2, T3> Parse<T1, T2, T3>(params object[] args) {
			return ((ArgumentMapProvider<T1, T2, T3>)this).Parse(args);
		}

		public ArgumentMap<T1, T2, T3, T4> Parse<T1, T2, T3, T4>(params object[] args) {
			return ((ArgumentMapProvider<T1, T2, T3, T4>)this).Parse(args);
		}

		public ArgumentMap<T1, T2, T3, T4, T5> Parse<T1, T2, T3, T4, T5>(params object[] args) {
			return ((ArgumentMapProvider<T1, T2, T3, T4, T5>)this).Parse(args);
		}


		/// <summary>
		/// Calls <see cref="ToString(bool)"/> with an argument of <see cref="INCLUDE_TYPE_NAMES_BY_DEFAULT"/>.
		/// </summary>
		/// <returns></returns>
		public override string ToString() => ToString(INCLUDE_TYPE_NAMES_BY_DEFAULT);

		/// <summary>
		/// Returns the name of the argument at the given index (where 0 is the first argument)
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public string GetArgName(int index) {
			return ArgNames[index];
		}

		/// <summary>
		/// Returns a user-friendly type name for the argument at the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public string GetArgTypeName(int index) {
			return GetUserFriendlyTypeName(ArgTypes[index]);
		}

		/// <summary>
		/// Returns a display name of this argument map with the provided argument types and names
		/// </summary>
		/// <returns></returns>
		public string ToString(bool withTypes) {
			StringBuilder resStr = new StringBuilder();
			for (int argIdx = 0; argIdx < ArgNames.Length; argIdx++) {
				bool required = RequiredArgs?.GetOrDefault(argIdx) ?? false;
				string arg = ArgNames[argIdx];
				Type type = ArgTypes[argIdx];
				if (withTypes) {
					string name = GetUserFriendlyTypeName(type);
					if (type.IsArray) {
						name = GetUserFriendlyTypeName(type.GetElementType()) + "(s)";
					} else if (type.IsGenericType) {
						Type[] generics = type.GetGenericArguments();
						name += "<";
						if (generics.Length > 2) {
							for (int gIdx = 0; gIdx < generics.Length; gIdx++) {
								name += GetUserFriendlyTypeName(generics[gIdx]);
								if (gIdx != generics.Length - 1) name += ", ";
								if (gIdx < generics.Length - 2 && gIdx != 0) name += "or ";
							}
						} else {
							if (generics.Length == 1) {
								name += GetUserFriendlyTypeName(generics[0]);
							} else {
								name += GetUserFriendlyTypeName(generics[0]) + " or " + GetUserFriendlyTypeName(generics[1]);
							}
						}
						name += ">";
					}
					arg = name + " `" + arg + "`";
				}
				if (type.IsArray) {
					arg += "...";
				}
				if (required) {
					arg = arg.SurroundIn(REQUIRED_SURROUND);
				} else {
					arg = arg.SurroundIn(OPTIONAL_SURROUND);
				}
				if (argIdx < ArgNames.Length - 1) arg += " ";
				resStr.Append(arg);
			}
			return resStr.ToString();
		}

		/// <summary>
		/// Returns whether or not this has at least one string argument.
		/// </summary>
		/// <returns></returns>
		public bool HasStringArg() {
			if (ArgTypes.Contains(typeof(string))) return true;
			foreach (Type argType in ArgTypes) {
				if (argType.IsGenericType && typeof(Variant).IsAssignableFrom(argType)) {
					// It's a variant. String?
					if (argType.GetGenericArguments().Contains(typeof(string))) return true;
				}
				if (argType.IsArray) {
					if (argType.GetElementType() == typeof(string)) return true;
				}
			}
			return false;
		}
	}

	/// <summary>
	/// An <see cref="ArgumentMapProvider{T1}"/> taking in one argument.
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	public class ArgumentMapProvider<T1> : ArgumentMapProvider {

		public ArgumentMapProvider(string nameArg1) : base(nameArg1) {
			ArgTypes = new Type[] { typeof(T1) };
		}

		/// <summary>
		/// Construct a new <see cref="ArgumentMap{T1}"/> from the given arguments. Missing arguments will be substituted with <see langword="default"/>.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// <exception cref="InvalidCastException">If the given parameter types don't match up.</exception>
		public ArgumentMap<T1> Parse(params object[] args) {
			return new ArgumentMap<T1>(args.GetOrDefault<T1>(this, 0, Context));
		}

	}

	/// <summary>
	/// An <see cref="ArgumentMapProvider{T1, T2}"/> taking in two arguments.
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	/// <typeparam name="T2"></typeparam>
	public class ArgumentMapProvider<T1, T2> : ArgumentMapProvider {

		public ArgumentMapProvider(string nameArg1, string nameArg2) : base(nameArg1, nameArg2) {
			ArgTypes = new Type[] { typeof(T1), typeof(T2) };
		}

		/// <summary>
		/// Construct a new <see cref="ArgumentMap{T1, T2}"/> from the given arguments. Missing arguments will be substituted with <see langword="default"/>.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// <exception cref="InvalidCastException">If the given parameter types don't match up.</exception>
		public ArgumentMap<T1, T2> Parse(params object[] args) {
			return new ArgumentMap<T1, T2>(args.GetOrDefault<T1>(this, 0, Context), args.GetOrDefault<T2>(this, 1, Context));
		}

	}

	/// <summary>
	/// An <see cref="ArgumentMapProvider{T1, T2, T3}"/> taking in three arguments.
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	/// <typeparam name="T2"></typeparam>
	/// <typeparam name="T3"></typeparam>
	public class ArgumentMapProvider<T1, T2, T3> : ArgumentMapProvider {

		public ArgumentMapProvider(string nameArg1, string nameArg2, string nameArg3) : base(nameArg1, nameArg2, nameArg3) {
			ArgTypes = new Type[] { typeof(T1), typeof(T2), typeof(T3) };
		}

		/// <summary>
		/// Construct a new <see cref="ArgumentMap{T1, T2, T3}"/> from the given arguments. Missing arguments will be substituted with <see langword="default"/>.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// <exception cref="InvalidCastException">If the given parameter types don't match up.</exception>
		public ArgumentMap<T1, T2, T3> Parse(params object[] args) {
			return new ArgumentMap<T1, T2, T3>(args.GetOrDefault<T1>(this, 0, Context), args.GetOrDefault<T2>(this, 1, Context), args.GetOrDefault<T3>(this, 2, Context));
		}

	}

	/// <summary>
	/// An <see cref="ArgumentMapProvider{T1, T2, T3, T4}"/> taking in four arguments.
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	/// <typeparam name="T2"></typeparam>
	/// <typeparam name="T3"></typeparam>
	/// <typeparam name="T4"></typeparam>
	public class ArgumentMapProvider<T1, T2, T3, T4> : ArgumentMapProvider {

		public ArgumentMapProvider(string nameArg1, string nameArg2, string nameArg3, string nameArg4) : base(nameArg1, nameArg2, nameArg3, nameArg4) {
			ArgTypes = new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) };
		}

		/// <summary>
		/// Construct a new <see cref="ArgumentMap{T1, T2, T3, T4}"/> from the given arguments. Missing arguments will be substituted with <see langword="default"/>.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// <exception cref="InvalidCastException">If the given parameter types don't match up.</exception>
		public ArgumentMap<T1, T2, T3, T4> Parse(params object[] args) {
			return new ArgumentMap<T1, T2, T3, T4>(args.GetOrDefault<T1>(this, 0, Context), args.GetOrDefault<T2>(this, 1, Context), args.GetOrDefault<T3>(this, 2, Context), args.GetOrDefault<T4>(this, 3, Context));
		}

	}

	/// <summary>
	/// An <see cref="ArgumentMapProvider{T1, T2, T3, T4, T5}"/> taking in five arguments.
	/// </summary>
	/// <typeparam name="T1"></typeparam>
	/// <typeparam name="T2"></typeparam>
	/// <typeparam name="T3"></typeparam>
	/// <typeparam name="T4"></typeparam>
	/// <typeparam name="T5"></typeparam>
	public class ArgumentMapProvider<T1, T2, T3, T4, T5> : ArgumentMapProvider {

		public ArgumentMapProvider(string nameArg1, string nameArg2, string nameArg3, string nameArg4, string nameArg5) : base(nameArg1, nameArg2, nameArg3, nameArg4, nameArg5) {
			ArgTypes = new Type[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) };
		}

		/// <summary>
		/// Construct a new <see cref="ArgumentMap{T1, T2, T3, T4, T5}"/> from the given arguments. Missing arguments will be substituted with <see langword="default"/>.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// <exception cref="InvalidCastException">If the given parameter types don't match up.</exception>
		public ArgumentMap<T1, T2, T3, T4, T5> Parse(params object[] args) {
			return new ArgumentMap<T1, T2, T3, T4, T5>(args.GetOrDefault<T1>(this, 0, Context), args.GetOrDefault<T2>(this, 1, Context), args.GetOrDefault<T3>(this, 2, Context), args.GetOrDefault<T4>(this, 3, Context), args.GetOrDefault<T5>(this, 4, Context));
		}

	}
}
