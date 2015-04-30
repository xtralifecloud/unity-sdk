using System;
using System.Collections.Generic;
using System.Text;
using LitJson;
using System.Collections;

namespace CloudBuilderLibrary
{
	public class CloudResult {
		public Bundle Data {
			get { return Json; }
		}
		public ErrorCode ErrorCode;
		public string ErrorInformation;
		public int HttpStatusCode;

		internal CloudResult(HttpResponse response) {
			Json = response.BodyJson;
			HttpStatusCode = response.StatusCode;
			if (response.HasFailed) {
				ErrorCode = ErrorCode.enNetworkError;
			}
			else if (response.StatusCode < 200 || response.StatusCode >= 300) {
				ErrorCode = ErrorCode.enServerError;
			}
			else {
				ErrorCode = ErrorCode.enNoErr;
			}
		}
		internal CloudResult(ErrorCode code, string description = null) {
			ErrorCode = code;
			ErrorInformation = description;
		}

		public override string ToString() {
			string start = String.Format("[CloudResult error={0}", ErrorCode);
			if (ErrorCode != ErrorCode.enNoErr) {
				start += String.Format(" ({0})", ErrorInformation ?? ErrorCode.Description());
			}
			if (HttpStatusCode != 0) {
				start += String.Format(" http={0}", HttpStatusCode);
			}
			if (Data != null) {
				start += String.Format(" data={0}", Data.ToJson());
			}
			return start + "]";
		}

		private Bundle Json;
    }
}
