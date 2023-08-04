using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.Data.JsonConversion;
using Newtonsoft.Json;

namespace EtiBotCore.Data.Structs {

	/// <summary>
	/// An ISO8601 Timestamp
	/// </summary>
	[JsonConverter(typeof(TimestampConverter))]
	public readonly struct ISO8601 : IEquatable<ISO8601>, IEquatable<string>, IEquatable<DateTime>, IEquatable<DateTimeOffset> {

		/// <summary>
		/// An <see cref="ISO8601"/> equivalent to <see cref="DateTimeOffset.UnixEpoch"/>.
		/// </summary>
		public static readonly ISO8601 Epoch = new ISO8601(DateTimeOffset.UnixEpoch);

		/// <summary>
		/// The string timestamp of this <see cref="ISO8601"/> timestamp.
		/// </summary>
		public readonly string Timestamp;

		/// <summary>
		/// The equivalent <see cref="DateTime"/> of this <see cref="ISO8601"/> timestamp.
		/// </summary>
		public readonly DateTimeOffset DateTime;

		/// <summary>
		/// Converts the given <see cref="string"/> into an <see cref="ISO8601"/> timestamp.
		/// </summary>
		/// <param name="timestamp">The ISO8601 timestamp to convert into this struct.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="timestamp"/> is <see langword="null"/>.</exception>
		/// <exception cref="FormatException">If <paramref name="timestamp"/> is not a valid ISO8601 timestamp.</exception>
		public ISO8601(string timestamp) {
			if (DateTimeOffset.TryParse(timestamp, null, DateTimeStyles.RoundtripKind, out DateTimeOffset ofs)) {
				DateTime = ofs.ToUniversalTime();
			} else {
				DateTime = DateTimeOffset.Parse(timestamp).ToUniversalTime();
			}
			Timestamp = timestamp;
		}

		/// <summary>
		/// Converts the given <see cref="System.DateTime"/> into an <see cref="ISO8601"/> timestamp.
		/// </summary>
		/// <param name="time"></param>
		public ISO8601(DateTime time) {
			DateTime = time;
			Timestamp = time.ToString("s", CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Converts the given <see cref="DateTimeOffset"/> into an <see cref="ISO8601"/> timestamp.
		/// </summary>
		/// <param name="time"></param>
		public ISO8601(DateTimeOffset time) {
			DateTime = time;
			Timestamp = time.ToString("s", CultureInfo.InvariantCulture);
		}

		/// <returns>The same string as <see cref="Timestamp"/></returns>
		public override string ToString() {
			return Timestamp;
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return HashCode.Combine(Timestamp, DateTime);
		}

		/// <inheritdoc/>
		public override bool Equals(object? obj) {
			if (obj is ISO8601 other) return Equals(other);
			if (obj is string otherStr) return Equals(otherStr);
			if (obj is DateTime otherDt) return Equals(otherDt);
			return base.Equals(obj);
		}

		/// <summary>
		/// Returns whether or not this <see cref="ISO8601"/> has the same time that is stored inside the given <see cref="ISO8601"/>.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(ISO8601 other) {
			if (ReferenceEquals(this, other)) return true;
			return DateTime.Equals(other.DateTime) || Timestamp.Equals(other.Timestamp);
		}

		/// <summary>
		/// Returns whether or not <see cref="Timestamp"/> is equal to the given string.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(string? other) {
			if (other is null) return false;
			return Timestamp.Equals(other);
		}

		/// <summary>
		/// Returns whether or not this <see cref="DateTime"/> is equal to the given <see cref="System.DateTime"/>.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(DateTime other) {
			return DateTime.Equals(other);
		}

		/// <inheritdoc/>
		public bool Equals(DateTimeOffset other) {
			return DateTime.EqualsExact(other);
		}
	}
}
