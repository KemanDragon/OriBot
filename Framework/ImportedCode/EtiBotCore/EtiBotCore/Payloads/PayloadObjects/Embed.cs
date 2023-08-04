using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads.Data;
using Newtonsoft.Json;

namespace EtiBotCore.Payloads.PayloadObjects {

	/// <summary>
	/// Represents an embed.
	/// </summary>
	internal class Embed : PayloadDataObject {

		/// <summary>
		/// The title of this embed.
		/// </summary>
		[JsonProperty("title")]
		public string? Title { get; set; }

		/// <summary>
		/// The type of embed that this is. This value type is deprecated but must be implemented for API compliance. Always <see cref="EmbedType.Rich"/> for webhook embeds.
		/// </summary>
		[JsonProperty("type")]
		public object? Type { get; set; }

		/// <summary>
		/// The description of this embed.
		/// </summary>
		[JsonProperty("description")]
		public string? Description { get; set; }

		/// <summary>
		/// The URL of this embed.
		/// </summary>
		[JsonProperty("url")]
		public string? URL { get; set; }

		/// <summary>
		/// The timestamp of the content.
		/// </summary>
		[JsonProperty("timestamp")]
		public ISO8601? Timestamp { get; set; }

		/// <summary>
		/// The color code for the embed's side strip.
		/// </summary>
		[JsonProperty("color")]
		public int? Color { get; set; }

		/// <summary>
		/// The footer of this embed.
		/// </summary>
		[JsonProperty("footer")]
		public FooterComponent? Footer { get; set; }

		/// <summary>
		/// The image in this embed.
		/// </summary>
		[JsonProperty("image")]
		public ImageComponent? Image { get; set; }

		/// <summary>
		/// The thumbnail of this embed.
		/// </summary>
		[JsonProperty("thumbnail")]
		public ThumbnailComponent? Thumbnail { get; set; }

		/// <summary>
		/// The video in of this embed.
		/// </summary>
		[JsonProperty("video")]
		public VideoComponent? Video { get; set; }

		/// <summary>
		/// The provider of this embed.
		/// </summary>
		[JsonProperty("provider")]
		public ProviderComponent? Provider { get; set; }

		/// <summary>
		/// The author of this embed.
		/// </summary>
		[JsonProperty("author")]
		public AuthorComponent? Author { get; set; }

		/// <summary>
		/// Fields in this embed.
		/// </summary>
		[JsonProperty("fields")]
		public FieldComponent[]? Fields { get; set; }

		public Embed() { }

		/// <summary>
		/// Constructs a new payload embed from the given embed.
		/// </summary>
		/// <param name="obj"></param>
		internal Embed(DiscordObjects.Universal.Embed obj) {
			Title = obj.Title;
			Type = (int)EmbedType.Rich;
			Description = obj.Description;
			URL = obj.URL;
			if (obj.Timestamp != null) Timestamp = new ISO8601(obj.Timestamp.Value);
			Color = obj.Color;
			if (obj.Footer != null) Footer = new FooterComponent(obj.Footer);
			if (obj.Image != null) Image = new ImageComponent(obj.Image);
			if (obj.Thumbnail != null) Thumbnail = new ThumbnailComponent(obj.Thumbnail);
			if (obj.Video != null) Video = new VideoComponent(obj.Video);
			if (obj.Provider != null) Provider = new ProviderComponent(obj.Provider);
			if (obj.Author != null) Author = new AuthorComponent(obj.Author);
			if (obj.Fields != null) {
				Fields = new FieldComponent[obj.Fields.Length];
				for (int idx = 0; idx < Fields.Length; idx++) {
					Fields[idx] = new FieldComponent(obj.Fields[idx]);
				}
			}
		}

		#region Embed Components

		/// <summary>
		/// The footer of an embed.
		/// </summary>
		internal class FooterComponent {

			/// <summary>
			/// The text on the footer of this embed.
			/// </summary>
			[JsonProperty("text"), JsonRequired]
			public string Text { get; set; } = string.Empty;

			/// <summary>
			/// The URL of the icon on this footer, if applicable.
			/// </summary>
			[JsonProperty("icon_url", NullValueHandling = NullValueHandling.Ignore)]
			public string? IconURL { get; set; }

			/// <summary>
			/// A proxied variant of <see cref="IconURL"/>.
			/// </summary>
			[JsonProperty("proxy_icon_url", NullValueHandling = NullValueHandling.Ignore)]
			public string? ProxyIconURL { get; set; }

			public FooterComponent() { }

			internal FooterComponent(DiscordObjects.Universal.Embed.FooterComponent cmp) {
				Text = cmp.Text;
				IconURL = cmp.IconURL;
				ProxyIconURL = cmp.ProxyIconURL;
			}

		}

		/// <summary>
		/// The image on an embed.
		/// </summary>
		internal class ImageComponent {

			/// <summary>
			/// The URL of the image. HTTPS only.
			/// </summary>
			[JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
			public string? URL { get; set; }

			/// <summary>
			/// A proxied variant of <see cref="URL"/>.
			/// </summary>
			[JsonProperty("proxy_url", NullValueHandling = NullValueHandling.Ignore)]
			public string? ProxyURL { get; set; }

			/// <summary>
			/// The height of this image.
			/// </summary>
			[JsonProperty("height", NullValueHandling = NullValueHandling.Ignore)]
			public int? Height { get; set; }

			/// <summary>
			/// The width of this image.
			/// </summary>
			[JsonProperty("width", NullValueHandling = NullValueHandling.Ignore)]
			public int? Width { get; set; }

			public ImageComponent() { }

			internal ImageComponent(DiscordObjects.Universal.Embed.ImageComponent cmp) {
				URL = cmp.URL;
				ProxyURL = cmp.ProxyURL;
				Height = cmp.Height;
				Width = cmp.Width;
			}

		}

		/// <summary>
		/// The thumbnail of an embed. Identical to an image.
		/// </summary>
		internal class ThumbnailComponent : ImageComponent {

			public ThumbnailComponent() : base() { }

			internal ThumbnailComponent(DiscordObjects.Universal.Embed.ThumbnailComponent cmp) : base(cmp) { }
		
		}

		/// <summary>
		/// A video in an embed.
		/// </summary>
		internal class VideoComponent {

			/// <summary>
			/// The URL to the video.
			/// </summary>
			[JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
			public string? URL { get; set; }

			/// <summary>
			/// The height of the video.
			/// </summary>
			[JsonProperty("height", NullValueHandling = NullValueHandling.Ignore)]
			public int? Height { get; set; }

			/// <summary>
			/// The width of the video.
			/// </summary>
			[JsonProperty("width", NullValueHandling = NullValueHandling.Ignore)]
			public int? Width { get; set; }

			public VideoComponent() { }

			internal VideoComponent(DiscordObjects.Universal.Embed.VideoComponent cmp) {
				URL = cmp.URL;
				Height = cmp.Height;
				Width = cmp.Width;
			}
		}

		/// <summary>
		/// The provider for an embed. Yeah you know as well as I do.
		/// </summary>
		internal class ProviderComponent {

			/// <summary>
			/// The name of the provider for this embed.
			/// </summary>
			[JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
			public string? Name { get; set; }

			/// <summary>
			/// The URL of the provider.
			/// </summary>
			[JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
			public string? URL { get; set; }

			public ProviderComponent() { }

			internal ProviderComponent(DiscordObjects.Universal.Embed.ProviderComponent cmp) {
				Name = cmp.Name;
				URL = cmp.URL;
			}
		}

		/// <summary>
		/// The author of an embed.
		/// </summary>
		internal class AuthorComponent {

			/// <summary>
			/// The name of the author.
			/// </summary>
			[JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
			public string? Name { get; set; }

			/// <summary>
			/// The URL to the author.
			/// </summary>
			[JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
			public string? URL { get; set; }

			/// <summary>
			/// The URL to the author's icon. HTTPS only.
			/// </summary>
			[JsonProperty("icon_url", NullValueHandling = NullValueHandling.Ignore)]
			public string? IconURL { get; set; }

			/// <summary>
			/// A proxied variant of <see cref="IconURL"/>.
			/// </summary>
			[JsonProperty("proxy_icon_url", NullValueHandling = NullValueHandling.Ignore)]
			public string? ProxyIconURL { get; set; }

			public AuthorComponent() { }

			internal AuthorComponent(DiscordObjects.Universal.Embed.AuthorComponent cmp) {
				Name = cmp.Name;
				URL = cmp.URL;
				IconURL = cmp.IconURL;
				ProxyIconURL = cmp.ProxyIconURL;
			}

		}

		/// <summary>
		/// A field of an embed.
		/// </summary>
		internal class FieldComponent {

			/// <summary>
			/// The title of this field.
			/// </summary>
			[JsonProperty("name")]
			public string Name { get; set; } = string.Empty;

			/// <summary>
			/// The content of this field.
			/// </summary>
			[JsonProperty("value")]
			public string Value { get; set; } = string.Empty;

			/// <summary>
			/// Whether or not this field displays inline.
			/// </summary>
			[JsonProperty("inline", NullValueHandling = NullValueHandling.Ignore)] 
			public bool? Inline { get; set; }

			public FieldComponent() { }

			internal FieldComponent(DiscordObjects.Universal.Embed.FieldComponent cmp) {
				Name = cmp.Name;
				Value = cmp.Value;
				Inline = cmp.Inline;
			}

		}

		#endregion


	}
}
