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
using WhitePlatinumLib.TemplateProcessing;
using DataType = WhitePlatinumLib.TemplateProcessing.DataType;
using Newtonsoft.Json.Linq;

namespace WhitePlatinumLib {
	public class Scripting {
		ScriptOptions Options;

		DataSet Data;
		WordProcessor Processor;

		public int NodeIndex;
		public int NodeCount;

		public Scripting(DataSet Data, WordProcessor Processor) {
			this.Data = Data;
			this.Processor = Processor;

			InTemplateChunk = false;

			Options = ScriptOptions.Default
				.WithReferences(typeof(Scripting).Assembly)
				.WithImports("System")
				.WithImports("WhitePlatinumLib")
				;
		}

		public object Eval(string Src) {
			Src = Utils.FixQuotes(Src);
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

		DataType GetSpecialObject(string Token, out object Obj) {
			switch (Token) {
				case "$NodeIndex":
					Obj = (NodeIndex + 1).ToString();
					return DataType.String;

				case "$NodeCount":
					Obj = NodeCount.ToString();
					return DataType.String;
			}

			Obj = null;
			return DataType.Hook_Skip;
		}

		public void TemplateChunkGenerate(string ArrayName) {
			if (InTemplateChunk)
				throw new Exception("Exit template chunk before generating");

			if (Data.GetObject(ArrayName, null, out object Obj) != DataType.Array)
				throw new Exception(string.Format("Object '{0}' is not array", ArrayName));

			JArray ObjArray = (JArray)Obj;
			int Length = NodeCount = ObjArray.Count;

			Data.UseSpecialObjectHook(GetSpecialObject, () => {
				for (int i = 0; i < Length; i++) {
					NodeIndex = i;

					JObject IndexedObj = (JObject)ObjArray[i];
					Data.UseRootObject(IndexedObj, () => {
						foreach (var El in TemplateChunk) {
							OpenXmlElement Clone = (OpenXmlElement)El.Clone();
							Clone = TemplateChunkBody.AppendChild(Clone);

							Processor.Process(TemplateChunkBody, Clone);
						}
					});
				}
			});
		}
	}
}
