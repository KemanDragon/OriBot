using EtiBotCore.Data.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OldOriBot.Utility {
	public static class TimeExtensions {
		/// <summary>
		/// Returns this <see cref="DateTimeOffset"/> in the format of DD/MM/YYYY HH:MM:SS UTC
		/// </summary>
		/// <param name="dt"></param>
		/// <returns></returns>
		public static string InEUFormat(this DateTimeOffset dt, bool withMillis = false) {
			dt = dt.ToUniversalTime();
			string tx = $"{dt.Day:D2}/{dt.Month:D2}/{dt.Year:D4} {dt.Hour:D2}:{dt.Minute:D2}:{dt.Second:D2}";
			if (withMillis) {
				tx += $"+ {dt.Millisecond}ms UTC";
			} else {
				tx += " UTC";
			}
			return tx;
		}

		/// <summary>
		/// Converts this <see cref="DateTimeOffset"/> into Discord's timestamp format <c>&lt;t:UNIX:MODE&gt;</c>, which displays relative to the reader. See <see href="https://discord.com/developers/docs/reference#message-formatting-timestamp-styles">Timestamp Styles</see> for more information.
		/// </summary>
		/// <param name="dt"></param>
		/// <param name="mode">The mode to display time in. See Timestamp Styles for more information.</param>
		/// <returns></returns>
		public static string AsDiscordTimestamp(this DateTimeOffset dt, char mode = 'F') {
			long unix = dt.ToUnixTimeSeconds();
			return $"<t:{unix}:{mode}>";
		}

		/// <summary>
		/// Converts this <see cref="Snowflake"/>'s internal <see cref="DateTimeOffset"/> into Discord's timestamp format <c>&lt;t:UNIX:MODE&gt;</c>, which displays relative to the reader.
		/// </summary>
		/// <param name="sf"></param>
		/// <param name="mode">The mode to display time in. See <see href="https://discord.com/developers/docs/reference#message-formatting-timestamp-styles">Timestamp Styles</see> for more information.</param>
		/// <returns></returns>
		public static string AsDiscordTimestamp(this Snowflake sf, char mode = 'F') => AsDiscordTimestamp(sf.ToDateTimeOffset(), mode);

		/// <summary>
		/// Returns how long ago this <see cref="DateTimeOffset"/> is compared to the given <paramref name="time"/>, or if it is <see langword="null"/>, <see cref="DateTimeOffset.UtcNow"/>.<para/>
		/// This will never return a negative, so it does not matter if <paramref name="originalTime"/> or <paramref name="time"/> is in the future.
		/// </summary>
		/// <param name="originalTime"></param>
		/// <returns></returns>
		public static string GetTimeDifferenceFrom(this DateTimeOffset originalTime, DateTimeOffset? time = null) {
			DateTimeOffset ogUtc = originalTime.ToUniversalTime();
			DateTimeOffset newUtc = time?.ToUniversalTime() ?? DateTimeOffset.UtcNow;

			TimeSpan span;
			if (ogUtc > newUtc) {
				span = ogUtc - newUtc;
			} else {
				span = newUtc - ogUtc;
			}
			span = span.Add(TimeSpan.FromSeconds(1)); // This is to prevent a very slight issue with time being 1 second short.
			return GetTimeDifference(span);
		}

		/// <summary>
		/// Returns the length of this <see cref="TimeSpan"/> as a cleanly formatted string.
		/// </summary>
		/// <param name="originalTime"></param>
		/// <returns></returns>
		public static string GetTimeDifference(this TimeSpan span) {
			int years = span.Days / 365;
			int days = span.Days % 365; // This clamps days to values between 0 and 364
			int months = (int)Math.Round(days / 30.4375); // 365.25 days in a year, so 30.4375 in a month.
			days %= 28;
			int weeks = days / 7;
			days %= 7;
			while (days >= 28) {
				weeks++;
				days -= 28;
			}
			while (weeks >= 4) {
				months++;
				weeks -= 4;
			}
			while (months >= 12) {
				years++;
				months -= 12;
			}
			string year = years == 1 ? "year" : "years";
			string month = months == 1 ? "month" : "months";
			string week = weeks == 1 ? "week" : "weeks";
			string day = days == 1 ? "day" : "days";
			string hour = span.Hours == 1 ? "hour" : "hours";
			string minute = span.Minutes == 1 ? "minute" : "minutes";
			string second = span.Seconds == 1 ? "second" : "seconds";
			return $"{years} {year}, {months}/12 {month}, {weeks} {week}, {days} {day}, {span.Hours} {hour}, {span.Minutes} {minute}, {span.Seconds} {second}";
		}

		/// <summary>
		/// Given the text <paramref name="time"/>, this will output the time in seconds, a unit of time, and the amount of time in the given unit of time.<para/>
		/// <paramref name="time"/> is expected to be a number with a single letter suffix e.g. 5s for 5 seconds (or just "5", as it is default), 10m for 10 minutes, 3w for 3 weeks, etc.
		/// </summary>
		/// <param name="time">The string containing the time, such as <c>5s</c>, <c>3d</c>, <c>2w</c>, etc.</param>
		/// <param name="timeSecs">The time in seconds.</param>
		/// <param name="timeUnit">The unit of time detected from the suffix.</param>
		/// <param name="baseUnit">The amount of time in the given unit (identical to the input number)</param>
		/// <param name="requireExplicitSecondDefinition">If <see langword="true"/>, time values with no suffix will throw an error (by default, no suffix assumes seconds)</param>
		/// <returns>Whether or not the conversion was successful.</returns>
		public static bool GetTimeFromText(string time, out ulong timeSecs, out TimeUnit timeUnit, out ulong baseUnit, bool requireExplicitSecondDefinition = false) {
			ulong multiplier = 1;
			time = time.ToLower();

			if (time.EndsWith("m")) {
				multiplier = 60;
				timeUnit = TimeUnit.Minutes;
			} else if (time.EndsWith("h")) {
				multiplier = 3600;
				timeUnit = TimeUnit.Hours;
			} else if (time.EndsWith("d")) {
				multiplier = 86400;
				timeUnit = TimeUnit.Days;
			} else if (time.EndsWith("w")) {
				multiplier = 604800;
				timeUnit = TimeUnit.Weeks;
			} else {
				// No known ending, or s ending.
				bool endsInS = time.EndsWith("s");
				bool endsInNumber = char.IsDigit(time.Last());

				bool isInvalid = requireExplicitSecondDefinition && !endsInS; // Requires s, doesn't end in s.
				isInvalid = isInvalid || (!endsInS && !endsInNumber); // Doesn't end in S, but also doesn't end in a number. It ends in a different letter or symbol.

				if (isInvalid) {
					if (!requireExplicitSecondDefinition) {
						//throw new CommandException(this, "Invalid number ending! Expected: `m` for minutes, `h` for hours, `d` for days, or `w` for weeks. The default unit is seconds.\nExample: `4d` for 4 days, `2m` for 2 minutes, `3` for 3 seconds.");
						timeSecs = 0;
						timeUnit = TimeUnit.Invalid;
						baseUnit = 0;
						return false;
					} else {
						//throw new CommandException(this, "Invalid number ending! Expected: `s` for seconds, `m` for minutes, `h` for hours, `d` for days, or `w` for weeks.\nExample: `4d` for 4 days, `2m` for 2 minutes, `3s` for 3 seconds.");
						timeSecs = 0;
						timeUnit = TimeUnit.Invalid;
						baseUnit = 0;
						return false;
					}
				}
				timeUnit = TimeUnit.Seconds;
			}

			if (char.IsLetter(time.Last())) time = time[0..^1];
			if (ulong.TryParse(time, out ulong timeNumber)) {
				timeSecs = timeNumber * multiplier;
				baseUnit = timeNumber;
				return true;
			}
			timeSecs = 0;
			baseUnit = 0;
			return false;
		}
	}

	public enum TimeUnit {
		/// <summary>
		/// An invalid unit of time.
		/// </summary>
		Invalid,

		/// <summary>
		/// The number is in seconds.
		/// </summary>
		Seconds,

		/// <summary>
		/// The number is in minutes.
		/// </summary>
		Minutes,

		/// <summary>
		/// The number is in hours.
		/// </summary>
		Hours,

		/// <summary>
		/// The number is in days.
		/// </summary>
		Days,

		/// <summary>
		/// The number is in weeks.
		/// </summary>
		Weeks
	}

}
