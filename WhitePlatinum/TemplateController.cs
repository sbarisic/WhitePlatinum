﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WhitePlatinumLib;
using WhitePlatinumLib.Sharepoint;
using WhitePlatinumLib.TemplateProcessing;

namespace WhitePlatinum {
	public class SharepointTemplateResponse {
		public string Message;
	}

	//[Route("api/[controller]")]
	public class TemplateController : Controller {
		[HttpGet("api/test")]
		public string Test() {
			return "It works!";
		}

		// POST api/sharepointtemplate
		[HttpPost("api/sharepointtemplate")]
		public SharepointTemplateResponse SharepointTemplate() {
			string Req = "";

			using (StreamReader Reader = new StreamReader(Request.Body, Encoding.UTF8))
				Req = Reader.ReadToEnd();

			DEBUG.WriteLine(Req);
			return new SharepointTemplateResponse() { Message = "Success, request saved" };
		}

		// POST api/template
		[HttpPost("api/template")]
		public TemplateResponse Post([FromBody]TemplateRequest Request) {
			const bool DEBUG_EXCEPTIONS = false;
			SharepointAPI Sharepoint = null;

			try {
				byte[] TemplateData = null;

				if (string.IsNullOrWhiteSpace(Request.Template.Content)) {
					if (string.IsNullOrWhiteSpace(Request.Template.Name))
						throw new Exception("Request template content and name empty");

					if (Uri.TryCreate(Request.Template.Name, UriKind.Absolute, out Uri Result)) {
						switch (Result.Scheme) {
							case "http":
							case "https":
								TemplateData = Utils.DownloadRaw(Result);
								break;

							case "sharepoint": {
									if (Sharepoint == null)
										Sharepoint = InitSharepoint();

									TemplateData = Sharepoint.OpenBinaryDirectArray("/" + Utils.GetStringWithoutScheme(Result));
									break;
								}

							default:
								throw new NotImplementedException(string.Format("URI scheme '{0}' not implemented for '{1}'", Result.Scheme, Request.Template.Name));
						}
					} else
						throw new NotImplementedException(string.Format("Empty template content for name '{0}' not implemented", Request.Template.Name));
				} else
					TemplateData = Request.Template.ContentBytes;

				DataSet Data = new DataSet(Encoding.UTF8.GetString(Request.Data.ContentBytes));
				WordProcessor Processor = new WordProcessor(Data);

				byte[] ProcessedFile = Processor.Process(TemplateData);
				FileEntry ResponseFile = null;

				switch (string.IsNullOrEmpty(Request.SaveMethod) ? "base64" : Request.SaveMethod) {
					case "base64":
						ResponseFile = new FileEntry(string.IsNullOrEmpty(Request.Path) ? "Response.docx" : Request.Path, ProcessedFile);
						break;

					case "sharepoint": {
							if (Sharepoint == null)
								Sharepoint = InitSharepoint();

							string Path = Request.Path;
							if (string.IsNullOrEmpty(Path))
								throw new Exception(string.Format("Sharepoint save path cannot be null or empty '{0}'", Path));

							Sharepoint.SaveBinaryDirect(Path, ProcessedFile, Request.Overwrite);
							ResponseFile = new FileEntry(Path, "");
						}
						break;

					default:
						throw new NotImplementedException(string.Format("Unknown response file save method '{0}'", Request.SaveMethod));
				}


				return new TemplateResponse(ResponseFile);
			} catch (Exception E) when (!(Debugger.IsAttached && DEBUG_EXCEPTIONS)) {
				return new TemplateResponse(new ErrorEntry(E, Request));
			} finally {
				Sharepoint?.Dispose();
			}
		}

		static SharepointAPI InitSharepoint() {
			/*string SpUsername = ConfigurationManager.AppSettings["sharepoint_username"];
			System.Security.SecureString SpPassword = Utils.CreateSecureString(ConfigurationManager.AppSettings["sharepoint_password"]);
			return new SharepointAPI(SpUsername, SpPassword);*/

			return new SharepointAPI("todo", Utils.CreateSecureString("todo"));
		}

	}
}
