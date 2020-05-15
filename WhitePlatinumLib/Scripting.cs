using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis;
using System.Threading;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WhitePlatinumLib {
	public class Scripting {
		ScriptOptions Options;

		public Scripting() {
			InTemplateChunk = false;

			Options = ScriptOptions.Default
				.WithReferences(typeof(Scripting).Assembly)
				.WithImports("System")
				.WithImports("WhitePlatinumLib")
				;
		}

		public object Eval(string Src) {
			Src = Src.Replace("“", "\"").Replace("”", "\"");

			return CSharpScript.EvaluateAsync(Src, Options, this).Result;
		}

		bool InTemplateChunk;
		Body TemplateChunkBody;
		List<OpenXmlElement> TemplateChunk = new List<OpenXmlElement>();

		public bool CaptureElement(Body Body, OpenXmlElement E) {
			if (!InTemplateChunk)
				return false;

			TemplateChunkBody = Body;
			TemplateChunk.Add(E);
			return true;
		}

		public void TemplateChunkBegin() {
			if (InTemplateChunk)
				throw new Exception("Already inside template chunk");

			TemplateChunk.Clear();
			InTemplateChunk = true;
		}

		public void TemplateChunkEnd() {
			if (!InTemplateChunk)
				throw new Exception("Already outside template chunk");

			InTemplateChunk = false;
		}

		public void TemplateChunkGenerate(string ArrayName) {
			if (InTemplateChunk)
				throw new Exception("Exit template chunk before generating");

			foreach (var El in TemplateChunk) {
				OpenXmlElement Clone = (OpenXmlElement)El.Clone();
				TemplateChunkBody.AppendChild(Clone);
			}
		}
	}
}
