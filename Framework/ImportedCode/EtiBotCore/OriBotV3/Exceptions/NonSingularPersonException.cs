using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Universal;
using OldOriBot.Data;

namespace OldOriBot.Exceptions {

	/// <summary>
	/// An exception thrown when attempting to create a Person object, but the query does not return a single member.
	/// </summary>
	public class NonSingularPersonException : Exception {

		/// <summary>
		/// The members that were found from the vague query.
		/// </summary>
		public IReadOnlyList<Member> Candidates { get; }

		public NonSingularPersonException(IEnumerable<Member> members) : base(Personality.Get("err.multiUser")) {
			Candidates = members.ToList();
		}

	}
}
