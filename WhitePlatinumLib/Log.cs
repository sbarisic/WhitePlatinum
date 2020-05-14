using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace WhitePlatinumLib {
	public static class Log {
		public static void Write(string Text) {
#if DEBUG
			Debug.Write(Text);
#endif
		}

		public static void Write(string Fmt, params object[] Args) {
			Write(string.Format(Fmt, Args));
		}

		public static void WriteLine(string Text) {
			Write(Text + "\n");
		}

		public static void WriteLine(string Fmt, params object[] Args) {
			WriteLine(string.Format(Fmt, Args));
		}
	}
}