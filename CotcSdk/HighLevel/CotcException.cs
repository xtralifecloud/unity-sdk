using System;

namespace CotcSdk {

	public class CotcException : Exception {
		public ErrorCode ErrorCode;
		public string ErrorInformation;
		public int HttpStatusCode;
		public Bundle ServerData {
			get { return Json; }
		}

		/// <summary>To be used for an higher level error. No information about the HTTP request would be attached.</summary>
		public CotcException(ErrorCode code, string failureDescription = null) {
			ErrorCode = code;
			ErrorInformation = failureDescription;
		}
		/// <summary>To be used when an HTTP request has failed. Will extract a default error code (server error, network error) from the HTTP request.</summary>
		internal CotcException(HttpResponse response, string failureDescription = null) {
			Json = response.BodyJson;
			HttpStatusCode = response.StatusCode;
			if (response.HasFailed) {
				ErrorCode = ErrorCode.NetworkError;
				ErrorInformation = failureDescription;
			}
			else if (response.StatusCode < 200 || response.StatusCode >= 300) {
				ErrorCode = ErrorCode.ServerError;
				ErrorInformation = failureDescription;
			}
			else {
				throw new InvalidOperationException("Should only call this for a failed request");
			}
		}

		public override string ToString() {
			string start = String.Format("[CotcException error={0} ({1})", ErrorCode, ErrorInformation ?? ErrorCode.Description());
			if (HttpStatusCode != 0) {
				start += String.Format(" http={0}", HttpStatusCode);
			}
			if (ServerData != null) {
				start += String.Format(" data={0}", ServerData.ToJson());
			}
			return start + "]";
		}

		private Bundle Json;
	}
}
