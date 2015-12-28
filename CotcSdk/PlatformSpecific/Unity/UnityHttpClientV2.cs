using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Networking;

namespace CotcSdk {
	/**
	 * Better HTTP client based on UnityEngine.Experimental.Networking.UnityWebRequest.
	 */
	internal class UnityHttpClientV2 : HttpClient {
		private int RequestCount = 0;

		/// <summary>Asynchronous request state.</summary>
		private class UnityRequest : WebRequest {
			// This class stores the State of the request.
			private string ContentType;
			public UnityHttpClientV2 self;
			private UnityWebRequest Request;
			private bool WasAborted = false;

			public UnityRequest(UnityHttpClientV2 inst, string url, HttpRequest request, object previousUserData, int requestId) {
				self = inst;
				OriginalRequest = request;
				RequestId = requestId;
				PreviousUserData = previousUserData;

				Request = new UnityWebRequest(url);
				// Auto-choose HTTP method
				Request.method = request.Method ?? (request.Body != null ? "POST" : "GET");
				// TODO Missing functionality (currently unsupported by UnityWebRequest).
//				req.SetRequestHeader("User-agent", request.UserAgent);
//				req.keepAlive = true;
				foreach (var pair in request.Headers) {
					Request.SetRequestHeader(pair.Key, pair.Value);
				}

				if (OriginalRequest.Body != null) {
					UploadHandler uploader = new UploadHandlerRaw(OriginalRequest.Body);
					if (ContentType != null) uploader.contentType = ContentType;
					Request.uploadHandler = uploader;
				}
				Request.downloadHandler = new DownloadHandlerBuffer();
			}

			public override void AbortRequest() {
				WasAborted = true;
				if (Request != null) {
					Request.Abort();
				}
			}

			/// <summary>Prints the current request for user convenience.</summary>
			internal void LogRequest() {
				if (!self.VerboseMode) { return; }

				StringBuilder sb = new StringBuilder();
				sb.AppendLine("[" + RequestId + "] " + Request.method + "ing on " + Request.url);
				sb.AppendLine("Headers:");
				foreach (var pair in OriginalRequest.Headers) {
					sb.AppendLine("\t" + pair.Key + ": " + pair.Value);
				}
				if (OriginalRequest.HasBody) {
					sb.AppendLine("Body: " + OriginalRequest.BodyString);
				}
				Common.Log(sb.ToString());
			}

			/// <summary>Prints information about the response for debugging purposes.</summary>
			internal void LogResponse(HttpResponse response) {
				if (!self.VerboseMode) { return; }

				StringBuilder sb = new StringBuilder();
				sb.AppendLine("[" + RequestId + "] " + response.StatusCode + " on " + Request.method + "ed on " + Request.url);
				sb.AppendLine("Recv. headers:");
				foreach (var pair in response.Headers) {
					if (String.Compare(pair.Key, "Content-Type", true) == 0) {
						ContentType = pair.Value;
					}
					else {
						sb.AppendLine("\t" + pair.Key + ": " + pair.Value);
					}
				}
				if (response.HasBody) {
					sb.AppendLine("Body: " + response.BodyString);
				}
				Common.Log(sb.ToString());
			}

			private IEnumerator ProcessRequest(UnityWebRequest req) {
				yield return req.Send();

				if (req.isError) {
					if (!WasAborted) {
						string errorMessage = "Failed web request: " + req.error;
						Common.Log(errorMessage);
						self.FinishWithRequest(this, new HttpResponse(new Exception(errorMessage)));
					}
				}
				else {
					// Extracts asset bundle
					HttpResponse response = new HttpResponse();
					response.Body = Request.downloadHandler.data;
					response.StatusCode = (int)Request.responseCode;
					foreach (var pair in Request.GetResponseHeaders()) {
						response.Headers[pair.Key] = pair.Value;
					}
					LogResponse(response);
					self.FinishWithRequest(this, response);
				}
			}

			public override void Start() {
				// Configure & perform the request
				LogRequest();
				Cotc.RunCoroutine(ProcessRequest(Request));
			}
		}

		protected override WebRequest CreateRequest(HttpRequest request, string url, object previousUserData) {
			return new UnityRequest(this, url, request, previousUserData, (RequestCount += 1));
		}
	}
}
