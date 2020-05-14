using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Web;

namespace WhitePlatinumLib {
	[DataContract]
	public class TemplateResponse {
		[DataMember(Name = "response")]
		public FileEntry Response;

		[DataMember(Name = "error")]
		public ErrorEntry Error;

		public TemplateResponse() {
		}

		public TemplateResponse(FileEntry Response) {
			this.Response = Response;

			Error = null;
			//this.Error = new FileEntry();
		}

		public TemplateResponse(ErrorEntry Error) {
			//this.Response = new FileEntry();

			this.Error = Error;
			Response = null;

			/*string ErrorJson = JsonConvert.SerializeObject(Error, Formatting.Indented);
			this.Error = new FileEntry("error.json", Convert.ToBase64String(Encoding.UTF8.GetBytes(ErrorJson)));*/
		}

		public bool ShouldSerializeResponse() {
			return Response != null;
		}

		public bool ShouldSerializeError() {
			return Error != null;
		}
	}
}