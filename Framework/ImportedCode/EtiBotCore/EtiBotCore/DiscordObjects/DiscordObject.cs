using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EtiBotCore.Client;
using EtiBotCore.Clockwork;
using EtiBotCore.Data;
using EtiBotCore.Data.Container;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiBotCore.Exceptions.Marshalling;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Data;
using EtiBotCore.Payloads.Events;
using EtiBotCore.Utility.Extension;
using EtiLogger.Logging;
using Newtonsoft.Json.Serialization;

namespace EtiBotCore.DiscordObjects {

	/// <summary>
	/// The base class for all things that exist in Discord. Provides a number of utilities relevant to management of these objects between the bot and Discord.
	/// </summary>
	
	public abstract class DiscordObject : IEquatable<DiscordObject?>, IEquatable<Snowflake>, IEquatable<ulong>, IComparable<DiscordObject> {

		/// <summary>
		/// All instantiated DiscordObject instances.
		/// </summary>
		public static readonly List<DiscordObject> Everything = new List<DiscordObject>();

		/// <summary>
		/// A logger that can be used to log stuff about this <see cref="DiscordObject"/>
		/// </summary>
		internal Logger ObjectLogger { get; }

		#region Discord Defaults & Important Class Data

		/// <summary>
		/// The ID of this object.
		/// </summary>
		public virtual Snowflake ID { get; }

		/// <summary>
		/// Whether or not this <see cref="DiscordObject"/> is a clone of another object.
		/// </summary>
		public bool IsClone { get; private set; }

		/// <summary>
		/// Assuming <see cref="IsClone"/> is <see langword="true"/>, this is the original <see cref="DiscordObject"/> that it was created from.
		/// </summary>
		/// <remarks>
		/// Due to the singleton setup of this bot framework, this object is very likely in the <em>future</em> relative to this clone (meaning this clone contains old values).
		/// </remarks>
		public DiscordObject? Original { get; }

		/// <summary>
		/// Construct a new <see cref="DiscordObject"/> with the given ID.
		/// </summary>
		/// <param name="id"></param>
		public DiscordObject(Snowflake id) {
			Everything.Add(this);
			ID = id;
			IsClone = false;
			Original = null;
			ObjectLogger = new Logger(new LogMessage.MessageComponent($"[DiscordObject::{GetType().Name} {ID}] ", new EtiLogger.Data.Structs.Color(0x7f7bc7))) {
				DefaultLevel = LogLevel.Trace
			};
		}
		#endregion

		#region Property Lock Code

		/// <summary>
		/// Sets the property to the given value, or raises a <see cref="PropertyLockedException"/> if it is locked.
		/// </summary>
		/// <typeparam name="T">The type of value that this is.</typeparam>
		/// <param name="storage">A value used to store the data.</param>
		/// <param name="value">The desired value.</param>
		/// <param name="propertyName">The name of the property, which is automatically populated (so it should not be set unless an explicit override is desired).</param>
		/// <exception cref="PropertyLockedException">If this property is locked.</exception>
		/// <exception cref="ObjectDeletedException">If this is called when the object is deleted.</exception>
		protected internal virtual void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null) {
			if (Deleted) throw new ObjectDeletedException(this);
			if (IgnoresNetworkUpdates) throw new PropertyLockedException(propertyName);
			object? old = storage;
			object? literalOld = storage;
			if (storage is IList list) {
				old = list.LazyCopy();
			} else if (storage is IDictionary dict) {
				old = dict.LazyCopy();
			} else if (storage is DiscordObjectContainer container) {
				old = container.Clone();
			} else if (storage is DiscordObject dobj) {
				old = dobj.MemberwiseClone();
			} else if (storage is PermissionInformation prm) {
				old = prm.Clone();
			} else if (storage is PermissionContainer cnt) {
				old = cnt.Clone();
			}
			bool changed = SetProperty(ref storage, value);
			if (changed) {
				Changes[propertyName!] = (GetType().GetProperty(propertyName!)!.GetJsonName() ?? propertyName!, literalOld, old, typeof(T));
			}
		}

		/// <summary>
		/// For custom container objects, this registers a change.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="oldValue"></param>
		/// <param name="propertyName"></param>
		internal void RegisterChange<T>(T oldValue, string propertyName) {
			object? old = oldValue;
			object? literalOld = oldValue;
			if (oldValue is IList list) {
				old = list.LazyCopy();
			} else if (oldValue is IDictionary dict) {
				old = dict.LazyCopy();
			} else if (oldValue is DiscordObjectContainer container) {
				old = container.Clone();
			} else if (oldValue is DiscordObject dobj) {
				old = dobj.MemberwiseClone();
			} else if (oldValue is PermissionInformation prm) {
				old = prm.Clone();
			} else if (oldValue is PermissionContainer cnt) {
				old = cnt.Clone();
			}
			Changes[propertyName] = (GetType().GetProperty(propertyName)?.GetJsonName() ?? propertyName, literalOld, old, typeof(T));
		}

		/// <summary>
		/// Returns whether or not a change for the given property is registered.
		/// </summary>
		/// <param name="propertyName"></param>
		internal bool HasChange(string propertyName) {
			return Changes.ContainsKey(propertyName);
		}

		private bool SetProperty<T>(ref T storage, T value) {
			if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
			storage = value;
			return true;
		}

		/// <summary>
		/// Automatically detects required permissions. Throws <see cref="InsufficientPermissionException"/> if the bot in the given server does not have the given permissions.<para/>
		/// This checks for <see cref="Permissions.Administrator"/>, and if it is present, this will never throw.
		/// </summary>
		/// <param name="inServer"></param>
		/// <param name="inChannel"></param>
		/// <param name="requiredPermissions"></param>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the given permissions in the given server and channel.</exception>
		internal static void EnforcePermissions(Guild inServer, GuildChannelBase inChannel, Permissions requiredPermissions) {
			Permissions allowed = inServer.BotMember.GetPermissionsInChannel(inChannel);
			if (allowed.HasFlag(Permissions.Administrator)) return;
			if (allowed.HasFlag(requiredPermissions)) return;
			throw new InsufficientPermissionException(requiredPermissions);
		}

		/// <inheritdoc cref="EnforcePermissions(Guild, GuildChannelBase, Permissions)"/>
		internal static void EnforcePermissions(GuildChannelBase inChannel, Permissions requiredPermissions) {
			Guild inServer = inChannel.Server!;
			Permissions allowed = inServer.BotMember.GetPermissionsInChannel(inChannel);
			if (allowed.HasFlag(Permissions.Administrator)) return;
			if (allowed.HasFlag(requiredPermissions)) return;
			throw new InsufficientPermissionException(requiredPermissions);
		}

		/// <summary>
		/// Automatically detects required permissions. Throws <see cref="InsufficientPermissionException"/> if the bot in the given server does not have the given permissions.<para/>
		/// This checks for <see cref="Permissions.Administrator"/>, and if it is present, this will never throw.
		/// </summary>
		/// <param name="inServer"></param>
		/// <param name="requiredPermissions"></param>
		/// <exception cref="ObjectUnavailableException">If the server is suffering from an outage.</exception>
		/// <exception cref="InsufficientPermissionException">If the bot does not have the given permissions in the given server and channel.</exception>
		internal static void EnforcePermissions(Guild inServer, Permissions requiredPermissions) {
			Permissions allowed = inServer.BotMember.AllowedServerPermissions;
			if (allowed.HasFlag(Permissions.Administrator)) return;
			if (allowed.HasFlag(requiredPermissions)) return;
			throw new InsufficientPermissionException(requiredPermissions);
		}

		#endregion

		#region Changes & Change State Values

		/// <summary>
		/// If <see langword="true"/>, this object will defer any changes coming in from Discord and is in a writable state.
		/// </summary>
		public bool IgnoresNetworkUpdates { get; private set; } = true;

		/// <summary>
		/// An event that can be used to wait until the object is unlocked.
		/// </summary>
		public readonly ManualResetEventSlim UnlockedEvent = new ManualResetEventSlim(true);

		/// <summary>
		/// If <see langword="true"/>, this object has been deleted. Attempting to call <see cref="BeginChanges"/> on a deleted object will throw an <see cref="ObjectDeletedException"/>.
		/// </summary>
		public bool Deleted { get; internal set; }

		/// <summary>
		/// The changes that have been made while this object was mutable.
		/// </summary>
		private readonly Dictionary<string, object> Changes = new Dictionary<string, object>();

		/// <summary>
		/// Must be called before changes can be made to this object.<para/>
		/// <strong>This will lock the object from network updates. If the object is updated in Discord, the object will IGNORE received network changes until <see cref="IgnoresNetworkUpdates"/> is false!</strong>
		/// </summary>
		/// <param name="waitForUnlock">If <see langword="true"/>, this method will block if the object is currently unlocked (being changed elsewhere) and yield until locked once more before starting changes, This will avoid the <see cref="PropertyLockedException"/>.</param>
		/// <exception cref="InvalidOperationException">If <see cref="IgnoresNetworkUpdates"/> is already <see langword="false"/>.</exception>
		/// <exception cref="ObjectDeletedException">If <see cref="Deleted"/> is <see langword="true"/>.</exception>
		public void BeginChanges(bool waitForUnlock = false) {
			if (Deleted) throw new ObjectDeletedException(this);
			if (waitForUnlock) UnlockedEvent.Wait();
			if (!IgnoresNetworkUpdates) throw new InvalidOperationException($"{this} object is already expecting changes! Cannot start changes.");
			IgnoresNetworkUpdates = false;
			UnlockedEvent.Reset();
		}

		/// <summary>
		/// Called to signify that changes are done being made. Additionally, this sends the object to Discord. Contrary to <see cref="BeginChanges"/>, this will NOT throw an <see cref="ObjectDeletedException"/> if the object just so happened to be deleted while being edited.<para/>
		/// </summary>
		/// <param name="reasonArray">A complex dictionary binding property names to a reason for why that property was changed. This is used for audit logging on Discord. An example for a Member might be to set <c>["Nickname"] = "Their nickname was inappropriate"</c></param>
		/// <exception cref="InvalidOperationException">If <see cref="IgnoresNetworkUpdates"/> is already <see langword="true"/>.</exception>
		/// <exception cref="EditingTooFastException">If this is called too frequently and Discord's rate limits do not permit this.</exception>
		public async Task ApplyChanges(Dictionary<string, string>? reasonArray) {
			string? endReason = null;
			if (reasonArray != null) {
				endReason = "";
				foreach (KeyValuePair<string, string> reason in reasonArray) {
					endReason += $"{reason.Key}-{reason.Value}\n";
				}
				if (endReason.Length > 512) {
					endReason = endReason.Substring(0, 512);
					ObjectLogger.WriteWarning("The complete reason defined in ApplyChanges ended up being over 512 chars long (the maximum length)! It has been cut short.");
				}
			}

			await ApplyChanges(endReason);
		}

		/// <summary>
		/// Called to signify that changes are done being made. Additionally, this sends the object to Discord. Contrary to <see cref="BeginChanges"/>, this will NOT throw an <see cref="ObjectDeletedException"/> if the object just so happened to be deleted while being edited.<para/>
		/// </summary>
		/// <param name="reason">The reason this was changed. This should generally only be used of all changes are for the same reason, or if it was a single property change.</param>
		/// <exception cref="Exception">If any errors occur when trying to run SendChangesToDiscord</exception>
		public async Task<HttpResponseMessage?> ApplyChanges(string? reason = null) {
			if (IgnoresNetworkUpdates) throw new InvalidOperationException("This object was not expecting changes! Cannot apply changes.");
			// ApplyLimiter.OperationPerformed();
			
			if (Changes.Count == 0) {
				IgnoresNetworkUpdates = true;
				return null;
			}
		
			try {
				HttpResponseMessage? response = await SendChangesToDiscord(Changes, reason);
				if (!(response?.IsSuccessStatusCode ?? false)) {
					ObjectLogger.WriteCritical(string.Format("Reverting changes to <<{0}>> due to network failure...", ToString()));
					UndoChanges();
					return response;
				}
				IgnoresNetworkUpdates = true;
				Changes.Clear();
				UnlockedEvent.Set();

				return response;
			} catch {
				ObjectLogger.WriteCritical(string.Format("Reverting changes to <<{0}>> due to an exception being thrown...", ToString()));
				UndoChanges();
				throw;
			}
		}

		/// <summary>
		/// Restores this object to its previous state when <see cref="BeginChanges"/> was called, and then sets <see cref="IgnoresNetworkUpdates"/> to true. This cancels any ongoing edits.
		/// </summary>
		public void UndoChanges() {
			if (Deleted) throw new ObjectDeletedException(this);
			if (IgnoresNetworkUpdates) throw new InvalidOperationException("The object is not being changed!");
			Type thisType = GetType();
			foreach (string name in Changes.Keys) {
				(string _, object originalOld, object potentiallyNew, Type objType) = (ValueTuple<string, object, object, Type>)Changes[name];
				try {
					FieldInfo? thisField = thisType.GetField("_" + name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? thisType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					FieldInfo? objField = objType.GetField("_" + name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? objType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

					if (ReferenceEquals(originalOld, potentiallyNew)) {
						// No special garbage here. Everything that has `SetProperty` needs a backing field, which I simply give the same name with a prepended _

						thisField!.SetValue(this, originalOld);
					} else {
						// special gargabe.
						if (originalOld is IList || originalOld is IDictionary || originalOld is PermissionInformation || originalOld is PermissionContainer) {
							// just set this to originalOld
							thisField!.SetValue(this, originalOld);

						} else if (originalOld is DiscordObjectContainer container) {
							DiscordObjectContainer modifiedContainer = (DiscordObjectContainer)potentiallyNew!;
							modifiedContainer.Reset();
							modifiedContainer.InternalList = container.InternalList;


						} else if (originalOld is DiscordObject dobj) {
							// originalOld is a duplicate created via a special memberwise thing. This is where shit gets difficult.
							thisField!.SetValue(this, objField!.GetValue(dobj));

						}
					}
				} catch { }
			}

			Changes.Clear();
			IgnoresNetworkUpdates = true;
			UnlockedEvent.Set();
		}

		/// <summary>
		/// Updates this object from a <see cref="PayloadDataObject"/> of the same event source type. If <see cref="IgnoresNetworkUpdates"/> is <see langword="false"/>, this change is deferred and ignored.
		/// </summary>
		/// <remarks>
		/// This should be the method that is called by events.
		/// </remarks>
		/// <param name="obj">The object containing the new data.</param>
		/// <param name="skipNonNullFields">If <see langword="true"/>, then this payload is only part of the object and this method will be called again. As such, fields that aren't null should be left alone (to prevent partial objects from setting things back to null). The initial creation of an object should have this set to <see langword="false"/> to prepare the object properly.</param>
		internal async Task UpdateFromObject(PayloadDataObject obj, bool skipNonNullFields) {
			// This variant is called by the base systems and does a check before calling the *real* version of this method which is below.
			if (!IgnoresNetworkUpdates) return; // Defer changes.
			IgnoresNetworkUpdates = false;
			await Update(obj, skipNonNullFields);
			IgnoresNetworkUpdates = true;
		}

		#region Appropriate Value Functions

		#region Generics

		/// <summary>
		/// A utility method that returns the value a property should be set to based on <paramref name="skipNonNullFields"/>.<para/>
		/// If <paramref name="existingValue"/> is not <see langword="null"/>, and if <paramref name="skipNonNullFields"/> is <see langword="true"/>, then <paramref name="existingValue"/> will be returned. Otherwise, <paramref name="newValue"/> will be returned.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="existingValue">The current value that is available.</param>
		/// <param name="newValue">The new value that it should be set to.</param>
		/// <param name="skipNonNullFields">If <see langword="true"/>, and if <paramref name="existingValue"/> is not <see langword="null"/>, then <paramref name="existingValue"/> will be returned instead of <paramref name="newValue"/>.</param>
		protected T AppropriateValue<T>(T existingValue, T newValue, bool skipNonNullFields) {
			if (!(existingValue is null) && skipNonNullFields) return existingValue;
			// ^ Existing value is not null, and non-null values need to be skipped.

			return newValue;
		}

		/// <summary>
		/// A utility method that returns the value a property should be set to based on <paramref name="skipNonNullFields"/>.<para/>
		/// In this variant, the input new value may be null, but the existing value cannot (and will not) be null.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="existingValue">The current value that is available.</param>
		/// <param name="newValue">The new value that it should be set to.</param>
		/// <param name="skipNonNullFields">If <see langword="true"/>, and if <paramref name="existingValue"/> is not <see langword="null"/>, then <paramref name="existingValue"/> will be returned instead of <paramref name="newValue"/>.</param>
		protected T AppropriateNullableValue<T>(T existingValue, T? newValue, bool skipNonNullFields) where T : class {
			if (!(existingValue is null) && skipNonNullFields) return existingValue;
			// ^ Target value is not null and non-null values need to be skipped.

			// new case
			if (newValue is null) return existingValue!;
			return newValue;
		}

		/// <summary>
		/// A utility method that returns the value a property should be set to based on <paramref name="skipNonNullFields"/>.<para/>
		/// In this variant, the input new value may be null, but the existing value cannot (and will not) be null.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="existingValue">The current value that is available.</param>
		/// <param name="newValue">The new value that it should be set to.</param>
		/// <param name="skipNonNullFields">If <see langword="true"/>, and if <paramref name="existingValue"/> is not <see langword="null"/>, then <paramref name="existingValue"/> will be returned instead of <paramref name="newValue"/>.</param>
		protected T AppropriateNullableValue<T>(T existingValue, T? newValue, bool skipNonNullFields) where T : struct {
			if (skipNonNullFields || !newValue.HasValue) return existingValue;
			// ^ Target value is not null (guaranteed) and non-null values need to be skipped, or the new value doesn't exist so we *have* to prefer the old one.
			return newValue.Value;
		}

		#endregion

		#region Special Object Handling

		/// <summary>
		/// For cases where the input string may be null via being explicitly sent that way, but where it may also be null simply because it was unsent. Payloads are expected to have a default value of <see cref="Constants.UNSENT_STRING_DEFAULT"/>, and if a payload has this value (as <paramref name="payloadValue"/>), then the existing value is returned.
		/// </summary>
		/// <param name="existingValue">The current value that is available.</param>
		/// <param name="payloadValue">The value stored in the payload.</param>
		/// <param name="skipNonNullFields">If <see langword="true"/>, and if <paramref name="existingValue"/> is not <see langword="null"/>, then <paramref name="existingValue"/> will be returned instead of <paramref name="payloadValue"/>.</param>
		/// <returns></returns>
		protected string? AppropriateNullableString(string? existingValue, string? payloadValue, bool skipNonNullFields) {
			if (!(existingValue is null) && skipNonNullFields) return existingValue;
			if (payloadValue == Constants.UNSENT_STRING_DEFAULT || payloadValue == string.Empty) return existingValue;
			return payloadValue;
		}

		/// <summary>
		/// Returns whichever time is greater between the two.
		/// </summary>
		/// <param name="existingTime"></param>
		/// <param name="newTime"></param>
		/// <returns></returns>
		protected DateTimeOffset AppropriateTime(DateTimeOffset existingTime, DateTimeOffset newTime) {
			return existingTime > newTime ? existingTime : newTime;
		}

		/// <summary>
		/// Returns whichever time is greater between the two. If any of the two values is <see langword="null"/>, then <see langword="null"/> will be returned. Otherwise, this returns whichever time is greater.
		/// </summary>
		/// <param name="existingTime"></param>
		/// <param name="newTime"></param>
		/// <returns></returns>
		protected DateTimeOffset? AppropriateNullableTime(DateTimeOffset? existingTime, DateTimeOffset? newTime) {
			if (!existingTime.HasValue || !newTime.HasValue) {
				return null;
			}
			return existingTime > newTime ? existingTime : newTime;
		}

		#endregion

		#endregion

		#endregion

		#region Abstract Implementation

		/// <summary>
		/// Updates this object from a <see cref="PayloadDataObject"/> of the same event source type.
		/// </summary>
		/// <remarks>
		/// This variant should not be called manually unless it is an internal update directly from a payload that should ignore the locked status.
		/// </remarks>
		/// <param name="obj">The object containing the new data.</param>
		/// <param name="skipNonNullFields">If <see langword="true"/>, then this payload is only part of the object and this method will be called again. As such, fields that aren't null should be left alone (to prevent partial objects from setting things back to null). The initial creation of an object should have this set to <see langword="false"/> to prepare the object properly.</param>
		protected internal abstract Task Update(PayloadDataObject obj, bool skipNonNullFields = false);

		/// <summary>
		/// Sends the changes made to this object to Discord. The passed in Dictionary <paramref name="changesAndOriginalValues"/> stores a mapping from its associated Property name to its json property name and old value.
		/// </summary>
		/// <param name="changesAndOriginalValues">A mapping from <c>[property name] = (json name, original value)</c>. To get the new value, access the property via the property name provided in the key.</param>
		/// <param name="changeReasons">A condensed string representing why a number of properties were changed, or null if no reasons were given.</param>
		protected abstract Task<HttpResponseMessage?> SendChangesToDiscord(IReadOnlyDictionary<string, object> changesAndOriginalValues, string? changeReasons);

		/// <summary>
		/// Returns the value of the given property or field name in this object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="propertyName"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="MissingMemberException"></exception>
		protected internal T ValueOf<T>(string propertyName) {
			PropertyInfo? prop = GetType().GetProperty(propertyName);
			if (prop != null) {
				return (T)prop.GetValue(this)!;
			}
			FieldInfo? field = GetType().GetField(propertyName);
			if (field != null) {
				return (T)field.GetValue(this)!;
			}
			throw new MissingMemberException("Cannot locate a property or field named " + propertyName);
		}

		#endregion

		#region Object Overrides

		/// <summary>
		/// Whether or not the given object is equal to this <see cref="DiscordObject"/>, which is tested in one of four ways. First, it checks for reference equality. The next check is if it is a different <see cref="DiscordObject"/>, from which it compares the IDs. The final two checks can take in a <see cref="Snowflake"/> or <see cref="ulong"/> from which the value will be compared to this <see cref="DiscordObject"/>'s ID.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool Equals(object? other) {
			if (other is DiscordObject o) return Equals(o);
			if (other is Snowflake id) return Equals(id);
			if (other is ulong uid) return Equals(uid);
			return false;
		}

		/// <param name="other"></param>
		/// <returns><see langword="true"/> if the ID of <paramref name="other"/> is equal to this ID, and <see langword="false"/> if it is not.</returns>
		public bool Equals(DiscordObject? other) {
			if (other is null) return false;
			return other.ID == ID && other.IsClone == IsClone;
		}

		/// <param name="other"></param>
		/// <returns><see langword="true"/> if <paramref name="other"/> is equal to this ID, and <see langword="false"/> if it is not.</returns>
		public bool Equals(Snowflake other) {
			return ID == other;
		}

		/// <param name="other"></param>
		/// <returns><see langword="true"/> if <paramref name="other"/> is equal to this ID's <see cref="ulong"/> value, and <see langword="false"/> if it is not.</returns>
		public bool Equals(ulong other) {
			return ID.Value == other;
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return ID.GetHashCode();
		}

		/// <summary>
		/// Returns this <see cref="DiscordObject"/>'s type name, followed by its ID as a rich string (see <see cref="Snowflake.ToRichString"/>)
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return $"DiscordObject [{GetType().Name}] :: {ID}";
		}

		#endregion

		#region Equality & Comparison

		/// <summary>
		/// Compares the IDs of these two <see cref="DiscordObject"/>s.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(DiscordObject? left, DiscordObject? right) {
			if (left is null && right is null) return true; // Both are null. Null is null.
			if (left is null || right is null) return false; // One is null (both is covered ^)
			return left.Equals(right);
		}

		/// <summary>
		/// Compares the IDs of these two <see cref="DiscordObject"/>s.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(DiscordObject? left, DiscordObject? right) => !(left == right);

		/// <inheritdoc/>
		public int CompareTo([AllowNull] DiscordObject other) {
			if (other == null) return -1;
			return ID.CompareTo(other.ID);
		}

		#endregion

		#region Backups

		/// <summary>
		/// A memberwise clone of this object. This may perform a slightly-deeper-than-shallow copy of the object (in that a select handful of object reference fields are actually duplicated), but a great deal of fields will not be duplicated.
		/// </summary>
		/// <remarks>
		/// Object-reference fields that are copied will be marked as such by saying <strong>This reference is cloned in clone objects.</strong> Unmarked members are to be assumed to be <em>NOT cloned, and thus not reflective of the object's older status.</em><para/>
		/// As should be implied, value types (numbers, boolean, string, so on) are always copied no matter what, so they will not say that special notice.<para/>
		/// And of course, cloned does not necessarily imply identical, as the original object may be changed by a payload. For example, if the old object references a channel named A, and the new object no longer references it, the old object will still reference A. The thing that changes is whether A is a copy of the old A, or if it's the current A as it is right now. If A is marked as cloned, then it will be the old A.
		/// </remarks>
		/// <returns></returns>
		/// 
		public new virtual DiscordObject MemberwiseClone() {
			DiscordObject newObj = (DiscordObject)base.MemberwiseClone();
			newObj.IsClone = true;
			return newObj;
		}

		/// <summary>
		/// Identical to <see cref="MemberwiseClone"/> but provides the ability to cast into the target type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T MemberwiseClone<T>() where T : DiscordObject {
			return (T)MemberwiseClone();
		}

		/*

		/// <inheritdoc/>
		public abstract DiscordObject CreateBackup();

		/// <inheritdoc/>
		public abstract void Restore();

		*/
		#endregion

	}
}
