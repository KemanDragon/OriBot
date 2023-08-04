using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal.Data;
using Newtonsoft.Json;
using OldOriBot.Data;
using OldOriBot.Interaction;
using OldOriBot.Utility.Formatting;

namespace OldOriBot.CoreImplementation.Handlers {
	public class HandlerSteamSystem : PassiveHandler {

		private const string STEAM_COMMUNITY_URL_BASE = "steamcommunity.com/sharedfiles/filedetails/?id=";
		private const string STEAM_STORE_URL_BASE = "store.steampowered.com/app/";
		private const string STEAM_PROFILE_URL_BASE_INT64 = "steamcommunity.com/profiles/";
		private const string STEAM_PROFILE_URL_BASE_CUSTOM = "steamcommunity.com/id/";

		private const string STEAM_STORE_PROTOCOL_BASE = "steam://store/";
		private const string STEAM_COMMUNITY_PROTOCOL_BASE = "steam://url/CommunityFilePage/";
		private const string STEAM_PROFILE_PROTOCOL_BASE = "steam://url/SteamIDPage/";

		#region Steam API Key
		private static string STEAM_API_KEY {
			get {
				if (_SteamApi == null) {
					if (Directory.Exists("V:\\")) {
						_SteamApi = File.ReadAllText(@"V:\EtiBotCore\steam.txt");
					} else {
						_SteamApi = File.ReadAllText(@"C:\EtiBotCore\steam.txt");
					}
				}
				return _SteamApi;
			}
		}
		private static string _SteamApi = null;
		#endregion

		public HandlerSteamSystem(BotContext ctx) : base(ctx) { }

		public override string Name { get; } = "Steam URL Redirection Utility";
		public override string Description { get; } = "Detects URLs pointing to various parts of Steam's website, and where applicable, posts a variant of that link using the " + new MDLink("Steam Browser Protocol", "https://developer.valvesoftware.com/wiki/Steam_browser_protocol").ToString() + ".";

		public override async Task<bool> ExecuteHandlerAsync(Member executor, BotContext executionContext, Message message) {
			if (message.Channel == executionContext.GetPassiveHandlerInstance<HandlerArtPinSystem>().ArtChannel) return false;

			string messageContent = message.Content.ToLower();
			if (messageContent.Contains(STEAM_COMMUNITY_URL_BASE)) {
				int idx = messageContent.IndexOf(STEAM_COMMUNITY_URL_BASE);
				string cutMsg = messageContent.Substring(idx + STEAM_COMMUNITY_URL_BASE.Length);
				string id = GetUntilLastDigit(cutMsg);

				//await originalMessage.RespondAsync(STEAM + " Steam Workshop URL detected!\nYou can click this link to open it directly in the Steam Client: " + STEAM_COMMUNITY_PROTOCOL_BASE + id);
				//await originalMessage.RespondAsync(Utility.Emojis.STEAM + " " + string.Format(LocalizationDictionary.Get("ori.passivehandlers.steam.workshop"), STEAM_COMMUNITY_PROTOCOL_BASE + id));
				await message.ReplyAsync(Personality.Get("ori.passivehandlers.steam.workshop", STEAM_COMMUNITY_PROTOCOL_BASE + id), mentionLimits: AllowedMentions.Reply);

				return true;
			}

			if (messageContent.Contains(STEAM_STORE_URL_BASE)) {
				int idx = messageContent.IndexOf(STEAM_STORE_URL_BASE);
				string cutMsg = messageContent.Substring(idx + STEAM_STORE_URL_BASE.Length);
				string id = GetUntilLastDigit(cutMsg);

				//await originalMessage.RespondAsync(STEAM + " Steam Store URL detected!\nYou can click this link to open it directly in the Steam Client: " + STEAM_STORE_PROTOCOL_BASE + id);
				//await originalMessage.RespondAsync(Utility.Emojis.STEAM + " " + string.Format(LocalizationDictionary.Get("ori.passivehandlers.steam.store"), STEAM_STORE_PROTOCOL_BASE + id));
				await message.ReplyAsync(Personality.Get("ori.passivehandlers.steam.store", STEAM_STORE_PROTOCOL_BASE + id), mentionLimits: AllowedMentions.Reply);

				return true;
			}

			if (messageContent.Contains(STEAM_PROFILE_URL_BASE_INT64)) {
				int idx = messageContent.IndexOf(STEAM_PROFILE_URL_BASE_INT64);
				string cutMsg = messageContent.Substring(idx + STEAM_PROFILE_URL_BASE_INT64.Length);
				string id = GetUntilLastDigit(cutMsg);

				//await originalMessage.RespondAsync(STEAM + " Steam Profile URL detected!\nYou can click this link to open it directly in the Steam Client: " + STEAM_PROFILE_PROTOCOL_BASE + id);
				//await originalMessage.RespondAsync(Utility.Emojis.STEAM + " " + string.Format(LocalizationDictionary.Get("ori.passivehandlers.steam.profile"), STEAM_PROFILE_PROTOCOL_BASE + id));
				await message.ReplyAsync(Personality.Get("ori.passivehandlers.steam.profile", STEAM_PROFILE_PROTOCOL_BASE + id), mentionLimits: AllowedMentions.Reply);

				return true;
			}

			if (messageContent.Contains(STEAM_PROFILE_URL_BASE_CUSTOM)) {
				int idx = messageContent.IndexOf(STEAM_PROFILE_URL_BASE_CUSTOM);
				string cutMsg = messageContent.Substring(idx + STEAM_PROFILE_URL_BASE_CUSTOM.Length);
				string vanity = CleanUpVanity(GetUntilSpaceOrInvalid(cutMsg));

				// Silly catch case. Vanity URL ends in a / so we need to pull that. Steam does not allow this character in vanity URLs so this is a safe method.
				//if (vanity.EndsWith("/"))
				//	vanity = vanity.Substring(0, vanity.Length - 1);


				using WebClient client = new WebClient();
				string webData = client.DownloadString(string.Format("http://api.steampowered.com/ISteamUser/ResolveVanityURL/v0001/?key={0}&vanityurl={1}", STEAM_API_KEY, vanity));
				Response steamResponse = JsonConvert.DeserializeObject<SteamReturnData>(webData).Response;

				if (steamResponse == null) {
					//await originalMessage.RespondAsync(STEAM + " Steam Profile URL Detected!\nUnfortunately, an error has occurred. The response from Steam's servers was null. Xan has been notified.");
					//await originalMessage.RespondAsync(Utility.Emojis.STEAM + " " + LocalizationDictionary.Get("ori.passivehandlers.steam.err.profileNoResponse"));
					await message.ReplyAsync(Personality.Get("ori.passivehandlers.steam.err.profileNoResponse"), mentionLimits: AllowedMentions.Reply);
					HandlerLogger.WriteWarning("Steam response null!");
					return true;
				}

				if (steamResponse.Success == 1) {
					//await originalMessage.RespondAsync(STEAM + " Steam Profile URL detected!\nYou can click this link to open it directly in the Steam Client: " + STEAM_PROFILE_PROTOCOL_BASE + steamResponse.steamid);
					//await originalMessage.RespondAsync(Utility.Emojis.STEAM + " " + string.Format(LocalizationDictionary.Get("ori.passivehandlers.steam.profile"), STEAM_PROFILE_PROTOCOL_BASE + steamResponse.steamid));
					await message.ReplyAsync(Personality.Get("ori.passivehandlers.steam.profile", STEAM_PROFILE_PROTOCOL_BASE + steamResponse.SteamID), mentionLimits: AllowedMentions.Reply);
					return true;
				}

				//await originalMessage.RespondAsync(STEAM + " Steam Profile URL detected!\nUnfortunately, an error has occurred. Steam reported that the specified vanity name (" + vanity + ") did not have an associated Steam ID. Are you sure you're using the correct URL?");
				//await originalMessage.RespondAsync(Utility.Emojis.STEAM + " " + string.Format(LocalizationDictionary.Get("ori.passivehandlers.steam.err.noVanityName"), vanity));
				await message.ReplyAsync(Personality.Get("ori.passivehandlers.steam.err.noVanityName", vanity), mentionLimits: AllowedMentions.Reply);
				return true;
			}

			return false;
		}


		private static string CleanUpVanity(string vanityName) {
			return vanityName.Replace("/", "").Replace("<", "").Replace(">", "").Replace("?", "").Replace("%", "");
		}

		/// <summary>
		/// A hacky method of getting string until the next character is not a digit. This is used to isolate numeric values from the end of Steam URLs.
		/// </summary>
		/// <param name="input">The string to search.</param>
		/// <returns></returns>
		private static string GetUntilLastDigit(string input) {
			string num = "";
			for (int idx = 0; idx < input.Length; idx++) {
				char c = input.ElementAt(idx);
				if (char.IsDigit(c)) {
					num += c;
				} else {
					return num;
				}
			}
			return num;
		}

		/// <summary>
		/// A hacky method of finding the nearest space in a string. This will be replaced in future builds.
		/// </summary>
		/// <param name="input">The input string</param>
		/// <returns></returns>
		private static string GetUntilSpaceOrInvalid(string input) {
			string str = "";
			for (int idx = 0; idx < input.Length; idx++) {
				char c = input.ElementAt(idx);
				if (c != ' ' && c != '?' && c != '<' && c != '>' && c != '/') {
					str += c;
				} else {
					return str;
				}
			}
			return str;
		}

		/// <summary>
		/// Represents a response from Steam's servers. This is the major wrapper.
		/// </summary>
		private class SteamReturnData {

			[JsonProperty("response")]
			public Response Response { get; set; } = null;

		}

		/// <summary>
		/// Represents the data in the response. The success value will be 1 if successful, 42 if it is not.
		/// </summary>
		private class Response {

			[JsonProperty("steamid")]
			public string SteamID { get; set; } = "";

			[JsonProperty("success")]
			public int Success { get; set; } = 42;

		}
	}
}
