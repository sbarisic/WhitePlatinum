using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace WhitePlatinumLib.TemplateProcessing {
	public enum DataType {
		None,
		Object,
		String,
		Array,
		EmbeddedData,
		PageBreak,

		Hook_Skip,
	}

	public delegate DataType SpecialObjectHookFunc(string Token, out object Obj);

	public class DataSet {
		JObject Data;
		SpecialObjectHookFunc SpecialObjectHook;

		public DataSet(string DataJSON) {
			Data = (JObject)JsonConvert.DeserializeObject(DataJSON);
		}

		public void UseSpecialObjectHook(SpecialObjectHookFunc Func, Action Act) {
			SpecialObjectHookFunc Old = SpecialObjectHook;
			SpecialObjectHook = Func;
			Act();
			SpecialObjectHook = Old;
		}

		public void UseRootObject(JObject Data, Action Act) {
			JObject Old = this.Data;
			this.Data = Data;
			Act();
			this.Data = Old;
		}

		DataType GetSpecialObject(string Token, out object Obj) {
			switch (Token) {
				case "$DATE":
					Obj = DateTime.Now.ToString("dd.MM.yyyy.", CultureInfo.InvariantCulture);
					return DataType.String;

				case "$TIME":
					Obj = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
					return DataType.String;

				case "$PAGE_BREAK":
					Obj = "";
					return DataType.PageBreak;
			}

			if (SpecialObjectHook != null) {
				DataType Ret = SpecialObjectHook(Token, out Obj);

				if (Ret != DataType.Hook_Skip)
					return Ret;
			}

			throw new Exception(string.Format("Invalid special object token '{0}'", Token));
		}

		public DataType GetObject(string Token, TokenParameters TokenParams, out object Obj) {
			string[] FullName = Token.Split('.');
			Obj = Data;

			if (FullName.Length == 1 & FullName[0].StartsWith("$"))
				return GetSpecialObject(FullName[0], out Obj);

			for (int i = 0; i < FullName.Length; i++) {
				if (FullName[i].StartsWith("$")) {
					WordTable CurTable = WordTableContext.GetCurrent();
					string TableToken = string.Join(".", FullName.Take(i).ToArray());

					WordTable.Column Col = CurTable.GetCurrentColumn();
					Col.Identifier = FullName[i].Substring(1);
					//Col.RefArrayToken = TableToken;


					DataType TableTokenDataType = GetObject(TableToken, TokenParams, out object _Obj);
					if (_Obj is JArray TableObj) {
						JToken ColumnDefinition = TableObj[0][Col.Identifier];

						Col.RefArray = TableObj;
						Col.Text = ColumnDefinition["text"].Value<string>();
						Col.Style = ColumnDefinition["style"].Value<string>();
					} else
						throw new Exception(string.Format("Expected array, got '{0}' for token '{1}'", TableTokenDataType, TableToken));

					Obj = Col.Text;
					return DataType.String;
				}

				if (Obj is JObject JRoot) {
					JToken JToken = JRoot.GetValue(FullName[i]);

					if (JToken == null)
						Obj = null;
					else if (JToken is JValue JValue)
						Obj = JValue.Value;
					else if (JToken is JObject)
						Obj = JToken.Value<JObject>();
					else if (JToken is JArray)
						Obj = JToken.Value<JArray>();
					else
						throw new NotImplementedException();

				} else {
					string Cur = string.Join(".", FullName.Take(i).ToArray());
					throw new Exception(string.Format("Could not find value for token '{0}', got '{1}' of value '{2}'", Token, Cur, Obj));
				}
			}

			if (Obj == null) {
				if (TokenParams != null && TokenParams.Defined("null")) {
					Obj = TokenParams.Get<string>("null");
					return DataType.String;
				}

				return DataType.None;
			} else if (Obj is JObject JObject) {
				if (JObject.TryGetValue("__type", out JToken TypeToken) && JObject.TryGetValue("__data", out JToken DataToken)) {
					Obj = new EmbeddedData(TypeToken, DataToken);
					return DataType.EmbeddedData;
				}

				return DataType.Object;
			} else if (Obj is JArray)
				return DataType.Array;
			else if (Obj is string RootStr)
				return DataType.String;
			else
				throw new NotImplementedException();
		}

		public DataType GetObjectType(string Token) {
			return GetObject(Token, null, out object Obj);
		}

		public string GetString(string Token) {
			DataType ObjType = DataType.None;

			if ((ObjType = GetObject(Token, null, out object Obj)) != DataType.String)
				throw new Exception(string.Format("Expected String, got {0} for '{1}", ObjType, Token));

			return (string)Obj;
		}

		public object[] GetArray(string Token) {
			DataType ObjType = DataType.None;

			if ((ObjType = GetObject(Token, null, out object Obj)) != DataType.Array)
				throw new Exception(string.Format("Expected Array, got {0} for '{1}", ObjType, Token));



			throw new NotImplementedException();
		}
	}
}