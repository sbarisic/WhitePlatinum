using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace WhitePlatinumLib {
	[DataContract]
	public class ErrorEntry {
		[DataContract]
		public class ExceptionPart {
			[DataMember(Name = "stacktrace")]
			public string StackTrace;

			[DataMember(Name = "methodname")]
			public string MethodName;

			[DataMember(Name = "modulename")]
			public string ModuleName;

			[DataMember(Name = "message")]
			public string Message;

			[DataMember(Name = "hresult")]
			public int HResult;

			[DataMember(Name = "hresulthex")]
			public string HResultHex;

			[DataMember(Name = "exception")]
			public ExceptionPart Exception;

			public ExceptionPart() {
			}

			public ExceptionPart(Exception E, int Depth = 0) {
				E = E ?? new Exception();

				StackTrace = E.StackTrace ?? "";
				MethodName = E.TargetSite?.Name ?? "";
				ModuleName = E.TargetSite?.Module?.Name ?? "";
				Message = E.Message ?? "";

				HResult = E.HResult;
				HResultHex = string.Format("0x{0}", HResult.ToString("X8"));

				if (Depth < 5 && E.InnerException != null)
					Exception = new ExceptionPart(E.InnerException, Depth + 1);
			}

			public bool ShouldSerializeStackTrace() {
				return !string.IsNullOrEmpty(StackTrace);
			}

			public bool ShouldSerializeMethodName() {
				return !string.IsNullOrEmpty(MethodName);
			}

			public bool ShouldSerializeModuleName() {
				return !string.IsNullOrEmpty(ModuleName);
			}

			public bool ShouldSerializeMessage() {
				return !string.IsNullOrEmpty(Message);
			}

			public bool ShouldSerializeException() {
				return Exception != null;
			}
		}

		[DataMember(Name = "exception")]
		public ExceptionPart Exception;
		
		[DataMember(Name = "request")]
		public TemplateRequest Request;

		public ErrorEntry() {
		}

		public ErrorEntry(Exception E, TemplateRequest Request) : this() {
			this.Request = Request;
			Exception = new ExceptionPart(E);
		}
	}
}