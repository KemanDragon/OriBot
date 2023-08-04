using System;
using System.Collections.Generic;
using System.Text;
using EtiBotCore.DiscordObjects.Universal;
using EtiLogger.Data.Structs;

namespace EtiBotCore.DiscordObjects.Factory {

	/// <summary>
	/// Used to create <see cref="Embed"/> objects that can be sent.
	/// </summary>
	public class EmbedBuilder {

		/// <summary>
		/// The title of this embed. This has a maximum of 256 characters.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">If the value is set to something more than 256 characters long.</exception>
		public string? Title {
			get => _Title;
			set {
				if (string.IsNullOrWhiteSpace(value)) {
					_Title = null;
					return;
				}
				if (value.Length > 256) throw new ArgumentOutOfRangeException(nameof(value), "Title cannot be more than 256 characters long.");
				_Title = value;
			}
		}
		private string? _Title = null;

		/// <summary>
		/// The description of this embed. This has a maximum of 2048 characters.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">If the value is set to something more than 2048 characters long.</exception>
		public string? Description {
			get => _Description;
			set {
				if (string.IsNullOrWhiteSpace(value)) {
					_Description = null;
					return;
				}
				if (value.Length > 2048) throw new ArgumentOutOfRangeException(nameof(value), "Description cannot be more than 2048 characters long.");
				_Description = value;
			}
		}
		private string? _Description = null;

		/// <summary>
		/// The link on this embed, which turns the title into a hyperlink.
		/// </summary>
		public Uri? Link { get; set; }

		/// <summary>
		/// The timestamp associated with this embed, which will display at the bottom of the embed.
		/// </summary>
		/// <remarks>
		/// If a footer is defined, the timestamp will display to the right of the footer text separated by a vertical bar <c>|</c> -- for example: <c>Footer Text | Fri Dec 25th, 2020 at 5:00 AM</c>
		/// </remarks>
		public DateTimeOffset? Timestamp { get; set; }

		/// <summary>
		/// The color associated with this embed, which determines the left-hand sidebar's color. Set to <see langword="null"/> to use Discord's default.
		/// </summary>
		public Color? Color { get; set; }

		/// <summary>
		/// The image associated with this embed. This is a large square image situated in the center of the embed that usually takes up as much space as it can.
		/// </summary>
		public Uri? Image { get; }

		/// <summary>
		/// A thumbnail image associated with this embed. This is a smaller square image situated on the right side of the embed, and has a smaller size limit than <see cref="Image"/> (I think it's 256x or something?)
		/// </summary>
		public Uri? Thumbnail { get; }
		
		private AuthorEntry? Author { get; set; }

		private List<Field> Fields { get; } = new List<Field>();

		private FooterEntry? Footer { get; set; }

		/// <summary>
		/// Create an <see cref="Embed"/> from the information in this <see cref="EmbedBuilder"/>
		/// </summary>
		/// <returns></returns>
		public Embed Build() {
			Embed embed = new Embed() {
				Title = Title,
				Description = Description,
				URL = Link?.ToString(),
				Timestamp = Timestamp,
				Color = Color?.Value
			};
			if (Author != null) {
				embed.Author = new Embed.AuthorComponent {
					Name = Author.Name,
					IconURL = Author.Icon?.ToString(),
					URL = Author.Url?.ToString()
				};
			}
			if (Footer != null) {
				embed.Footer = new Embed.FooterComponent {
					Text = Footer.Text,
					IconURL = Footer.Image?.ToString()
				};
			}
			if (Fields.Count > 0) {
				embed.Fields = new Embed.FieldComponent[Fields.Count];
				for (int idx = 0; idx < Fields.Count; idx++) {
					Field field = Fields[idx];
					embed.Fields[idx] = new Embed.FieldComponent {
						Name = field.Name,
						Value = field.Value,
						Inline = field.Inline
					};
				}
			}
			if (Image != null) {
				embed.Image = new Embed.ImageComponent {
					URL = Image.ToString()
				};
			}
			if (Thumbnail != null) {
				embed.Thumbnail = new Embed.ThumbnailComponent {
					URL = Thumbnail.ToString()
				};
			}
			return embed;
		}

		/// <summary>
		/// Sets <see cref="Timestamp"/> to right now. Returns <see langword="this"/> for chaining.
		/// </summary>
		public EmbedBuilder StampToNow() {
			Timestamp = DateTimeOffset.UtcNow;
			return this;
		}

		/// <summary>
		/// Sets the footer of this embed to the text <c>Note: All dates are in the format of: day/month/year hours:minutes:seconds UTC</c>. Returns <see langword="this"/> for chaining. 
		/// </summary>
		/// <returns></returns>
		public EmbedBuilder AddTimeFormatFooter() {
			SetFooter("Note: All dates are in the format of: day/month/year days.hours:minutes:seconds UTC", new Uri("https://i.imgur.com/syNiIyT.png"));
			return this;
		}

		/// <summary>
		/// Sets the author field of this embed. The author field appends a small circular picture at the top left corner of the embed (created by <paramref name="image"/>) with the given author name right next to it. If <paramref name="link"/> is provided, clicking the author's name will lead to that link. Returns <see langword="this"/> for chaining.
		/// </summary>
		/// <remarks>
		/// The author field appears above the embed's title.
		/// </remarks>
		/// <param name="name">The author's name, which displays at the top left of the embed. Max 256 characters.</param>
		/// <param name="link">The link that clicking the author's name leads to, or <see langword="null"/> to have no link.</param>
		/// <param name="image">A small circular image placed to the left of the author's name, at the top left of the embed.</param>
		/// <exception cref="ArgumentException">If the author's name is null, empty, or more than 256 characters long.</exception>
		public EmbedBuilder SetAuthor(string name, Uri? link = null, Uri? image = null) {
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException();
			if (name.Length > 256) throw new ArgumentException();
			Author = new AuthorEntry {
				Name = name,
				Url = link,
				Icon = image
			};
			return this;
		}

		/// <summary>
		/// Removes the author field from the embed, granted one has been added beforehand with <see cref="SetAuthor(string, Uri?, Uri?)"/>
		/// </summary>
		public void RemoveAuthor() {
			Author = null;
		}

		/// <summary>
		/// Sets the footer field of this embed. The footer field appears with a small rounded square icon in the lower left corner with the given footer text next to it, if applicable. Returns <see langword="this"/> for chaining.
		/// </summary>
		/// <param name="text">The text to display on the footer.</param>
		/// <param name="icon">The icon to display on the footer.</param>
		/// <exception cref="ArgumentException">If the text is more than 2048 characters long.</exception>
		/// <exception cref="ArgumentNullException">If text is null.</exception>
		public EmbedBuilder SetFooter(string text, Uri? icon = null) {
			if (text == null) throw new ArgumentNullException(nameof(text));
			if (text.Length > 2048) throw new ArgumentException();
			Footer = new FooterEntry {
				Text = text,
				Image = icon
			};
			return this;
		}

		/// <summary>
		/// Removes the footer field from the embed, granted one has been added beforehand with <see cref="SetFooter(string?, Uri?)"/>
		/// </summary>
		public void RemoveFooter() {
			Footer = null;
		}

		/// <summary>
		/// Add a new field to this embed. Returns the index of the field in the registry.
		/// </summary>
		/// <param name="name">The name of this field.</param>
		/// <param name="value">The body of this field.</param>
		/// <param name="inline">If <see langword="true"/>, this field can display horizontally to other fields to form a grid layout rather than a list layout.</param>
		/// <exception cref="InvalidOperationException">If there are 25 fields already, which is the maximum that Discord allows.</exception>
		/// <exception cref="ArgumentException">If the field's name is more than 128 chars, or the field's value is more than 1024 chars, or either of the two are empty strings.</exception>
		public int AddField(string name, string value, bool inline = false) {
			if (Fields.Count == 25) throw new InvalidOperationException("Cannot add more than 25 fields to an embed.");
			if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value) || name.Length > 128 || value.Length > 1024) throw new ArgumentException("Field name or value is empty or null, or the name is longer than 128 chars, or the value is longer than 1024 chars.");
			Fields.Add(new Field {
				Name = name,
				Value = value,
				Inline = inline
			});
			return Fields.Count - 1;
		}

		/// <summary>
		/// Removes the field at the given index.
		/// </summary>
		/// <param name="index"></param>
		public void RemoveField(int index) {
			Fields.RemoveAt(index);
		}


		private class AuthorEntry {

			/// <summary>
			/// The name of the author.
			/// </summary>
			public string Name { get; set; } = string.Empty;

			/// <summary>
			/// The URL that click on the author's name goes to.
			/// </summary>
			public Uri? Url { get; set; }

			/// <summary>
			/// The author's icon.
			/// </summary>
			public Uri? Icon { get; set; }

		}

		private class Field {

			/// <summary>
			/// The name of this field.
			/// </summary>
			public string Name { get; set; } = string.Empty;

			/// <summary>
			/// The body of this field.
			/// </summary>
			public string Value { get; set; } = string.Empty;

			/// <summary>
			/// Whether or not to inline this field.
			/// </summary>
			public bool Inline { get; set; } = false;

		}

		private class FooterEntry {

			/// <summary>
			/// The text on the footer.
			/// </summary>
			public string Text { get; set; } = string.Empty;

			/// <summary>
			/// The image of the footer.
			/// </summary>
			public Uri? Image { get; set; }

		}

	}
}
