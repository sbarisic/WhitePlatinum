﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using WhitePlatinumLib;
using WhitePlatinumLib.Sharepoint;
using WhitePlatinumLib.TemplateProcessing;

namespace WhitePlatinum {
	public class SharepointTemplateResponse {
		public string Message;
	}

	[Produces("application/json")]
	public class TemplateController : Controller {
		PlatinumConfig PlatinumCfg;

		public TemplateController([FromServices] IConfiguration Config) {
			PlatinumCfg = new PlatinumConfig(Config.GetSection(nameof(PlatinumConfig)));
		}

		void SetupResponseHeaders() {
			Response.Headers.Add("Content-Type", "application/json; charset=utf-8");
			Response.StatusCode = 200;
		}

		[EnableCors]
		[HttpGet("api/test")]
		public string Test() {
			SetupResponseHeaders();
			return "It works!";
		}

		// POST api/sharepointtemplate
		/*[EnableCors]
		[HttpPost("api/sharepointtemplate")]
		public TemplateResponse SharepointTemplate([FromBody]TemplateRequest TemplateRequest) {
			//SetupResponseHeaders();

			DEBUG.WriteLine(Utils.ToJSON(TemplateRequest));
			return Post(TemplateRequest);
		}*/

		[HttpOptions]
		public void Options() {
		}

		// POST api/template
		//[HttpPost("api/template")]
		[EnableCors]
		[HttpPost("api/sharepointtemplate")]
		public TemplateResponse Post([FromBody]TemplateRequest Request) {
			if (PlatinumCfg.EnableLogging)
				DEBUG.WriteLine(DateTime.Now.ToString("[dd.MM.yyyy. HH:mm:ss]") + " " + Utils.ToJSON(Request).Trim());

			if (PlatinumCfg.DumpLastRequestJson)
				DEBUG.WriteBytes("request.json", Encoding.UTF8.GetBytes(Utils.ToJSON(Request, true)));

			if (PlatinumCfg.DumpLastDataJson)
				DEBUG.WriteBytes("data.json", Utils.JsonPrettify(Convert.FromBase64String(Request.Data.Content)));

			SetupResponseHeaders();
			const bool DEBUG_EXCEPTIONS = true;
			SharepointAPI Sharepoint = null;

			try {
				byte[] TemplateData = null;

				if (Request == null)
					throw new Exception("Request was null");

				if (Request.Template == null)
					throw new Exception("Request.Template was null");

				if (string.IsNullOrWhiteSpace(Request.Template.Content)) {
					if (string.IsNullOrWhiteSpace(Request.Template.Name))
						throw new Exception("Request template content and name empty");

					if (Uri.TryCreate(Request.Template.Name, UriKind.Absolute, out Uri Result)) {
						string PathWithoutScheme = Result.OriginalString.Substring(Result.Scheme.Length + 3);

						switch (Result.Scheme) {
							case "http":
							case "https":
								TemplateData = Utils.DownloadRaw(Result);
								break;

							case "template":
								TemplateData = Utils.GetTemplateBytes(PathWithoutScheme);
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

				if (PlatinumCfg.DumpLastResponseFile)
					DEBUG.WriteBytes("response.docx", ProcessedFile);

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
