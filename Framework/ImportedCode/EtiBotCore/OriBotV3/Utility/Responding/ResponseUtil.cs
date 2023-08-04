using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EtiBotCore.DiscordObjects.Base;
using EtiBotCore.DiscordObjects.Guilds;
using EtiBotCore.DiscordObjects.Guilds.ChannelData;
using EtiBotCore.DiscordObjects.Universal;
using EtiBotCore.DiscordObjects.Universal.Data;
using EtiLogger.Logging;
using OldOriBot.Interaction;
using OldOriBot.UserProfiles;

namespace OldOriBot.Utility.Responding {

	/// <summary>
	/// A utility that allows ambiguous responses to messages and console
	/// </summary>
	public static class ResponseUtil {

		public const string PING_ON_REPLY = "PingOnReply";

		/*
		public const char CORNER_TL = '╔';

		public const char CORNER_BL = '╚';

		public const char CORNER_TR = '╗';

		public const char CORNER_BR = '╝';

		public const char VERT = '║';

		public const char HORZ = '═';

		public const char THIN_L_T = '╟';

		public const char THIN_R_T = '╢';

		public const char THIN_HORZ = '─';
		*/

		/// <summary>
		/// Create a typing signal in the channel this message exists in, or do nothing if the channel is null.
		/// </summary>
		/// <param name="onMessage"></param>
		/// <returns></returns>
		public static Task StartTypingAsync(Message onMessage) {
			if (onMessage == null) return Task.CompletedTask;
			return StartTypingAsync(onMessage.Channel);
		}

		/// <summary>
		/// Create a typing signal in the given channel, or do nothing if the channel is null.
		/// </summary>
		/// <param name="channel"></param>
		/// <returns></returns>
		public static Task StartTypingAsync(ChannelBase channel) {
			if (channel == null) return Task.CompletedTask;
			if (channel is TextChannel tx) return StartTypingAsync(tx);
			if (channel is DMChannel dm) return StartTypingAsync(dm);
			return Task.CompletedTask;
		}

		/// <inheritdoc cref="StartTypingAsync(ChannelBase)"/>
		public static Task StartTypingAsync(TextChannel channel) => channel.StartTypingAsync();

		/// <inheritdoc cref="StartTypingAsync(ChannelBase)"/>
		public static Task StartTypingAsync(DMChannel channel) => channel.StartTypingAsync();

		/*
		/// <summary>
		/// An inefficient utility method that creates an array out of varargs, excluding null elements.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="components"></param>
		/// <returns></returns>
		private static T[] ArrayOf<T>(params T[] components) where T : class {
			List<T> result = new List<T>();
			foreach (T item in components) {
				if (item == null) continue;
				result.Add(item);
			}
			return result.ToArray();
		}
		*/

		/// <summary>
		/// Responds to the given message in the same channel as the message. This is special because if there is no message, it will write it to the console.
		/// </summary>
		/// <param name="message">The message to reply to.</param>
		/// <param name="toLog">The <see cref="Logger"/> to use if this happens to run in the console.</param>
		/// <param name="response">The text based response to send.</param>
		/// <param name="embed">The embed to send.</param>
		/// <param name="mentions">Limitations on who can be mentioned. If <see langword="null"/>, it defaults to <see cref="AllowedMentions.Reply"/></param>
		/// <param name="asReply">If the sent message should reply to <paramref name="message"/>.</param>
		/// <param name="inConsoleToo">Show this in the console anyway even if this is running on Discord.</param>
		/// <param name="deleteAfterMS">After this many milliseconds, the message will be deleted. A negative or zero value will cause no deletion time.</param>
		/// <param name="attachments">One or more files to attach.</param>
		/// <returns></returns>
		public static async Task RespondToAsync(Message message, Logger toLog, string response = null, Embed embed = null, AllowedMentions mentions = null, bool asReply = true, bool inConsoleToo = false, int deleteAfterMS = -1, params FileInfo[] attachments) {
			Message responseInstance = null;
			if (mentions == null) mentions = AllowedMentions.Reply;
			if (message != null) {
				// Special behavior: For replies, get user prefs.
				if (message.AuthorMember != null) {
					bool state = true;
					UserProfile.GetOrCreateProfileOf(message.AuthorMember).UserData.TryGetValue(PING_ON_REPLY, ref state);
					mentions.PingRepliedUser = state;
				}
				responseInstance = await message.ReplyAsync(response, embed, mentions, asReply, attachments ?? Array.Empty<FileInfo>());
			}
			if ((!string.IsNullOrWhiteSpace(response) && message == null) || inConsoleToo) toLog.WriteLine(response);
			if ((message == null && embed != null) || inConsoleToo) WriteEmbed(embed, toLog);
			if (responseInstance != null) {
				if (deleteAfterMS > 0) {
					// Below: Don't await.
					_ = Task.Run(async () => {
						await Task.Delay(deleteAfterMS);
						await responseInstance.DeleteAsync("Message was scheduled for self-deletion after " + deleteAfterMS + "ms");
					});
				}
			}
		}

		/*
		/// <inheritdoc cref="RespondToAsync(Message, Logger, string, Embed, AllowedMentions, bool, bool, int, FileInfo[])"/>
		/// <param name="attachment">A file to attach</param>
		/// <remarks>
		/// This variant exists for backwards compatibility. To send multiple files, pass in a <see cref="FileInfo"/> array.
		/// </remarks>
		public static Task RespondToAsync(Message message, Logger toLog, string response = null, Embed embed = null, AllowedMentions mentions = null, bool asReply = true, bool inConsoleToo = false, int deleteAfterMS = -1, FileInfo? attachment = null) {
			return RespondToAsync(message, toLog, response, embed, mentions, asReply, inConsoleToo, deleteAfterMS, ArrayOf(attachment));
		}
		*/

		/// <summary>
		/// Identical to <see cref="RespondToAsync(Message, Logger, string, Embed, AllowedMentions, bool?, bool, FileInfo)"/> but you can change the channel the reply is posted in.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="otherChannel"></param>
		/// <param name="toLog"></param>
		/// <param name="response"></param>
		/// <param name="embed"></param>
		/// <param name="mentions"></param>
		/// <param name="asReply"></param>
		/// <param name="inConsoleToo"></param>
		/// <param name="deleteAfterMS">After this many milliseconds, the message will be deleted. A negative or zero value will cause no deletion time.</param>
		/// <param name="attachments">One or more files to attach.</param>
		/// <returns></returns>
		public static async Task RespondToInAsync(Message message, ChannelBase otherChannel, Logger toLog, string response = null, Embed embed = null, AllowedMentions mentions = null, bool asReply = true, bool inConsoleToo = false, int deleteAfterMS = -1, params FileInfo[] attachments) {
			Message responseInstance = null;
			if (mentions == null) mentions = AllowedMentions.Reply;
			if (message != null) {
				// Special behavior: For replies, get user prefs.
				if (message.AuthorMember != null) {
					bool state = true;
					UserProfile.GetOrCreateProfileOf(message.AuthorMember).UserData.TryGetValue(PING_ON_REPLY, ref state);
					mentions.PingRepliedUser = state;
				}
				if (otherChannel is TextChannel txt) {
					if (asReply) {
						responseInstance = await txt.SendReplyMessageAsync(response, embed, mentions, message, attachments ?? Array.Empty<FileInfo>());
					} else {
						responseInstance = await txt.SendMessageAsync(response, embed, mentions, attachments ?? Array.Empty<FileInfo>());
					}
				} else if (otherChannel is DMChannel dm) {
					if (asReply) {
						responseInstance = await dm.SendReplyMessageAsync(response, embed, mentions, message, attachments ?? Array.Empty<FileInfo>());
					} else {
						responseInstance = await dm.SendMessageAsync(response, embed, mentions, attachments ?? Array.Empty<FileInfo>());
					}
				}
			}
			if ((!string.IsNullOrWhiteSpace(response) && message == null) || inConsoleToo) toLog.WriteLine(response);
			if ((message == null && embed != null) || inConsoleToo) WriteEmbed(embed, toLog);
			if (responseInstance != null) {
				if (deleteAfterMS > 0) {
					// Below: Don't await.
					_ = Task.Run(async () => {
						await Task.Delay(deleteAfterMS);
						await responseInstance.DeleteAsync("Message was scheduled for self-deletion after " + deleteAfterMS + "ms");
					});
				}
			}
		}

		/*
		/// <inheritdoc cref="RespondToInAsync(Message, ChannelBase, Logger, string, Embed, AllowedMentions, bool, bool, int, FileInfo[])"/>
		/// <param name="attachment">A file to attach</param>
		/// <remarks>
		/// This variant exists for backwards compatibility. To send multiple files, pass in a <see cref="FileInfo"/> array.
		/// </remarks>
		public static Task RespondToInAsync(Message message, ChannelBase otherChannel, Logger toLog, string response = null, Embed embed = null, AllowedMentions mentions = null, bool asReply = true, bool inConsoleToo = false, int deleteAfterMS = -1, FileInfo attachment = null) {
			return RespondToInAsync(message, otherChannel, toLog, response, embed, mentions, asReply, inConsoleToo, deleteAfterMS, ArrayOf(attachment));
		}
		*/

		/// <summary>
		/// Effectively sending a message in a channel.
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="toLog"></param>
		/// <param name="response"></param>
		/// <param name="embed"></param>
		/// <param name="mentions"></param>
		/// <param name="inConsoleToo"></param>
		/// <param name="deleteAfterMS">After this many milliseconds, the message will be deleted. A negative or zero value will cause no deletion time.</param>
		/// <param name="attachments">One or more files to attach.</param>
		/// <returns></returns>
		public static async Task RespondInAsync(TextChannel channel, Logger toLog, string response = null, Embed embed = null, AllowedMentions mentions = null, bool inConsoleToo = false, int deleteAfterMS = -1, params FileInfo[] attachments) {
			Message responseInstance = null;
			if (mentions == null) mentions = AllowedMentions.Reply;
			if (channel != null) {
				responseInstance = await channel.SendMessageAsync(response, embed, mentions, attachments ?? Array.Empty<FileInfo>());
			}
			if ((!string.IsNullOrWhiteSpace(response) && channel == null) || inConsoleToo) toLog.WriteLine(response);
			if ((channel == null && embed != null) || inConsoleToo) WriteEmbed(embed, toLog);
			if (responseInstance != null) {
				if (deleteAfterMS > 0) {
					// Below: Don't await.
					_ = Task.Run(async () => {
						await Task.Delay(deleteAfterMS);
						await responseInstance.DeleteAsync("Message was scheduled for self-deletion after " + deleteAfterMS + "ms");
					});
				}
			}
		}

		/*
		/// <inheritdoc cref="RespondInAsync(TextChannel, Logger, string, Embed, AllowedMentions, bool, int, FileInfo[])"/>
		/// <param name="attachment">A file to attach</param>
		/// <remarks>
		/// This variant exists for backwards compatibility. To send multiple files, pass in a <see cref="FileInfo"/> array.
		/// </remarks>
		public static Task RespondInAsync(TextChannel channel, Logger toLog, string response = null, Embed embed = null, AllowedMentions mentions = null, bool inConsoleToo = false, int deleteAfterMS = -1, FileInfo attachment = null) {
			return RespondInAsync(channel, toLog, response, embed, mentions, inConsoleToo, deleteAfterMS, ArrayOf(attachment));
		}
		*/

		/// <summary>
		/// Dumps an embed as text.
		/// </summary>
		/// <param name="embed"></param>
		private static void WriteEmbed(Embed embed, Logger toLog) {
			if (embed == null) return;
			if (embed.Title != null) toLog.WriteLine(embed.Title);
			if (embed.Description != null) toLog.WriteLine("§7" + embed.Description);
			if (embed.Fields != null) {
				if (embed.Fields.Length > 0) {
					foreach (Embed.FieldComponent field in embed.Fields) {
						toLog.WriteLine(field.Name);
						toLog.WriteLine("§7" + field.Value);
					}
				}
			}
		}

	}
}
