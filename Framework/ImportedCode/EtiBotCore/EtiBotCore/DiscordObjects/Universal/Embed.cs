using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.Payloads.Data;

namespace EtiBotCore.DiscordObjects.Universal {

	/// <summary>
	/// Represents an embed.
	/// </summary>
	
	public class Embed {

		/// <summary>
		/// The title of this embed, or <see langword="null"/> if it doesn't have one.
		/// </summary>
		public string? Title { get; internal set; }

		/// <summary>
		/// The description of this embed, or <see langword="null"/> if it doesn't have one.
		/// </summary>
		public string? Description { get; internal set; }

		/// <summary>
		/// The URL of this embed.
		/// </summary>
		public string? URL { get; internal set; }

		/// <summary>
		/// The timestamp of the content.
		/// </summary>
		public DateTimeOffset? Timestamp { get; internal set; }

		/// <summary>
		/// The color code for the embed's side strip, or <see langword="null"/> if it uses Discord's default color.
		/// </summary>
		public int? Color { get; internal set; }

		/// <summary>
		/// The footer of this embed, or <see langword="null"/> if it doesn't have one.
		/// </summary>
		public FooterComponent? Footer { get; internal set; }

		/// <summary>
		/// The image in this embed, or <see langword="null"/> if it doesn't have one.
		/// </summary>
		public ImageComponent? Image { get; internal set; }

		/// <summary>
		/// The thumbnail of this embed, or <see langword="null"/> if it doesn't have one.
		/// </summary>
		public ThumbnailComponent? Thumbnail { get; internal set; }

		/// <summary>
		/// The video in of this embed, or <see langword="null"/> if it doesn't have one.
		/// </summary>
		public VideoComponent? Video { get; internal set; }

		/// <summary>
		/// The provider of this embed, or <see langword="null"/> if it doesn't have one.
		/// </summary>
		public ProviderComponent? Provider { get; internal set; }

		/// <summary>
		/// The author of this embed, or <see langword="null"/> if it doesn't have one.
		/// </summary>
		public AuthorComponent? Author { get; internal set; }

		/// <summary>
		/// Fields in this embed, or <see langword="null"/> if it doesn't have one.
		/// </summary>
		public FieldComponent[]? Fields { get; internal set; }

		/// <summary>
		/// Constructs a new empty Embed.
		/// </summary>
		internal Embed() { }

		/// <summary>
		/// Constructs a new embed from the given payload embed.
		/// </summary>
		/// <param name="obj"></param>
		internal Embed(Payloads.PayloadObjects.Embed obj) {
			Title = obj.Title;
			Description = obj.Description;
			URL = obj.URL;
			if (obj.Timestamp != null) Timestamp = obj.Timestamp.Value.DateTime;
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

		/// <summary>
		/// Performs a deep-copy of this embed.
		/// </summary>
		/// <returns></returns>
		internal Embed Clone() {
			// https://youtu.be/lwQ9unSkpsI?t=11
			return new Embed(new Payloads.PayloadObjects.Embed(this));
		}

		#region Embed Components

		/// <summary>
		/// The footer of an embed.
		/// </summary>
		public class FooterComponent {

			/// <summary>
			/// The text on the footer of this embed.
			/// </summary>
			public string Text { get; set; } = string.Empty;

			/// <summary>
			/// The URL of the icon on this footer, if applicable.
			/// </summary>
			public string? IconURL { get; set; }

			/// <summary>
			/// A proxied variant of <see cref="IconURL"/>.
			/// </summary>
			public string? ProxyIconURL { get; set; }

			internal FooterComponent() { }

			internal FooterComponent(Payloads.PayloadObjects.Embed.FooterComponent cmp) {
				Text = cmp.Text;
				IconURL = cmp.IconURL;
				ProxyIconURL = cmp.ProxyIconURL;
			}

		}

		/// <summary>
		/// The image on an embed.
		/// </summary>
		public class ImageComponent {

			/// <summary>
			/// The URL of the image. HTTPS only.
			/// </summary>
			public string? URL { get; set; }

			/// <summary>
			/// A proxied variant of <see cref="URL"/>.
			/// </summary>
			public string? ProxyURL { get; set; }

			/// <summary>
			/// The height of this image.
			/// </summary>
			public int? Height { get; set; }

			/// <summary>
			/// The width of this image.
			/// </summary>
			public int? Width { get; set; }

			internal ImageComponent() { }

			internal ImageComponent(Payloads.PayloadObjects.Embed.ImageComponent cmp) {
				URL = cmp.URL;
				ProxyURL = cmp.ProxyURL;
				Height = cmp.Height;
				Width = cmp.Width;
			}

		}

		/// <summary>
		/// The thumbnail of an embed. Identical to an image.
		/// </summary>
		public class ThumbnailComponent : ImageComponent {

			internal ThumbnailComponent() : base() { }

			internal ThumbnailComponent(Payloads.PayloadObjects.Embed.ThumbnailComponent cmp) : base(cmp) { }

		}

		/// <summary>
		/// A video in an embed.
		/// </summary>
		public class VideoComponent {

			/// <summary>
			/// The URL to the video.
			/// </summary>
			public string? URL { get; set; }

			/// <summary>
			/// The height of the video.
			/// </summary>
			public int? Height { get; set; }

			/// <summary>
			/// The width of the video.
			/// </summary>
			public int? Width { get; set; }

			internal VideoComponent() { }

			internal VideoComponent(Payloads.PayloadObjects.Embed.VideoComponent cmp) {
				URL = cmp.URL;
				Height = cmp.Height;
				Width = cmp.Width;
			}
		}

		/// <summary>
		/// The provider for an embed. Yeah you know as well as I do.
		/// </summary>
		public class ProviderComponent {

			/// <summary>
			/// The name of the provider for this embed.
			/// </summary>
			public string? Name { get; set; }

			/// <summary>
			/// The URL of the provider.
			/// </summary>
			public string? URL { get; set; }


			internal ProviderComponent() { }

			internal ProviderComponent(Payloads.PayloadObjects.Embed.ProviderComponent cmp) {
				Name = cmp.Name;
				URL = cmp.URL;
			}
		}

		/// <summary>
		/// The author of an embed.
		/// </summary>
		public class AuthorComponent {

			/// <summary>
			/// The name of the author.
			/// </summary>
			public string? Name { get; set; }

			/// <summary>
			/// The URL to the author.
			/// </summary>
			public string? URL { get; set; }

			/// <summary>
			/// The URL to the author's icon. HTTPS only.
			/// </summary>
			public string? IconURL { get; set; }

			/// <summary>
			/// A proxied variant of <see cref="IconURL"/>.
			/// </summary>
			public string? ProxyIconURL { get; set; }

			internal AuthorComponent() { }

			internal AuthorComponent(Payloads.PayloadObjects.Embed.AuthorComponent cmp) {
				Name = cmp.Name;
				URL = cmp.URL;
				IconURL = cmp.IconURL;
				ProxyIconURL = cmp.ProxyIconURL;
			}

		}

		/// <summary>
		/// A field of an embed.
		/// </summary>
		public class FieldComponent {

			/// <summary>
			/// The title of this field.
			/// </summary>
			public string Name { get; set; } = string.Empty;

			/// <summary>
			/// The content of this field.
			/// </summary>
			public string Value { get; set; } = string.Empty;

			/// <summary>
			/// Whether or not this field displays inline.
			/// </summary>
			public bool? Inline { get; set; }

			internal FieldComponent() { }

			internal FieldComponent(Payloads.PayloadObjects.Embed.FieldComponent cmp) {
				Name = cmp.Name;
				Value = cmp.Value;
				Inline = cmp.Inline;
			}

		}

		#endregion
	}
}
