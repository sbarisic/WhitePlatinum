using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WhitePlatinumLib {
	public struct PageDimensions {
		public uint Width;
		public uint Height;
		public uint MarginLeft;
		public uint MarginRight;
		public int MarginTop;
		public int MarginBottom;

		public uint FillWidth {
			get {
				return Width - (MarginLeft + MarginRight);
			}
		}

		public PageDimensions(uint Width, uint Height, uint MarginLeft, uint MarginRight, int MarginTop, int MarginBottom) {
			this.Width = Width;
			this.Height = Height;
			this.MarginLeft = MarginLeft;
			this.MarginRight = MarginRight;
			this.MarginTop = MarginTop;
			this.MarginBottom = MarginBottom;
		}
	}
}