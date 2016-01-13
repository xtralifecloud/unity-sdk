using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace CotcSdk
{
	/**
	 * Former Unity HTTP client, based on the standard .NET framework classes (HttpWebRequest).
	 * 
	 * Since this class is not really supported (compared to the WWW class which is supposed to be the standard
	 * but unsufficient), it causes a lot of problems on some platforms and we're happy to get rid of it.
	 */
	internal class MonoHttpClient : HttpClient {
		private const int ConcurrentHttpRequestLimit = 100;

		/// <summary>Asynchronous request state.</summary>
		private class RequestState: WebRequest {
			// This class stores the State of the request.
			public const int BufferSize = 1024;
			public MemoryStream ResponseBuffer;
			public byte[] BufferRead;
			public HttpWebRequest Request;
			public HttpWebResponse Response;
			public Stream StreamResponse;
			private MonoHttpClient self;

			public RequestState(MonoHttpClient inst, HttpRequest originalReq, HttpWebRequest req, object previousUserData, int requestId) : base(inst, originalReq) {
				self = inst;
				BufferRead = new byte[BufferSize];
				OriginalRequest = originalReq;
				ResponseBuffer = new MemoryStream();
				Request = req;
				StreamResponse = null;
				RequestId = requestId;
				PreviousUserData = previousUserData;
			}

			public override void AbortRequest() {
				if (Request != null) {
					Request.Abort();
				}
			}

			/// <summary>Prints the current request for user convenience.</summary>
			internal void LogRequest() {
				if (!VerboseMode) { return; }

				StringBuilder sb = new StringBuilder();
				HttpWebRequest request = Request;
				sb.AppendLine("[" + RequestId + "] " + request.Method + "ing on " + request.RequestUri);
				sb.AppendLine("Headers (Mono):");
				foreach (string key in request.Headers) {
					sb.AppendLine("\t" + key + ": " + request.Headers[key]);
				}
				if (OriginalRequest.HasBody) {
					sb.AppendLine("Body: " + OriginalRequest.BodyString);
				}
				Common.Log(sb.ToString());
			}

			/// <summary>Prints information about the response for debugging purposes.</summary>
			internal void LogResponse(HttpResponse response) {
				if (!VerboseMode) { return; }

				StringBuilder sb = new StringBuilder();
				HttpWebRequest req = Request;
				sb.AppendLine("[" + RequestId + "] " + response.StatusCode + " on " + req.Method + "ed on " + req.RequestUri);
				sb.AppendLine("Recv. headers:");
				foreach (var pair in response.Headers) {
					sb.AppendLine("\t" + pair.Key + ": " + pair.Value);
				}
				if (response.HasBody) {
					sb.AppendLine("Body: " + response.BodyString);
				}
				Common.Log(sb.ToString());
			}

			public override void Start() {
				self.StartRequest(this);
			}
		}

		protected override WebRequest CreateRequest(HttpRequest request, string url, object previousUserData) {
			HttpWebRequest req = HttpWebRequest.Create(url) as HttpWebRequest;

			// Auto choose HTTP method
			req.Method = request.Method ?? (request.Body != null ? "POST" : "GET");
			req.UserAgent = request.UserAgent;
			req.KeepAlive = true;
			req.ServicePoint.ConnectionLimit = ConcurrentHttpRequestLimit;
			foreach (var pair in request.Headers) {
				if (String.Compare(pair.Key, "Content-Type", true) == 0)
					req.ContentType = pair.Value;
				else
					req.Headers[pair.Key] = pair.Value;
			}

			// Configure & perform the request
			return new RequestState(this, request, req, previousUserData, (RequestCount += 1));
		}

		private void StartRequest(RequestState request) {
			request.LogRequest();
			if (request.OriginalRequest.Body != null) {
				request.Request.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), request);
			}
			else {
				request.Request.BeginGetResponse(new AsyncCallback(RespCallback), request);
			}
		}

		/// <summary>Got a network stream to write to.</summary>
		private void GetRequestStreamCallback(IAsyncResult asynchronousResult) {
			RequestState state = asynchronousResult.AsyncState as RequestState;
			try {
				// End the operation
				Stream postStream = state.Request.EndGetRequestStream(asynchronousResult);
				// Write to the request stream.
				postStream.Write(state.OriginalRequest.Body, 0, state.OriginalRequest.Body.Length);
				postStream.Close();
				// Start the asynchronous operation to get the response
				state.Request.BeginGetResponse(new AsyncCallback(RespCallback), state);
			}
			catch (WebException e) {
				Common.Log("Failed to send data: " + e.Message + ", status=" + e.Status);
				if (e.Status != WebExceptionStatus.RequestCanceled) {
					FinishWithRequest(state, new HttpResponse(e));
				}
			}
		}

		/// <summary>Called when a response has been received by the HttpWebRequest.</summary>
		private void RespCallback(IAsyncResult asynchronousResult) {
			RequestState state = asynchronousResult.AsyncState as RequestState;
			try {
				// State of request is asynchronous.
				HttpWebRequest req = state.Request;
				state.Response = req.EndGetResponse(asynchronousResult) as HttpWebResponse;
				
				// Read the response into a Stream object.
				Stream responseStream = state.Response.GetResponseStream();
				state.StreamResponse = responseStream;
				// Begin reading the contents of the page
				responseStream.BeginRead(state.BufferRead, 0, RequestState.BufferSize, new AsyncCallback(ReadCallBack), state);
				return;
			}
			catch (WebException e) {
				if (e.Response == null) {
					if (e.Status != WebExceptionStatus.RequestCanceled) {
						Common.Log("Error: " + e.Message);
					}
					FinishWithRequest(state, new HttpResponse(e));
					return;
				}
				// When there is a ProtocolError or such (HTTP code 4xx…), there is also a response associated, so read it anyway.
				state.Response = e.Response as HttpWebResponse;
				Stream responseStream = state.Response.GetResponseStream();
				state.StreamResponse = responseStream;
				responseStream.BeginRead(state.BufferRead, 0, RequestState.BufferSize, new AsyncCallback(ReadCallBack), state);
				return;
			}
			catch (Exception e) {
				Common.Log("Error: " + e.Message);
				FinishWithRequest(state, new HttpResponse(e));
			}
			if (state.Response != null) { state.Response.Close(); }
		}

		/// <summary>Reads the response buffer little by little.</summary>
		private void ReadCallBack(IAsyncResult asyncResult) {
			RequestState state = asyncResult.AsyncState as RequestState;
			try {
				Stream responseStream = state.StreamResponse;
				int read = responseStream.EndRead(asyncResult);
				// Read the HTML page and then print it to the console.
				if (read > 0) {
					state.ResponseBuffer.Write(state.BufferRead, 0, read);
					responseStream.BeginRead(state.BufferRead, 0, RequestState.BufferSize, new AsyncCallback(ReadCallBack), state);
					return;
				}
				else {
					// Finished reading
					responseStream.Close();

					HttpResponse result = new HttpResponse();
					HttpWebResponse response = state.Response;
					result.StatusCode = (int) response.StatusCode;
					foreach (string key in response.Headers) {
						result.Headers[key] = response.Headers[key];
					}
					// Read the body
					result.Body = state.ResponseBuffer.ToArray();
					// Logging
					state.LogResponse(result);
					FinishWithRequest(state, result);
				}
			}
			catch (Exception e) {
				Common.LogWarning("Failed to read response: " + e.ToString());
				FinishWithRequest(state, new HttpResponse(e));
			}
		}
	}
}
