using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WhitePlatinum {
	public static class DEBUG {
#if DEBUG
		static string LogLocation = "C:/Projekti/WhitePlatinum/bin/DocumentEngineLog.txt";
#else
		static string LogLocation = "C:/inetpub/wwwroot/DocumentEngine/logs/DocumentEngineLog.txt";
#endif

		public static void Write(string Str) {
			try {
				File.AppendAllText(LogLocation, Str);
			} catch (Exception) {
			}
		}

		public static void WriteLine(string Str) {
			Write(Str + "\r\n");
		}
	}
}
