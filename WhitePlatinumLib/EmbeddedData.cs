using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Web;

namespace WhitePlatinumLib {
	public class EmbeddedData {
		public ContentType Type;
		public string Data;

		// Hyperlink data caching, don't download each time
		byte[] HyperlinkData;

		public EmbeddedData(JToken TypeToken, JToken DataToken) {
			Type = new ContentType(TypeToken.Value<string>());
			Data = DataToken.Value<string>();
			HyperlinkData = null;
		}

		public string GetParameter(string Name, string Default = null) {
			if (Type.Parameters.ContainsKey(Name))
				return Type.Parameters[Name];

			return Default;
		}

		public byte[] ParseBinaryData() {
			string DataFormat = GetParameter("format", "raw").ToLower();

			switch (DataFormat) {
				case "raw":
					return Encoding.UTF8.GetBytes(Data);

				case "hyperlink": {
						if (HyperlinkData != null)
							return HyperlinkData;

						return HyperlinkData = Utils.DownloadRaw(Data);
					}

				case "base64":
					return Convert.FromBase64String(Data);
			}

			throw new NotImplementedException("EmbeddedData format not recognized " + DataFormat);
		}

		public Stream ParseStreamData() {
			MemoryStream MS = new MemoryStream(ParseBinaryData());
			MS.Seek(0, SeekOrigin.Begin);
			return MS;
		}

		public string ParseStringData() {
			return Encoding.UTF8.GetString(ParseBinaryData());
		}

		public Image ParseImageData() {
			return Image.FromStream(ParseStreamData());
		}
	}
}