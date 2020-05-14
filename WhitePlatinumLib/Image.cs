using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace WhitePlatinumLib {
	public enum ImageFormat {
		Png
	}

	public class Image {
		public int Width;
		public int Height;
		public int HorizontalResolution;
		public int VerticalResolution;

		public void Save(Stream OutStream, ImageFormat Fmt) {
			throw new NotImplementedException();
		}

		public static Image FromStream(Stream S) {
			throw new NotImplementedException();
		}
	}
}
