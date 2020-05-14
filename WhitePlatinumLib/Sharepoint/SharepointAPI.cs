using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security;
using System.Web;
//using Microsoft.SharePoint.Client;
//using SharepointFile = Microsoft.SharePoint.Client.File;
//using SharepointFileInfo = Microsoft.SharePoint.Client.FileInformation;

namespace WhitePlatinumLib.Sharepoint {
	public class SharepointAPI : IDisposable {
		//ClientContext Ctx;

		public SharepointAPI(string Username, SecureString Password) {
			//Ctx = new ClientContext("https://serengetitech.sharepoint.com");
			//Ctx.Credentials = new SharePointOnlineCredentials(Username, Password);
			//Ctx.ExecuteQuery();
		}

		//public SharepointAPI() : this(ConfigurationManager.AppSettings["sharepoint_username"], Utils.CreateSecureString(ConfigurationManager.AppSettings["sharepoint_password"])) {
		//}

		public void SaveBinaryDirect(string ServerRelativeURL, Stream Data, bool Overwrite = true) {
			//Data.Seek(0, SeekOrigin.Begin);
			//SharepointFile.SaveBinaryDirect(Ctx, ServerRelativeURL, Data, Overwrite);
			throw new NotImplementedException();
		}

		public void SaveBinaryDirect(string ServerRelativeURL, byte[] Data, bool Overwrite = true) {
			using (MemoryStream MS = new MemoryStream(Data)) {
				SaveBinaryDirect(ServerRelativeURL, MS, Overwrite);
			}
		}

		public MemoryStream OpenBinaryDirectStream(string ServerRelativeURL) {
			/*SharepointFileInfo FInfo = SharepointFile.OpenBinaryDirect(Ctx, ServerRelativeURL);
			MemoryStream MS = new MemoryStream();
			FInfo.Stream.CopyTo(MS);
			MS.Seek(0, SeekOrigin.Begin);
			return MS;*/
			throw new NotImplementedException();
		}

		public byte[] OpenBinaryDirectArray(string ServerRelativeURL) {
			using (MemoryStream MS = OpenBinaryDirectStream(ServerRelativeURL))
				return MS.ToArray();
		}

		public void Dispose() {
			//Ctx.Dispose();
		}
	}
}