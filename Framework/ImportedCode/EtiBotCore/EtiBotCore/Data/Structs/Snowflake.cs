using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using EtiBotCore.Data.JsonConversion;
using Newtonsoft.Json;

namespace EtiBotCore.Data.Structs {

	/// <summary>
	/// All Discord IDs are a special object known as a "Snowflake" (see <see href="https://discord.com/developers/docs/reference#snowflakes"/>)<para/>
	/// By default, these are encoded as 64 bit unsigned integers, but this struct exposes the extra data inside.
	/// It can be implicitly cast to and from <see cref="ulong"/>, making certain conversions and operations incredibly straightforward.
	/// </summary>
	[JsonConverter(typeof(SnowflakeConverter))]
	public readonly struct Snowflake : IEquatable<Snowflake>, IEquatable<ulong>, IEquatable<DateTimeOffset>, IComparable<Snowflake>, IComparable<ulong>, IComparable<DateTimeOffset> {

		/// <summary>
		/// The first second of 2015. Internally, all snowflake timestamps are relative to this value.
		/// </summary>
		[JsonIgnore] public const long DISCORD_EPOCH = 1420070400000;

		/// <summary>
		/// A <see cref="Snowflake"/> constructed from a GUID of 0. Identical to <see langword="default"/>.<para/>
		/// This <see cref="Snowflake"/> does not exist on Discord.
		/// </summary>
		[JsonIgnore] public static readonly Snowflake Invalid = default;

		/// <summary>
		/// The minimum / earliest possible <see cref="Snowflake"/> in existence. No <see cref="Snowflake"/>s exist on Discord with a value smaller than this.
		/// </summary>
		[JsonIgnore] public static readonly Snowflake MinValue = new Snowflake(DISCORD_EPOCH);

		/// <summary>
		/// A <see cref="Snowflake"/> with a GUID of 18446744073709551615.
		/// </summary>
		[JsonIgnore] public static readonly Snowflake MaxValue = new Snowflake(ulong.MaxValue);

		/// <summary>
		/// A <see cref="Snowflake"/> with its time component set to the current time. All other values are zero.
		/// </summary>
		[JsonIgnore] public static Snowflake UtcNow => FromDateTimeOffset(DateTimeOffset.UtcNow);

		#region Fields

		/// <summary>
		/// Whether or not this snowflake is valid (that is, greater than or equal to <see cref="MinValue"/> and less than or equal to <see cref="MaxValue"/>)
		/// </summary>
		[JsonIgnore] public readonly bool IsValid;

		/// <summary>
		/// The ulong value assembled from this <see cref="Snowflake"/>
		/// </summary>
		public readonly ulong Value;

		/// <summary>
		/// The unix epoch timestamp of this <see cref="Snowflake"/> in milliseconds, which corresponds to its creation.<para/>
		/// Note: This already accomodates for <see cref="DISCORD_EPOCH"/> -- This is a true unix epoch value, and is <strong>not</strong> relative to the Discord epoch.
		/// </summary>
		[JsonIgnore] public readonly long Timestamp;

		/// <summary>
		/// The internal worker ID of this <see cref="Snowflake"/>, which is something specific to Discord and is probably useless to you.
		/// </summary>
		[JsonIgnore] public readonly byte InternalWorkerID;

		/// <summary>
		/// The internal process ID of this <see cref="Snowflake"/>, which is something specific to Discord and is probably useless to you.
		/// </summary>
		[JsonIgnore] public readonly byte InternalProcessID;

		/// <summary>
		/// The <em>n</em>th ID generated on the process that this <see cref="Snowflake"/> was generated on (denoted by <see cref="InternalProcessID"/>).
		/// </summary>
		[JsonIgnore] public readonly short Increment;

		#endregion

		/// <summary>
		/// Create a new snowflake from the given ulong ID.
		/// </summary>
		/// <param name="value"></param>
		public Snowflake(ulong value) {
			Value = value;
			Timestamp = (long)((value >> 22) + DISCORD_EPOCH);
			InternalWorkerID = (byte)((value & 0x3E0000) >> 17);
			InternalProcessID = (byte)((value & 0x1F000) >> 12);
			Increment = (short)(value & 0xFFF);
			IsValid = Value >= DISCORD_EPOCH;
		}

		/// <summary>
		/// Translates <see cref="Timestamp"/> into a new <see cref="DateTimeOffset"/>, which reflects the creation time of this Snowflake.
		/// </summary>
		/// <remarks>
		/// This timestamp will always be relative to UTC+0 and requires no further conversion in the scope of the <see cref="DISCORD_EPOCH"/>.
		/// </remarks>
		/// <returns></returns>
		public DateTimeOffset ToDateTimeOffset() => DateTimeOffset.FromUnixTimeMilliseconds(Timestamp);

		/// <summary>
		/// Constructs a <see cref="Snowflake"/> from the given <see cref="DateTimeOffset"/>. This sets all other internal values to 0 (the worker and process IDs as well as the increment).
		/// </summary>
		/// <param name="time">The desired timestamp of the fabricated <see cref="Snowflake"/></param>
		/// <param name="maxInternalData">If <see langword="true"/>, the internal data (worker ID, process ID, and increment) will be set to the highest available values.</param>
		/// <returns></returns>
		public static Snowflake FromDateTimeOffset(DateTimeOffset time, bool maxInternalData = false) {
			ulong val = (ulong)time.ToUnixTimeMilliseconds();
			val -= DISCORD_EPOCH;
			val <<= 22;
			if (maxInternalData) {
				val |= 0x3FFFFF;
			}
			return new Snowflake(val);
		}

		/// <summary>
		/// Tries to convert a string representation of a numeric snowflake into its <see cref="Snowflake"/> equivalent. Effectively identical to <see cref="ulong.TryParse(string?, out ulong)"/>
		/// </summary>
		/// <inheritdoc cref="ulong.TryParse(string?, out ulong)"/>
		public static bool TryParse(string? s, out Snowflake result) {
			if (ulong.TryParse(s, out ulong numVal)) {
				result = new Snowflake(numVal);
				return true;
			}
			result = default;
			return false;
		}

		/// <summary>
		/// Converts a string representation of a snowflake into its <see cref="Snowflake"/> equivalent.
		/// </summary>
		/// <returns>A 64-bit unsigned integer equivalent to the number contained in <paramref name="s"/>, represented as a <see cref="Snowflake"/>.</returns>
		/// <inheritdoc cref="ulong.Parse(string)"/>
		public static Snowflake Parse(string s) {
			return ulong.Parse(s);
		}

		/// <summary>
		/// Given a numeric snowflake, a mention to a user or role, or a channel link, this will attempt to extract the snowflake.
		/// </summary>
		/// <param name="s"></param>
		/// <param name="result"></param>
		/// <param name="type">The type of snowflake this is.</param>
		/// <returns></returns>
		public static bool TryExtract(string? s, out Snowflake result, out SnowflakeType type) {
			bool straightSuccess = TryParse(s, out result);
			if (straightSuccess) {
				type = SnowflakeType.Ambiguous;
				return true;
			}

			// @"(<@(!?|&?))(\d+)(>)";
			Match ping = Regex.Match(s, Constants.REGEX_ANY_MENTION);
			if (ping.Success) {
				// 0 is the matched string in full. Just so you remember.
				type = ping.Groups[2].Value.Contains("&") ? SnowflakeType.Role : SnowflakeType.User;
				return TryParse(ping.Groups[3].Value, out result);
			}

			// @"(<#(!?|&?))(\d+)(>)"
			Match channel = Regex.Match(s, Constants.REGEX_CHANNEL);
			if (channel.Success) {
				type = SnowflakeType.Channel;
				return TryParse(channel.Groups[2].Value, out result);
			}

			type = default;
			result = default;
			return false;
		}

		#region Casting

		/// <summary>
		/// Translates this <see cref="Snowflake"/> by providing <see cref="Value"/>.
		/// </summary>
		/// <param name="sf"></param>
		public static implicit operator ulong(Snowflake sf) {
			return sf.Value;
		}

		/// <summary>
		/// Constructs a new <see cref="Snowflake"/> from the given <see cref="ulong"/> ID.
		/// </summary>
		/// <param name="ul"></param>
		public static implicit operator Snowflake(ulong ul) {
			return new Snowflake(ul);
		}

		#endregion

		#region Equality

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(Snowflake left, Snowflake right) => left.Equals(right);

		public static bool operator !=(Snowflake left, Snowflake right) => !left.Equals(right);

		public static bool operator ==(Snowflake left, ulong right) => left.Equals(right);

		public static bool operator !=(Snowflake left, ulong right) => !left.Equals(right);

		public static bool operator ==(ulong left, Snowflake right) => right.Equals(left);

		public static bool operator !=(ulong left, Snowflake right) => !right.Equals(left);

		public static bool operator ==(Snowflake left, long right) => left.Equals((ulong)right);

		public static bool operator !=(Snowflake left, long right) => !left.Equals((ulong)right);

		public static bool operator ==(long left, Snowflake right) => right.Equals((ulong)left);

		public static bool operator !=(long left, Snowflake right) => !right.Equals((ulong)left);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <inheritdoc/>
		public override bool Equals(object? obj) {
			if (ReferenceEquals(obj, this)) return true;
			if (obj is Snowflake sf) return Equals(sf);
			if (obj is ulong ul) return Equals(ul);
			return false;
		}

		/// <inheritdoc/>
		public bool Equals(Snowflake other) {
			return Value == other.Value;
		}

		/// <inheritdoc/>
		public bool Equals(ulong other) {
			return Value == other;
		}

		/// <inheritdoc/>
		public bool Equals(DateTimeOffset other) {
			return (ulong)(other.ToUnixTimeMilliseconds() - DISCORD_EPOCH) == Value;
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return Value.GetHashCode();
		}

		#endregion

		#region Strings

		/// <summary>
		/// Returns <see cref="Value"/> as a <see cref="string"/>.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return Value.ToString();
		}

		/// <summary>
		/// Returns a more descriptive string containing all applicable fields of this <see cref="Snowflake"/>.
		/// </summary>
		public string ToRichString() {
			return $"Snowflake {Value} [CreatedEpoch={Timestamp} ({GetDisplayTimestampMS()} // Age {DateTimeOffset.UtcNow - ToDateTimeOffset()}), InternalWorkerID={InternalWorkerID}, InternalProcessID={InternalProcessID}, Increment={Increment}]";
		}

		/// <summary>
		/// Returns a <see cref="string"/> timestamp formatted with milliseconds.
		/// </summary>
		/// <returns></returns>
		public string GetDisplayTimestampMS() {
			DateTimeOffset dt = ToDateTimeOffset();
			return $"{dt.Day:D2}/{dt.Month:D2}/{dt.Year:D4} {dt.Hour:D2}:{dt.Minute:D2}:{dt.Second:D2} + {dt.Millisecond}ms UTC+00:00";
		}

		/// <inheritdoc/>
		public int CompareTo([AllowNull] Snowflake other) {
			return Value.CompareTo(other.Value);
		}

		/// <inheritdoc/>
		public int CompareTo([AllowNull] ulong other) {
			return Value.CompareTo(other);
		}

		/// <inheritdoc/>
		public int CompareTo([AllowNull] DateTimeOffset other) {
			return ToDateTimeOffset().CompareTo(other);
		}

		#endregion
	}
}
