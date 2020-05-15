using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WhitePlatinum {
	public static class DEBUG {
#if DEBUG
		static string DebugLocation = "C:/Projekti/WhitePlatinum/bin/";
#else
		static string DebugLocation = "C:/inetpub/wwwroot/DocumentEngine/logs/";
#endif

		public static void Write(string Str) {
			if (!Directory.Exists(DebugLocation))
				Directory.CreateDirectory(DebugLocation);

			File.AppendAllText(Path.Combine(DebugLocation, "DocumentEngineLog.txt"), Str);
		}

		public static void WriteLine(string Str) {
			Write(Str + "\r\n");
		}

		public static void WriteBytes(string FileName, byte[] Bytes) {
			if (!Directory.Exists(DebugLocation))
				Directory.CreateDirectory(DebugLocation);

			FileName = Path.Combine(DebugLocation, FileName);
			File.WriteAllBytes(FileName, Bytes);
		}
	}
}
