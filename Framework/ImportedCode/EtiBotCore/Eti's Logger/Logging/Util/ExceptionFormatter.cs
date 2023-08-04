using EtiLogger.Data.Structs;
using static EtiLogger.Data.Structs.Color;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EtiLogger.Logging.Util {

	/// <summary>
	/// A utility that can format exception messages for the console.
	/// </summary>
	public static class ExceptionFormatter {


		/// <summary>
		/// Red text <c>"-----------------------------------------------"</c> that is colored via VT codes.
		/// </summary>
		public static readonly string VT_SEPARATOR = new LogMessage.MessageComponent(
			"-----------------------------------------------",
			RED,
			new Color(0, 0, 0),
			false
		).ToVTString();

		/// <summary>
		/// Returns a formatted <see cref="LogMessage"/> for the given <see cref="Exception"/>. If it is an <see cref="AggregateException"/> it will return all children.
		/// </summary>
		/// <param name="exc"></param>
		/// <returns></returns>
		public static LogMessage GetExceptionMessage(Exception exc) {
			return GetExceptionMessage(exc, false);
		}

		private static LogMessage GetExceptionMessage(Exception exc, bool fromAggregate) {
			if (exc is null) throw new ArgumentNullException("exc");
			if (exc is AggregateException agg) {
				return GetAggregateThrowMessage(agg);
			} else if (exc is TypeInitializationException tix) {
				return GetTypeInitThrowMessage(tix);
			} else {
				return GetGenericThrowMessage(exc, !fromAggregate);
			}
		}

		/// <summary>
		/// Returns a formatted message fit for a generic exception, but where this exception is instantiated and not thrown. This omits the stack information, which will be missing.
		/// </summary>
		/// <param name="exc"></param>
		/// <param name="sayThrown">Whether or not to add the text <c>Thrown!</c> after the exception name.</param>
		/// <returns></returns>
		public static LogMessage GetUnthrownExceptionMessage(Exception exc, bool sayThrown = true) {
			LogMessage message = new LogMessage();
			message.AddComponent(new LogMessage.MessageComponent("[ ", RED, BLOOD_RED, false));
			message.AddComponent(new LogMessage.MessageComponent(exc.GetType().FullName + (sayThrown ? " Thrown!" : ""), GOLD, null, true));
			message.AddComponent(new LogMessage.MessageComponent(" ]: ", RED, null, false));
			message.AddComponent(new LogMessage.MessageComponent(exc.Message, ORANGE, null, false));

			return message;
		}

		/// <summary>
		/// Returns a formatted message fit for a generic exception.
		/// </summary>
		/// <param name="exc"></param>
		/// <param name="sayThrown">Whether or not to add the text <c>Thrown!</c> after the exception name.</param>
		/// <returns></returns>
		private static LogMessage GetGenericThrowMessage(Exception exc, bool sayThrown) {
			LogMessage message = new LogMessage();
			message.AddComponent(new LogMessage.MessageComponent("[ ", RED, BLOOD_RED, false));
			message.AddComponent(new LogMessage.MessageComponent(exc.GetType().FullName + (sayThrown ? " Thrown!" : ""), GOLD, null, true));
			message.AddComponent(new LogMessage.MessageComponent(" ]: ", RED, null, false));
			message.AddComponent(new LogMessage.MessageComponent(exc.Message, ORANGE, null, false));
			message.AddComponent(new LogMessage.MessageComponent("\nStack:\n", DARK_RED, null, true));
			message.AddComponent(new LogMessage.MessageComponent((exc.StackTrace ?? "Exception was instantiated but not thrown.") + "\n", DARK_RED, null, false));
			
			return message;
		}

		/// <summary>
		/// Returns a formatted message fit for an aggregate exception.
		/// </summary>
		/// <param name="aggExc"></param>
		/// <returns></returns>
		private static LogMessage GetAggregateThrowMessage(AggregateException aggExc) {
			LogMessage message = new LogMessage();
			message.AddComponent(new LogMessage.MessageComponent("[ ", RED, BLOOD_RED, false));
			message.AddComponent(new LogMessage.MessageComponent(aggExc.GetType().FullName + " Thrown!", GOLD, null, true));
			message.AddComponent(new LogMessage.MessageComponent(" ] -- ", RED, null, false));
			message.AddComponent(new LogMessage.MessageComponent("Inner Exceptions:", ORANGE, null, false));
			message.AddComponent(new LogMessage.MessageComponent("\nAggregate Source:\n", DARK_RED, null, true));
			message.AddComponent(new LogMessage.MessageComponent((aggExc.StackTrace ?? "Exception was instantiated but not thrown.") + "\n", DARK_RED, null, false));

			foreach (Exception inner in aggExc.InnerExceptions) {
				message = message.ConcatLocal(GetExceptionMessage(inner, true));
			}

			return message;
		}

		/// <summary>
		/// Returns a formatted message fit for a type init exception.
		/// </summary>
		/// <param name="typeInitExc"></param>
		/// <returns></returns>
		private static LogMessage GetTypeInitThrowMessage(TypeInitializationException typeInitExc) {
			LogMessage message = new LogMessage();
			message.AddComponent(new LogMessage.MessageComponent("[ ", RED, BLOOD_RED, false));
			message.AddComponent(new LogMessage.MessageComponent(typeInitExc.GetType().FullName + " Thrown!", GOLD, null, true));
			message.AddComponent(new LogMessage.MessageComponent(" ] -- ", RED, null, false));
			message.AddComponent(new LogMessage.MessageComponent("Inner Exception:", ORANGE, null, true));
			return message.ConcatLocal(GetExceptionMessage(typeInitExc.InnerException));
		}

	}
}
