using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WhitePlatinumLib {
	public class TokenParameters {
		List<TokenParameter> Params;

		public TokenParameters() {
			Params = new List<TokenParameter>();
		}

		public TokenParameters(string ParamsString) : this() {
			ParamsString = Utils.FixQuotes(ParamsString);

			string[] KeyValueStrings = ParamsString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var KV in KeyValueStrings) {
				if (KV.Contains("=")) {
					string[] KVTokens = KV.Split(new[] { '=' });

					if (KVTokens.Length > 2)
						throw new Exception(string.Format("Token parameter string malformed ({0})", ParamsString));

					Params.Add(new TokenParameter(KVTokens[0].Trim(), KVTokens[1].Trim()));
				} else
					Params.Add(new TokenParameter(KV.Trim(), "true"));
			}

			// Last defined item takes precedence
			Params.Reverse();
		}

		public T Get<T>(string Key, T Default = default(T)) {
			TokenParameter Param = null;

			foreach (var P in Params)
				if (P.Key == Key) {
					Param = P;
					break;
				}

			if (Param == null)
				return Default;

			if (typeof(T) == typeof(int))
				return (T)(object)int.Parse(Param.Value);
			else if (typeof(T) == typeof(bool))
				return (T)(object)bool.Parse(Param.Value);
			else if (typeof(T) == typeof(string)) {
				string Ret = Param.Value.Trim();
				Ret = Ret.Substring(1, Ret.Length - 2);
				return (T)(object)Ret;
			}

			throw new NotImplementedException(string.Format("TokenParameter Get<{0}> not implemented", typeof(T).Name));
		}

		public bool Defined(string Key) {
			foreach (var P in Params)
				if (P.Key == Key)
					return true;

			return false;
		}
	}

	public class TokenParameter {
		public string Key;
		public string Value;

		public TokenParameter(string Key, string Value) {
			this.Key = Key;
			this.Value = Value;
		}
	}
}