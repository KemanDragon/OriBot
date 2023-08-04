using EtiBotCore.Exceptions;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Payloads.Events;
using EtiBotCore.Payloads.Events.Intents.GuildPresences;
using EtiBotCore.Utility.Extension;
using EtiLogger.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.Data {

	/// <summary>
	/// Provides a method of getting the event name for a <see cref="PayloadDataObject"/>
	/// </summary>
	internal static class PayloadEventRegistry {

		/// <summary>
		/// A binding from event name to its associated <see cref="Type"/> (e.g. <c>PRESENCE_UPDATE</c> to <see cref="PresenceUpdateEvent"/>).
		/// </summary>
		public static IReadOnlyDictionary<string, Type> EventToTypeBinding { get; }

		/// <summary>
		/// Given an event name, this returns its associated <see cref="Type"/> from <see cref="EventToTypeBinding"/>, or <see langword="null"/> if no associated <see cref="Type"/> was found.
		/// </summary>
		/// <param name="eventName">The name of the event as Discord sends it, e.g. <c>PRESENCE_UPDATE</c>.</param>
		/// <returns></returns>
		public static Type? GetTypeFromEventName(string eventName) {
			if (EventToTypeBinding.TryGetValue(eventName, out Type? type)) {
				return type;
			}
			return null;
		}

		/// <summary>
		/// Given a <see cref="Payload"/>, this will look at its event name and return an instance of the corresponding event as the given type.
		/// </summary>
		/// <typeparam name="T">The specific event type to return.</typeparam>
		/// <param name="payload">The payload that contains the event.</param>
		/// <returns>An instance of the given event type <typeparamref name="T"/> acquired from the payload.</returns>
		/// <exception cref="ArgumentException">If the payload's opcode is not <see cref="PayloadOpcode.Dispatch"/>.</exception>
		/// <exception cref="ValueNotFoundException">If the payload's event name could not be resolved.</exception>
		public static T CreateInstanceForEventPayload<T>(Payload payload) where T : IEvent {
			if (payload.Operation != PayloadOpcode.Dispatch) throw new ArgumentException("The input payload was not a dispatch payload, and does not have an event name as a result!", nameof(payload));
			Type? eventType = GetTypeFromEventName(payload.EventName ?? "");
			if (eventType == null) throw new ValueNotFoundException($"The given event name {payload.EventName} could not be resolved!");
			return payload.GetObjectFromData<T>();
		}

		/// <summary>
		/// Given a <see cref="Payload"/>, this will look at its event name and return an instance of the corresponding event.
		/// </summary>
		/// <param name="payload">The payload that contains the event.</param>
		/// <returns>An instance of the given event from the payload.</returns>
		/// <exception cref="ArgumentException">If the payload's opcode is not <see cref="PayloadOpcode.Dispatch"/>, or if the payload's <see cref="Payload.Data"/> field is not a <see cref="JObject"/>.</exception>
		/// <exception cref="ValueNotFoundException">If the payload's event name could not be resolved.</exception>
		public static IEvent? CreateInstanceForEventPayload(Payload payload) {
			if (payload.Operation != PayloadOpcode.Dispatch) throw new ArgumentException("The input payload was not a dispatch payload, and does not have an event name as a result!", nameof(payload));
			if (!(payload.Data is JObject)) throw new ArgumentException("The payload's data is not an instance of JObject!", nameof(payload));
			Type? eventType = GetTypeFromEventName(payload.EventName ?? "");
			if (eventType == null) throw new ValueNotFoundException($"The given event name {payload.EventName} could not be resolved!");
			JObject dataObj = (JObject)payload.Data;
			object? o = null;
			try {
				o = dataObj.ToObject(eventType);
			} catch (Exception exc) {
				Logger.Default.WriteException(exc);
			}
			return o as IEvent;
		}

		/// <summary>
		/// This is used to reference this class. The method does nothing but trigger the static class initializer if it hasn't been triggered already.
		/// </summary>
		public static void Initialize() { }

		static PayloadEventRegistry() {
			Dictionary<string, Type> binding = new Dictionary<string, Type>();

			// Grab all types in this assembly.
			Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();

			// Filter it to types in the events namespace.
			allTypes = allTypes.Where(type => type.Implements(typeof(IEvent)) && !type.HasAttribute<IgnoreEventAttribute>() && !type.HasAttribute<ObsoleteAttribute>()).ToArray();

			// Register
			foreach (Type t in allTypes) {
				string name = GetEventName(t);
				binding[name] = t;
				Logger.Default.WriteLine("Registered event " + name, LogLevel.Trace);
			}

			// Set
			EventToTypeBinding = binding;
		}

		/// <summary>
		/// Returns the event name from the given type's name.
		/// </summary>
		/// <param name="payloadEventType"></param>
		/// <returns></returns>
		public static string GetEventName(Type payloadEventType) {
			string typeName = payloadEventType.Name;
			if (!typeName.EndsWith("Event")) throw new ArgumentException("Type name does not end in \"Event\"! It should end in this.");
			typeName = typeName.Substring(0, typeName.Length - 5);
			string newName = "";
			for (int i = 0; i < typeName.Length; i++) {
				char c = typeName[i];
				if (i != 0 && char.IsUpper(c)) {
					newName += "_";
				}
				newName += char.ToUpper(c);
			}
			return newName;
		}

	}
}
