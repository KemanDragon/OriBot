using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using EtiBotCore.Data.Structs;
using EtiLogger.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EtiBotCore.DiscordObjects.Factory {

	/// <summary>
	/// A container class for sending API requests.
	/// </summary>
	public class APIRequestData {

		/// <summary>
		/// If this request supports JSON, these are the included parameters that will be sent with the request.
		/// </summary>
		protected readonly Dictionary<string, JToken?> JSONParams = new Dictionary<string, JToken?>();

		/// <summary>
		/// The parameters to include in the URL.
		/// </summary>
		public List<object> Params { get; set; } = new List<object>();

		/// <summary>
		/// The files to include with this API request.
		/// </summary>
		/// <remarks>
		/// <strong>This is strictly for sending messages.</strong> This field does absolutely nothing on any other requests.
		/// </remarks>
		public IReadOnlyList<FileInfo>? Files => _Files;
		private List<FileInfo> _Files = new List<FileInfo>();
		private FileAttachment[] Attachments = Array.Empty<FileAttachment>();

		/// <summary>
		/// Only for administrative actions. Why was this operation performed? This goes to the audit log.
		/// </summary>
		public string? Reason { get; set; } = null;
		
		/// <summary>
		/// Sets the <see cref="Files"/> array
		/// </summary>
		/// <param name="files"></param>
		public void SetFiles(IEnumerable<FileInfo?> files) {
			_Files = files.Where(file => file != null && file.Exists).ToList()!;
			FileAttachment[] attachments = new FileAttachment[_Files.Count];
			for (int idx = 0; idx < _Files.Count; idx++) {
				attachments[idx] = new FileAttachment(_Files[idx], idx);
			}
			Attachments = attachments;
			SetJsonField("attachments", attachments);
		}

		internal List<IDisposable> AddFilesToContent(MultipartFormDataContent content) {
			List<IDisposable> dispose = new List<IDisposable>();
			for (int idx = 0; idx < Attachments.Length; idx++) {
				FileInfo file = Attachments[idx].File;
				byte[] data = File.ReadAllBytes(file.FullName);
				Debug.WriteLine(data.Length);

				ByteArrayContent fileContent = new ByteArrayContent(data); // DO NOT DISPOSE HERE!
				fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") {
					Name = $"files[{Attachments[idx].ID}]",
					FileName = file.Name
				};
				fileContent.Headers.ContentLength = data.Length;
				content.Add(fileContent);
				dispose.Add(fileContent);
			}
			return dispose;
		}

		/// <summary>
		/// Sets a json parameter on this specific request. Returns <see langword="this"/> for chaining.
		/// </summary>
		/// <param name="key">The key of the parameter.</param>
		/// <param name="value">The value associated with the parameter.</param>
		public APIRequestData SetJsonField(string key, object? value) {
			if (value != null) {
				if (value is JToken token) {
					JSONParams[key] = token;
				} else {
					JSONParams[key] = JToken.FromObject(value);
				}
			} else {
				JSONParams[key] = null;
			}
			return this;
		}

		/*
		/// <summary>
		/// Sets a json parameter on this specific request. Returns <see langword="this"/> for chaining.
		/// </summary>
		/// <param name="key">The key of the parameter.</param>
		/// <param name="value">The value associated with the parameter.</param>
		public APIRequestData SetJsonField(string key, JToken value) {
			JSONParams[key] = value;
			return this;
		}
		*/

		/// <summary>
		/// Removes the given key from the json parameters of this request.
		/// </summary>
		/// <param name="key">The key of the parameter to remove.</param>
		public void RemoveJsonField(string key) {
			if (JSONParams.ContainsKey(key)) JSONParams.Remove(key);
		}

		/// <summary>
		/// Clears all JSON parameters from this request.
		/// </summary>
		public void ResetJsonParams() {
			JSONParams.Clear();
		}

		/// <summary>
		/// Returns the JSON parameters as a string.
		/// </summary>
		/// <returns></returns>
		public string GetJson() {
			if (JSONParams.Count == 0) return "{}";
			return JsonConvert.SerializeObject(JSONParams);
		}

		/// <summary>
		/// Returns this request data as a URL parameter string (rather than a json string) for HTTP GET requests.
		/// </summary>
		/// <returns></returns>
		public string GetURLParams() {
			int count = JSONParams.Count;
			if (count == 0) return string.Empty;
			StringBuilder query = new StringBuilder("?");
			
			int current = 0;
			foreach (KeyValuePair<string, JToken?> json in JSONParams) {
				query.Append(HttpUtility.UrlEncode(json.Key) + "=" + HttpUtility.UrlEncode(json.Value?.ToString() ?? "null"));
				if (current < count - 1) {
					query.Append("&");
				}
				current++;
			}
			return query.ToString();
		}

		private sealed class FileAttachment {

			[JsonProperty("id")]
			//public Snowflake ID { get; }
			public int ID { get; }

			[JsonProperty("description")]
			public string Description { get; set; } = string.Empty;

			[JsonProperty("filename")]
			public string FileName => File.Name;

			[JsonIgnore]
			public FileInfo File { get; }

			public FileAttachment(FileInfo fromFile, int idx = 0) {
				File = fromFile;
				// ID = Snowflake.UtcNow;
				ID = idx;
			}

		}
	}
}
