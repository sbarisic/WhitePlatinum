using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace WhitePlatinumLib {
	[DataContract]
	public class FileEntry {
		[DataMember(Name = "name")]
		public string Name;

		[DataMember(Name = "content")]
		public string Content;

		[IgnoreDataMember]
		public byte[] ContentBytes {
			get {
				return Convert.FromBase64String(Content);
			}
		}

		public FileEntry() {
			this.Name = "";
			this.Content = "";
		}

		public FileEntry(string Name, string Content) {
			this.Name = Name;
			this.Content = Content;
		}

		public FileEntry(string Name, byte[] Content) : this(Name, Convert.ToBase64String(Content)) {
		}

		public FileEntry(string FileName) : this(Path.GetFileName(FileName), File.ReadAllBytes(FileName)) { }
	}
}