using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WhitePlatinumLib {
	public static class WordTableContext {
		static List<WordTable> Tables;

		static WordTableContext() {
			Tables = new List<WordTable>();
		}

		public static WordTable Begin() {
			WordTable T = new WordTable();
			Tables.Add(T);
			return GetCurrent();
		}

		public static WordTable GetCurrent() {
			if (Tables.Count == 0)
				return null;

			return Tables[Tables.Count - 1];
		}

		public static void End() {
			Tables.RemoveAt(Tables.Count - 1);
		}
	}
}