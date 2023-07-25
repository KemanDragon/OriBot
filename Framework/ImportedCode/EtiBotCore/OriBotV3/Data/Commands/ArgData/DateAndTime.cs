using EtiBotCore.Utility.Marshalling;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OldOriBot.Data.Commands.ArgData {
	public class DateAndTime : ICommandArg<DateAndTime> {

		public DateAndTime() { }

		public DateTimeOffset Inner { get; }

		public DateAndTime(DateTimeOffset inner) {
			Inner = inner;
		}

		public DateAndTime From(string instance, object inContext) {
			return new DateAndTime(DateTimeOffset.Parse(instance, CultureInfo.GetCultureInfo("en-GB").DateTimeFormat, DateTimeStyles.AssumeUniversal).UtcDateTime);
		}

		object ICommandArg.From(string instance, object inContext) => ((ICommandArg<DateAndTime>)this).From(instance, inContext);

		public static implicit operator DateTimeOffset(DateAndTime dt) {
			return dt.Inner;
		}
	}
}
