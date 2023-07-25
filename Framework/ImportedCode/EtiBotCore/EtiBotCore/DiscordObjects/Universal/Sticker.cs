using EtiBotCore.Data.Structs;
using EtiBotCore.Payloads;
using EtiBotCore.Payloads.Data;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EtiBotCore.DiscordObjects.Universal {

	/// <summary>
	/// A sticker object in a message.
	/// </summary>
	public class Sticker : DiscordObject {

		private static readonly Dictionary<Snowflake, Sticker> AllStickers = new Dictionary<Snowflake, Sticker>();

		/// <summary>
		/// The name of this sticker.
		/// </summary>
		public string Name {
			get => _Name;
			set {
				SetProperty(ref _Name, value);
			}
		}
		private string _Name = string.Empty;

		/// <summary>
		/// The format this sticker's file uses.
		/// </summary>
		public StickerFormatType Format { get; }

		internal Sticker(Payloads.PayloadObjects.Sticker plSticker) : base(plSticker.ID) {
			_Name = plSticker.Name;
			Format = (StickerFormatType)plSticker.Format;
		}

		/// <summary>
		/// Returns a new instance of a sticker or returns an existing instance of a sticker for the given payload.
		/// </summary>
		/// <param name="plSticker">The sticker payload.</param>
		/// <returns></returns>
		internal static Sticker GetOrCreate(Payloads.PayloadObjects.Sticker plSticker) {
			if (AllStickers.ContainsKey(plSticker.ID)) {
				return AllStickers[plSticker.ID];
			}
			Sticker newInstance = new Sticker(plSticker);
			AllStickers[plSticker.ID] = newInstance;
			return newInstance;
		}

		/// <inheritdoc/>
		protected internal override Task Update(PayloadDataObject obj, bool skipNonNullFields = false) {
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		protected override Task<HttpResponseMessage?> SendChangesToDiscord(IReadOnlyDictionary<string, object> changesAndOriginalValues, string? changeReasons) {
			throw new NotImplementedException();
		}
	}
}
