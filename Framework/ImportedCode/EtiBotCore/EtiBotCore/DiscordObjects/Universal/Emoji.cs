using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Events.Intents.GuildOrDirectMessageReactions;
using EtiBotCore.Utility.Threading;

namespace EtiBotCore.DiscordObjects.Universal {

	/// <summary>
	/// Represents a default emoji, which is one representable with unicode.<para/>
	/// See <see cref="CustomEmoji"/> for user-designed Emojis. The ID of a stock emoji will always be <see cref="Snowflake.Invalid"/>
	/// </summary>
	
	public class Emoji : DiscordObject, IEquatable<Emoji> {

		internal static readonly ThreadedDictionary<string, Emoji> InstantiatedEmojis = new ThreadedDictionary<string, Emoji>();

		/// <summary>
		/// The name of this emoji if it's custom (will never have colons), or the actual unicode character if this is a default Emoji.
		/// </summary>
		public string? Name { get; protected set; }

		/// <summary>
		/// Whether or not this Emoji is a user-defined / custom Emoji. If <see langword="false"/>, this is a standard Unicode Emoji. If <see langword="true"/>, this can (and should) be cast into <see cref="CustomEmoji"/>.
		/// </summary>
		public virtual bool IsCustom { get; } = false;

		/// <summary>
		/// Construct a new emoji for the given unicode character.
		/// </summary>
		/// <param name="unicodeCharacter"></param>
		protected Emoji(string? unicodeCharacter) : base(Snowflake.Invalid) {
			Name = unicodeCharacter;
		}

		/// <summary>
		/// Intended for being called by <see cref="CustomEmoji"/>.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="id"></param>
		protected Emoji(string? name, Snowflake id) : base(id) {
			Name = name;
		}

		/// <summary>
		/// Get or create an emoji from the given unicode character. Remember to check if this is custom BEFORE using this, and if it is, use <see cref="CustomEmoji.GetOrCreate(Payloads.PayloadObjects.Emoji)"/>
		/// </summary>
		/// <param name="unicodeChar"></param>
		/// <returns></returns>
		public static Emoji GetOrCreate(string unicodeChar) {
			if (!InstantiatedEmojis.ContainsKey(unicodeChar)) {
				InstantiatedEmojis[unicodeChar] = new Emoji(unicodeChar);
			}
			return InstantiatedEmojis[unicodeChar];
		}

		/// <summary>
		/// Converts this <see cref="Emoji"/> to a url-safe encoding.
		/// </summary>
		/// <returns></returns>
		internal string ToURLEncoding() {
			if (ID.IsValid) {
				return HttpUtility.UrlEncode($"{Name}:{ID}", Encoding.UTF8);
			} else {
				return HttpUtility.UrlEncode(Name, Encoding.UTF8);
			}
		}

		/// <summary>
		/// Does nothing.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="skipNonNullFields"></param>
		protected internal override Task Update(PayloadDataObject obj, bool skipNonNullFields) => Task.CompletedTask;

		/// <summary>
		/// This will always throw an <see cref="InvalidOperationException"/> -- Emojis cannot be altered by bots.
		/// </summary>
		/// <param name="changes"></param>
		/// <param name="reasons"></param>
		protected override Task<HttpResponseMessage?> SendChangesToDiscord(IReadOnlyDictionary<string, object> changes, string? reasons) {
			throw new InvalidOperationException("Emojis cannot be changed.");
		}

		/// <inheritdoc/>
		public override bool Equals(object? other) {
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;
			if (other is Emoji emoji) return Name?.Equals(emoji.Name) ?? false;
			return false;
		}

		/// <inheritdoc/>
		public bool Equals([AllowNull] Emoji other) {
			if (other is null) return false;
			return Name?.Equals(other.Name) ?? false;
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return HashCode.Combine(Name, ID);
		}

		/// <summary>
		/// Converts this <see cref="Emoji"/> to a string that is formatted for use in chats.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return Name!;
		}

		/// <inheritdoc/>
		public static bool operator ==(Emoji left, Emoji right) {
			if (left is null && right is null) return true;
			if (left is null || right is null) return false;
			if (left.Equals(right)) return true;
			return false;
		}

		/// <inheritdoc/>
		public static bool operator !=(Emoji left, Emoji right) => !(left == right);
	}

	/// <summary>
	/// Represents a user-uploaded emoji.
	/// </summary>
	
	public class CustomEmoji : Emoji, IEquatable<CustomEmoji> {

		internal static readonly ThreadedDictionary<Snowflake, CustomEmoji> InstantiatedCustomEmojis = new ThreadedDictionary<Snowflake, CustomEmoji>();

		/// <summary>
		/// The user that created this.
		/// </summary>
		/// <remarks>
		/// <strong>This could be <see langword="null"/></strong> solely depending on whether or not this <see cref="CustomEmoji"/> was created in a reaction or not. This behavior will be changed in the future.
		/// </remarks>
		public User? Creator { get; }

		/// <summary>
		/// Whether or not this Emoji is animated.
		/// </summary>
		public bool Animated { get; }

		/// <summary>
		/// Whether or not this Emoji is managed, e.g. from a Twitch streamer.
		/// </summary>
		public bool Managed { get; }

		/// <inheritdoc/>
		public override bool IsCustom { get; } = true;

		internal static CustomEmoji GetOrCreate(Payloads.PayloadObjects.Emoji plEmoji) {
			if (InstantiatedCustomEmojis.ContainsKey(plEmoji.ID!.Value)) {
				return InstantiatedCustomEmojis[plEmoji.ID!.Value];
			}

			User? creator;
			if (plEmoji.Creator != null) {
				creator = User.EventGetOrCreate(plEmoji.Creator);
			} else {
				creator = null;
			}
			CustomEmoji emoji = new CustomEmoji(creator, plEmoji.ID!.Value, plEmoji.Name, plEmoji.Animated, plEmoji.Managed);
			InstantiatedCustomEmojis[plEmoji.ID!.Value] = emoji;
			return emoji;
		}

		/// <summary>
		/// Constructs a new CustomEmoji with the given creator user and name.
		/// </summary>
		/// <param name="creator"></param>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="animated"></param>
		/// <param name="managed"></param>
		internal CustomEmoji(User? creator, Snowflake id, string? name, bool? animated, bool? managed) : base(name, id) {
			Creator = creator;
			if (animated != null) Animated = animated.Value;
			if (managed != null) Managed = managed.Value;
		}

		/// <inheritdoc/>
		public override bool Equals(object? other) {
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;
			if (other is Emoji emoji) return Equals(emoji);
			if (other is CustomEmoji custom) return Equals(custom);
			return false;
		}

		/// <inheritdoc/>
		public  bool Equals([AllowNull] CustomEmoji other) {
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;
			return ID.Equals(other.ID);
		}

		/// <inheritdoc/>
		public override int GetHashCode() {
			return HashCode.Combine(Name, ID);
		}

		/// <summary>
		/// Converts this <see cref="CustomEmoji"/> to a string that is formatted for use in chats.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return $"<{(Animated ? "a" : "")}:{Name}:{ID}>";
		}

		/// <inheritdoc/>
		public static bool operator ==(CustomEmoji left, CustomEmoji right) {
			if (left is null && right is null) return true;
			if (left is null || right is null) return false;
			if (left.Equals(right)) return true;
			return false;
		}

		/// <inheritdoc/>
		public static bool operator !=(CustomEmoji left, CustomEmoji right) => !(left == right);
	}
}
