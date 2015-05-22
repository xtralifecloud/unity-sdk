using System;
using System.Collections.Generic;
using System.Text;
using LitJson;
using System.Collections;

namespace CloudBuilderLibrary {
	public class Result<T> {
		public ErrorCode ErrorCode;
		public string ErrorInformation;
		public bool IsSuccessful {
			get { return ErrorCode == ErrorCode.Ok; }
		}
		public int HttpStatusCode;
		public Bundle ServerData {
			get { return Json; }
		}
		public T Value;

		/**
		 * To be used when an HTTP request has failed. Will extract a default error code (server error, network error) from the HTTP request.
		 */
		internal Result(HttpResponse response, string failureDescription = null) {
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
				ErrorCode = ErrorCode.Ok;
			}
		}
		/**
		 * To be used for an higher level error. No information about the HTTP request would be attached.
		 */
		internal Result(ErrorCode code, string failureDescription = null) {
			ErrorCode = code;
			ErrorInformation = failureDescription;
		}
		/**
		 * To be used when successful. The "no error" code will be attached and the result forwarded. The serverData can be attached (response.BodyJson) for more information.
		 */
		internal Result(T value, Bundle serverData) {
			ErrorCode = ErrorCode.Ok;
			Json = serverData;
			Value = value;
		}

		public override string ToString() {
			string start = String.Format("[CloudResult error={0}", ErrorCode);
			if (ErrorCode != ErrorCode.Ok) {
				start += String.Format(" ({0})", ErrorInformation ?? ErrorCode.Description());
			}
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
