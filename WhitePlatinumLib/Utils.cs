﻿using DocumentFormat.OpenXml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Security;
using System.Web;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace WhitePlatinumLib {
	public static class Utils {
		static string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DocumentEngine");
		static string TemplateFolder = Path.Combine(AppDataFolder, "Templates");

		public static string FixQuotes(string Str) {
			return Str.Replace("“", "\"").Replace("”", "\"");
		}

		public static byte[] GetAppdataFileBytes(string Name) {
			if (!Directory.Exists(AppDataFolder))
				Directory.CreateDirectory(AppDataFolder);

			return File.ReadAllBytes(Path.Combine(AppDataFolder, Name));
		}

		public static byte[] GetTemplateBytes(string TemplateName) {
			if (!Directory.Exists(TemplateFolder))
				Directory.CreateDirectory(TemplateFolder);

			//System.Diagnostics.Process.Start("explorer.exe", Path.GetFullPath("."));

			return File.ReadAllBytes(Path.Combine(TemplateFolder, TemplateName));
		}

		public static byte[] DownloadRaw(string URL) {
			using (WebClient WClient = new WebClient()) {
				return WClient.DownloadData(URL);
			}
		}

		public static byte[] DownloadRaw(Uri Uri) {
			return DownloadRaw(Uri.AbsoluteUri);
		}

		public static void GetSizeInMilimeter(Image Img, out float Width, out float Height) {
			const float MMPerInch = 25.4f;

			Width = (Img.Width / Img.HorizontalResolution) * MMPerInch;
			Height = (Img.Height / Img.VerticalResolution) * MMPerInch;
		}

		public static void GetSizeInEMU(Image Img, out long Width, out long Height) {
			GetSizeInMilimeter(Img, out float W, out float H);
			Width = MilimeterToEmu(W);
			Height = MilimeterToEmu(H);
		}

		public static long MilimeterToEmu(float Size) {
			return (long)(Size * 36000);
		}

		public static void Scale(float Scale, ref long X, ref long Y) {
			X = (long)(X * Scale);
			Y = (long)(Y * Scale);
		}

		public static T Copy<T>(this T Node) where T : OpenXmlElement {
			return (T)Node?.CloneNode(true);
		}

		public static bool HasChild<T>(this OpenXmlElement Node) where T : OpenXmlElement {
			return Node.GetFirstChild<T>() != null;
		}

		// TODO: This is unsafe? Find a better alternative
		public static SecureString CreateSecureString(string Str) {
			SecureString SS = new SecureString();

			foreach (var C in Str)
				SS.AppendChar(C);

			return SS;
		}

		public static string GetStringWithoutScheme(Uri URI) {
			string Str = URI.OriginalString.Substring(URI.Scheme.Length + 3);
			return Str;
		}

		static JsonSerializer Serializer;

		public static string ToJSON(object Obj, bool Pretty = false) {
			if (Serializer == null) {
				Serializer = new JsonSerializer();
			}

			using (MemoryStream MS = new MemoryStream()) {
				using (StreamWriter SW = new StreamWriter(MS, Encoding.UTF8, 64, true))
				using (JsonWriter Writer = new JsonTextWriter(SW) { Formatting = Pretty ? Formatting.Indented : Formatting.None })
					Serializer.Serialize(Writer, Obj);

				MS.Seek(0, SeekOrigin.Begin);
				return Encoding.UTF8.GetString(MS.ToArray());
			}
		}

		public static string JsonPrettify(string Json) {
			using (var StrReader = new StringReader(Json))
			using (var StrWriter = new StringWriter()) {
				JsonTextReader JsonReader = new JsonTextReader(StrReader);
				JsonTextWriter JsonWriter = new JsonTextWriter(StrWriter) { Formatting = Formatting.Indented };
				JsonWriter.WriteToken(JsonReader);
				return StrWriter.ToString();
			}
		}

		public static byte[] JsonPrettify(byte[] Json) {
			return Encoding.UTF8.GetBytes(JsonPrettify(Encoding.UTF8.GetString(Json)));
		}
	}
}