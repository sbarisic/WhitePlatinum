using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WhitePlatinumLib {
	public class WordTable {
		// TODO: Move this somewhere else, make it more generalized?
		[Flags]
		public enum ColumnStyle {
			NONE = 0,
			BOLD = 1 << 1,
			ITALIC = 1 << 2,
			UNDERLINE = 1 << 3
		}

		public class Column {
			public string Text;
			public string Identifier;
			public string Style;

			public ColumnStyle ParseStyle() {
				ColumnStyle[] Styles = Style.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries).Select(S => (ColumnStyle)Enum.Parse(typeof(ColumnStyle), S.Trim(), true)).ToArray();
				ColumnStyle Result = ColumnStyle.NONE;

				foreach (var S in Styles)
					Result |= S;

				return Result;
			}

			//public string RefArrayToken;
			public JArray RefArray;
		}

		List<Column> Columns;
		Column CurrentColumn;

		public WordTable() {
			Columns = new List<Column>();
		}

		public Column[] GetColumns() {
			return Columns.ToArray();
		}

		public void BeginColumn() {
			CurrentColumn = new Column();
		}

		public Column GetCurrentColumn() {
			return CurrentColumn;
		}

		public void EndColumn() {
			Columns.Add(GetCurrentColumn());
		}
	}
}