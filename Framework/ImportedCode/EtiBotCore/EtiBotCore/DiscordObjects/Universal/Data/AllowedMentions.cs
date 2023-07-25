using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.Utility.Marshalling;
using Newtonsoft.Json;

namespace EtiBotCore.DiscordObjects.Universal.Data {

	/// <summary>
	/// Represents what mentions are allowed in a message. JSON compatible.
	/// </summary>
	
	public class AllowedMentions {

		/// <summary>
		/// Returns a new <see cref="AllowedMentions"/> instance that does not allow anything to be pinged.
		/// </summary>
		[JsonIgnore]
		public static AllowedMentions AllowNothing => new AllowedMentions() {
			PingRepliedUser = false,
			AllowEveryoneAndHere = false,
			AllowPingingAnyRoles = false,
			AllowPingingAnyUsers = false
		};

		/// <summary>
		/// Returns a new <see cref="AllowedMentions"/> instance that only allows roles to be pinged (not including @everyone and @here)
		/// </summary>
		[JsonIgnore]
		public static AllowedMentions AllowOnlyRoles => new AllowedMentions() {
			PingRepliedUser = false,
			AllowEveryoneAndHere = false,
			AllowPingingAnyRoles = true,
			AllowPingingAnyUsers = false
		};

		/// <summary>
		/// Returns a new <see cref="AllowedMentions"/> instance that only allows users to be pinged.
		/// </summary>
		[JsonIgnore]
		public static AllowedMentions AllowOnlyUsers => new AllowedMentions() {
			PingRepliedUser = false,
			AllowEveryoneAndHere = false,
			AllowPingingAnyRoles = false,
			AllowPingingAnyUsers = true
		};

		/// <summary>
		/// Returns a new <see cref="AllowedMentions"/> that only pings the person this message is replying to and nobody else, of course, granted that this is an actual reply message.
		/// </summary>
		[JsonIgnore]
		public static AllowedMentions Reply => new AllowedMentions() {
			PingRepliedUser = true,
			AllowEveryoneAndHere = false,
			AllowPingingAnyRoles = false,
			AllowPingingAnyUsers = false
		};

		/// <summary>
		/// Returns a new <see cref="AllowedMentions"/> that does not ping the person this message is replying to, of course, granted that this is an actual reply message.<para/>
		/// This is identical to <see cref="AllowNothing"/> and exists for idiomatic programming.
		/// </summary>
		[JsonIgnore] 
		public static AllowedMentions NoReply => AllowNothing;

		/// <summary>
		/// Returns a new <see cref="AllowedMentions"/> that allows pinging any role or user, excluding @everyone and @here.
		/// </summary>
		[JsonIgnore]
		public static AllowedMentions AllowOnlyRolesAndUsers => new AllowedMentions() {
			PingRepliedUser = false,
			AllowEveryoneAndHere = false,
			AllowPingingAnyRoles = true,
			AllowPingingAnyUsers = true
		};

		/// <summary>
		/// Returns a new <see cref="AllowedMentions"/> that allows pinging @everyone and @here, but not any individual roles or users. Probably useless but hey.
		/// </summary>
		[JsonIgnore]
		public static AllowedMentions AllowOnlyEveryoneAndHere => new AllowedMentions() {
			PingRepliedUser = false,
			AllowEveryoneAndHere = true,
			AllowPingingAnyRoles = false,
			AllowPingingAnyUsers = false
		};

		/// <summary>
		/// Returns a new <see cref="AllowedMentions"/> that allows anyone and anything to be pinged, but will not ping whoever it's replying to.
		/// </summary>
		[JsonIgnore]
		public static AllowedMentions AllowAllButReplies => new AllowedMentions() {
			PingRepliedUser = false,
			AllowPingingAnyRoles = true,
			AllowPingingAnyUsers = true,
			AllowEveryoneAndHere = true
		};

		/// <summary>
		/// Returns a new <see cref="AllowedMentions"/> that allows anyone and anything to be pinged, and will ping whoever it's replying to.
		/// </summary>
		[JsonIgnore]
		public static AllowedMentions AllowAnything => new AllowedMentions() {
			AllowPingingAnyRoles = true,
			AllowPingingAnyUsers = true,
			AllowEveryoneAndHere = true,
			PingRepliedUser = true
		};

		/// <summary>
		/// Whether or not this should be allowed to ping any and all roles (or mentionable roles, if the bot does not have permission).<para/>
		/// <strong>Default value:</strong> <see langword="false"/>
		/// </summary>
		[JsonIgnore]
		public bool AllowPingingAnyRoles {
			get => Parse.Contains("roles");
			set {
				if (value && !Parse.Contains("roles")) {
					Parse.Add("roles");
				} else if (!value && Parse.Contains("roles")) {
					Parse.Remove("roles");
				}
			}
		}

		/// <summary>
		/// Whether or not this should be allowed to ping any and all users.<para/>
		/// <strong>Default value:</strong> <see langword="false"/>
		/// </summary>
		[JsonIgnore] 
		public bool AllowPingingAnyUsers {
			get => Parse.Contains("users");
			set {
				if (value && !Parse.Contains("users")) {
					Parse.Add("users");
				} else if (!value && Parse.Contains("users")) {
					Parse.Remove("users");
				}
			}
		}

		/// <summary>
		/// Whether or not this should be allowed to ping @everyone and @here.<para/>
		/// <strong>Default value:</strong> <see langword="false"/>
		/// </summary>
		[JsonIgnore] 
		public bool AllowEveryoneAndHere {
			get => Parse.Contains("everyone");
			set {
				if (value && !Parse.Contains("everyone")) {
					Parse.Add("everyone");
				} else if (!value && Parse.Contains("everyone")) {
					Parse.Remove("everyone");
				}
			}
		}

		/// <summary>
		/// Whether or not to ping the user this message replied to.<para/>
		/// <strong>Default value:</strong> <see langword="false"/>
		/// </summary>
		[JsonProperty("replied_user")]
		public bool PingRepliedUser { get; set; } = false;

		/// <summary>
		/// All roles to ping. Max 100 IDs.<para/>
		/// If <see cref="AllowPingingAnyRoles"/> is <see langword="true"/>, this list will do nothing, as any and all role pings would be resolved.
		/// </summary>
		[JsonProperty("roles")]
		public readonly LimitedSpaceList<Snowflake> Roles = new LimitedSpaceList<Snowflake>(100);

		/// <summary>
		/// All users to ping. Max 100 IDs.<para/>
		/// If <see cref="AllowPingingAnyUsers"/> is <see langword="true"/>, this list will do nothing, as any and all user pings would be resolved.
		/// </summary>
		[JsonProperty("users")]
		public readonly LimitedSpaceList<Snowflake> Users = new LimitedSpaceList<Snowflake>(100);


		[JsonProperty("parse")]
		private readonly List<string> Parse = new List<string>();

#pragma warning disable IDE0051 // Remove unused private members
		private bool ShouldSerializeRoles() => !AllowPingingAnyRoles;
		private bool ShouldSerializeUsers() => !AllowPingingAnyUsers;
#pragma warning restore IDE0051 // Remove unused private members

		/// <summary>
		/// Create a blank <see cref="AllowedMentions"/>
		/// </summary>
		public AllowedMentions() { }

		internal AllowedMentions(AllowedMentions other) {
			PingRepliedUser = other.PingRepliedUser;
			Users = new LimitedSpaceList<Snowflake>(other.Users);
			Roles = new LimitedSpaceList<Snowflake>(other.Roles);
			Parse = other.Parse.ToArray().ToList(); // shitty copy
		}

	}
}
