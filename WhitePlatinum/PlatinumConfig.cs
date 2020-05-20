using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;

namespace WhitePlatinum {
	public class PlatinumConfig {
		public PlatinumConfig(IConfigurationSection Section) {
			FieldInfo[] Fields = GetType().GetFields();

			foreach (var F in Fields)
				F.SetValue(this, Section.GetValue(F.FieldType, F.Name));
		}

		public bool EnableLogging;
		public bool DumpLastRequestJson;
		public bool DumpLastDataJson;
		public bool DumpLastResponseFile;
	}
}
