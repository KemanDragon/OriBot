#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.Utility.Marshalling;
using OldOriBot.Exceptions;
using OldOriBot.Interaction;

namespace OldOriBot.Data.Commands.ArgData {

	/// <summary>
	/// A command argument type representing someone in Discord.
	/// </summary>
	[Serializable]
	public class Person : ICommandArg<Person> {

		/// <summary>
		/// The member associated with this <see cref="Person"/>, or <see langword="null"/> if the member was not found.
		/// </summary>
		public Member? Member { get; private set; }

		public Person() { }

		/// <summary>
		/// Tries to find the member from the guild. Returns <see langword="null"/> if nobody was found.
		/// </summary>
		/// <param name="guild"></param>
		/// <param name="nameQuery"></param>
		/// <returns></returns>
		//internal async Task<Member> GetMember(Guild guild, string nameQuery) {
		protected internal Member? GetMember(Guild guild, string nameQuery) {
			Member[] mbrs = guild.FindMembers(nameQuery);
			if (mbrs.Length == 0) {
				return null;
			} else if (mbrs.Length == 1) {
				return mbrs[0];
			} else {
				throw new NonSingularPersonException(mbrs);
			}
		}

		private async Task<Person> FromAsync(string personQuery, BotContext? inContext) {
			if (string.IsNullOrWhiteSpace(personQuery)) throw new ArgumentNullException(Personality.Get("err.noUserParam"), new NoThrowDummyException());

			Guild? guild = inContext?.Server;
			if (guild == null) {
				throw new InvalidOperationException($"Cannot use {nameof(Person)} without a BotContext.", new NoThrowDummyException());
			}
			if (Snowflake.TryExtract(personQuery, out Snowflake id, out SnowflakeType type)) {
				if (type != SnowflakeType.User && type != SnowflakeType.Ambiguous) throw new ArgumentException("The given ID does not correspond to a user!", new NoThrowDummyException());
				User? user = await User.GetOrDownloadUserAsync(id).ConfigureAwait(false);
				if (user == null) throw new ArgumentException("The given user could not be found!", new NoThrowDummyException());
				return new Person {
					Member = await user.InServerAsync(guild)
				};
			}
			Member? mbr = GetMember(guild, personQuery);
			if (mbr != null) {
				return new Person {
					Member = mbr
				};
			}
			return new Person {
				Member = null
			};
		}

		/// <inheritdoc/>
		public Person? From(string instance, object? inContext) {
			try {
				return FromAsync(instance, inContext as BotContext).GetAwaiter().GetResult();
			} catch {
				throw;
			}
		}

		/// <inheritdoc/>
		object? ICommandArg.From(string instance, object? inContext) => ((ICommandArg<Person>)this).From(instance, inContext);
	}
}
