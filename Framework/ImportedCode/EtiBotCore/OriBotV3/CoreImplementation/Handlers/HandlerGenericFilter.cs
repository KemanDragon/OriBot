using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EtiBotCore.Data.Structs;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using OldOriBot.Data.MemberInformation;
using OldOriBot.Interaction;

namespace OldOriBot.CoreImplementation.Handlers {
	public class HandlerGenericFilter : PassiveHandler {
		public override string Name { get; } = "Generic Filter";
		public override string Description { get; } = "Implements a non-malicious filter intended to assist in management of content distribution.";

		public HandlerGenericFilter(BotContext ctx) : base(ctx) { }

		/// <summary>
		/// Regex used to find an emoji with a specific ID in a message, uses string.format
		/// </summary>
		public const string EMOJI_REGEX = @"<(a?):(.+):({0})>";

		/// <summary>
		/// The IDs of emojis that cannot be sent in this server.
		/// </summary>
		public static readonly Snowflake[] UnwantedEmojis = {
			737914348436979863
		};

		/// <summary>
		/// Queries that must be stated in general to be removed.
		/// </summary>
		[Obsolete]
		public static readonly string[] UnwantedQueries = {
			
		};

		/// <summary>
		/// Queries that must be stated verbatim in order to be removed.
		/// </summary>
		[Obsolete]
		public static readonly string[] UnwantedExplicitQueries = {
			
		};

		public override async Task<bool> ExecuteHandlerAsync(Member executor, BotContext executionContext, Message message) {
			if (message.AuthorMember.GetPermissionLevel() >= PermissionData.PermissionLevel.Operator) return false;
			foreach (Snowflake id in UnwantedEmojis) {
				if (Regex.IsMatch(message.Content, string.Format(EMOJI_REGEX, id.Value))) {
					await message.DeleteAsync("Message contained an emoji that uses content not allowed for the server.");
					return true;
				}
			}
			return false;
		}
	}
}
