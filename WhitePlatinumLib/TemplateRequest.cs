using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace WhitePlatinumLib {
	[DataContract]
	public class TemplateRequest {
		[DataMember(Name = "savemethod")]
		public string SaveMethod;

		[DataMember(Name = "path")]
		public string Path;

		[DataMember(Name = "overwrite")]
		public bool Overwrite;


		[DataMember(Name = "template")]
		public FileEntry Template;

		[DataMember(Name = "data")]
		public FileEntry Data;

		public TemplateRequest() {
			SaveMethod = "";
			Path = "";
			Overwrite = false;
		}

		public TemplateRequest(FileEntry Template, FileEntry Data) : this() {
			this.Template = Template;
			this.Data = Data;
		}

		public bool ShouldSerializeSaveMethod() {
			return !string.IsNullOrEmpty(SaveMethod);
		}

		public bool ShouldSerializePath() {
			return !string.IsNullOrEmpty(Path);
		}

		public bool ShouldSerializeOverwrite() {
			return Overwrite;
		}
	}
}