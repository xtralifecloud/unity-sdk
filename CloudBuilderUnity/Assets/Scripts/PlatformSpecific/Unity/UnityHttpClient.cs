using System;
using UnityEngine;
using System.Collections;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	internal class UnityHttpClient : IHttpClient {
		#region IHttpClient implementation
		void IHttpClient.Abort(HttpRequest request) {
			foreach (RequestState state in RunningRequests) {
				if (state.OriginalRequest == request) {
					state.Aborted = true;
					state.Request.Abort();
					return;
				}
			}
			CloudBuilder.Log(LogLevel.Error, "Unable to abort " + request.ToString() + ", probably not running anymore");
		}

		void IHttpClient.Run(HttpRequest request, Action<HttpResponse> callback) {
			request.Callback = callback;
			EnqueueRequest(request);
		}

		void IHttpClient.Terminate() {
			// Abort all pending requests
			lock (this) {
				Terminated = true;
                foreach (RequestState state in RunningRequests) {
					state.Aborted = true;
					state.Request.Abort();
				}
				RunningRequests.Clear();
			}
		}

		bool IHttpClient.VerboseMode {
			get { return VerboseMode; }
			set { VerboseMode = value; }
		}
		#endregion

		#region Private
		/**
		 * Asynchronous request state.
		 */
		private class RequestState {
			// This class stores the State of the request.
			public const int BufferSize = 1024;
			public StringBuilder RequestData;
			public byte[] BufferRead;
			public int RequestId;
			public HttpRequest OriginalRequest;
			public HttpWebRequest Request;
			public HttpWebResponse Response;
			public Stream StreamResponse;
			public UnityHttpClient self;
			public Action<RequestState, HttpResponse> FinishRequestOverride;
			public bool Aborted;
			public RequestState(UnityHttpClient inst, HttpRequest originalReq, HttpWebRequest req) {
				self = inst;
				BufferRead = new byte[BufferSize];
				OriginalRequest = originalReq;
				RequestData = new StringBuilder("");
				Request = req;
				StreamResponse = null;
				RequestId = (self.RequestCount += 1);
			}
		}

		private void ChooseLoadBalancer() {
			CurrentLoadBalancerId = Random.Next(1, CloudBuilder.ClanInstance.LoadBalancerCount + 1);
		}

		/** Enqueues a request to make it processed asynchronously. Will potentially wait for the other requests enqueued to finish. */
		private void EnqueueRequest(HttpRequest req) {
			// On the first time, choose a load balancer
			if (CurrentLoadBalancerId == -1) {
				ChooseLoadBalancer();
			}
			lock (this) {
				// Dismiss additional requests
				if (Terminated) {
					CloudBuilder.Log(LogLevel.Error, "Attempted to run a request after having terminated");
					return;
				}
				// Need to enqueue process?
				if (IsProcessingRequest) {
					PendingRequests.Add(req);
					return;
				}
				IsProcessingRequest = true;
			}
			// Or start immediately (if the previous request failed, use the last delay directly)
			CurrentRequestTryCount = LastRequestFailed ? req.TimeBetweenTries.Length - 1 : 0;
			ProcessRequest(req);
		}

		/** Called after an HTTP request has been processed in any way (error or failure). Decides what to do next. */
		private void FinishWithRequest(RequestState state, HttpResponse response) {
			// IDEA This function could probably be moved to another file with a little gymnastic…
			HttpRequest nextReq;
			// Avoid timeout to be triggered after that
			AllDone.Set();
            lock (this) {
				// No need to continue, dismiss the result
				if (Terminated) return;
				RunningRequests.Remove(state);
			}
			// Prevent doing the next tasks
			if (state.FinishRequestOverride != null) {
				state.FinishRequestOverride(state, response);
				return;
			}
			// Has failed?
			if (response.ShouldBeRetried(state.OriginalRequest) && !state.Aborted) {
				// Will try again
				int[] retryTimes = state.OriginalRequest.TimeBetweenTries;
				if (CurrentRequestTryCount < retryTimes.Length) {
					CloudBuilder.Log(LogLevel.Warning, "[" + state.RequestId + "] Request failed, retrying in " + retryTimes[CurrentRequestTryCount] + "ms.");
					Thread.Sleep(retryTimes[CurrentRequestTryCount]);
					CurrentRequestTryCount += 1;
					ChooseLoadBalancer();
					ProcessRequest(state.OriginalRequest);
					return;
				}
				// Maximum failure count reached, will simply process the next request
				CloudBuilder.Log(LogLevel.Warning, "[" + state.RequestId + "] Request failed too many times, giving up.");
				LastRequestFailed = true;
			}
			else {
				LastRequestFailed = false;
			}
			// Final result for this request
			if (state.OriginalRequest.Callback != null) {
				try {
					state.OriginalRequest.Callback(response);
				} catch (Exception e) {
					CloudBuilder.Log(LogLevel.Error, "Error happened when processing the response: " + e.ToString());
				}
			}
			
			// Process next request
			lock (this) {
				// Note: currently another request is only launched after synchronously processed by the callback. This behavior is slower but might be safer.
				if (PendingRequests.Count == 0) {
					IsProcessingRequest = false;
					return;
				}
				nextReq = PendingRequests[0];
				PendingRequests.RemoveAt(0);
			}
			ProcessRequest(nextReq);
		}

		/** Got a network stream to write to. */
		private void GetRequestStreamCallback(IAsyncResult asynchronousResult) {
			RequestState state = asynchronousResult.AsyncState as RequestState;
			try {
				// End the operation
				Stream postStream = state.Request.EndGetRequestStream(asynchronousResult);
				// Convert the string into a byte array. 
				byte[] byteArray = Encoding.UTF8.GetBytes(state.OriginalRequest.BodyString);
				// Write to the request stream.
				postStream.Write(byteArray, 0, byteArray.Length);
				postStream.Close();
				// Start the asynchronous operation to get the response
				state.Request.BeginGetResponse(new AsyncCallback(RespCallback), state);
			}
			catch (WebException e) {
				CloudBuilder.Log(LogLevel.Warning, "Failed to send data: " + e.Message + ", status=" + e.Status);
				if (e.Status != WebExceptionStatus.RequestCanceled) {
					FinishWithRequest(state, new HttpResponse(e));
				}
			}
		}
				
		/** Prints the current request for user convenience. */
		private void LogRequest(RequestState state) {
			if (!VerboseMode) { return; }

			StringBuilder sb = new StringBuilder();
			HttpWebRequest request = state.Request;
			sb.AppendLine("[" + state.RequestId + "] " + request.Method + "ing on " + request.RequestUri);
			sb.AppendLine("Headers:");
			foreach (string key in request.Headers) {
				sb.AppendLine("\t" + key + ": " + request.Headers[key]);
			}
			if (state.OriginalRequest.HasBody) {
				sb.AppendLine("Body: " + state.OriginalRequest.BodyString);
			}
			CloudBuilder.Log(sb.ToString());
		}

		/** Prints information about the response for debugging purposes. */
		private void LogResponse(RequestState state, HttpResponse response) {
			if (!VerboseMode) { return; }

			StringBuilder sb = new StringBuilder();
			HttpWebRequest req = state.Request;
			sb.AppendLine("[" + state.RequestId + "] " + response.StatusCode + " on " + req.Method + "ed on " + req.RequestUri);
			sb.AppendLine("Recv. headers:");
			foreach (var pair in response.Headers) {
				sb.AppendLine("\t" + pair.Key + ": " + pair.Value);
			}
			if (response.HasBody) {
				sb.AppendLine("Body: " + response.BodyString);
			}
			CloudBuilder.Log(sb.ToString());
		}

		/** Processes a single request asynchronously. Will continue to FinishWithRequest in some way. */
		private void ProcessRequest(HttpRequest request, Action<RequestState, HttpResponse> bypassProcessNextRequest = null) {
			String url = request.Url.Replace("[id]", CurrentLoadBalancerId.ToString("00"));
			HttpWebRequest req = HttpWebRequest.Create(url) as HttpWebRequest;

			// Auto choose HTTP method
			req.Method = request.Method ?? (request.BodyString != null ? "POST" : "GET");
			req.UserAgent = CloudBuilder.ClanInstance.UserAgent;
			foreach (var pair in request.Headers) {
				if (String.Compare(pair.Key, "Content-Type", true) == 0)
					req.ContentType = pair.Value;
				else
					req.Headers[pair.Key] = pair.Value;
			}

			// Configure & perform the request
			RequestState state = new RequestState(this, request, req);
			state.FinishRequestOverride = bypassProcessNextRequest;
			LogRequest(state);
			lock (this) {
				RunningRequests.Add(state);
			}

			AllDone.Reset();
			if (request.BodyString != null) {
				req.BeginGetRequestStream(new AsyncCallback(GetRequestStreamCallback), state);
			}
			else {
				req.BeginGetResponse(new AsyncCallback(RespCallback), state);
			}
			// Setup timeout
			if (request.TimeoutMillisec > 0) {
				ThreadPool.RegisterWaitForSingleObject(AllDone, new WaitOrTimerCallback(TimeoutCallback), state, request.TimeoutMillisec, true);
			}
		}

		/** Called when a response has been received by the HttpWebRequest. */
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
				FinishWithRequest(state, new HttpResponse(e));
			}
			if (state.Response != null) { state.Response.Close(); }
			AllDone.Set();
		}

		/** Reads the response buffer little by little. */
		private void ReadCallBack(IAsyncResult asyncResult) {
			RequestState state = asyncResult.AsyncState as RequestState;
			try {
				Stream responseStream = state.StreamResponse;
				int read = responseStream.EndRead(asyncResult);
				// Read the HTML page and then print it to the console. 
				if (read > 0) {
					state.RequestData.Append(Encoding.UTF8.GetString(state.BufferRead, 0, read));
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
					result.BodyString = state.RequestData.ToString();
					// Logging
					LogResponse(state, result);
					FinishWithRequest(state, result);
				}
			}
			catch (Exception e) {
				CloudBuilder.Log(LogLevel.Warning, "Failed to read response: " + e.Message);
				FinishWithRequest(state, new HttpResponse(e));
			}
			AllDone.Set();
		}

		/** Called upon timeout. */
		private static void TimeoutCallback(object state, bool timedOut) { 
			if (timedOut) {
				RequestState requestState = state as RequestState;
				if (requestState.Request != null) {
					requestState.Request.Abort();
				}
				HttpResponse response = new HttpResponse(new HttpTimeoutException());
				CloudBuilder.Log (LogLevel.Warning, "Request timed out");
				requestState.self.FinishWithRequest(requestState, response);
			}
		}

		// Request processing
		private ManualResetEvent AllDone = new ManualResetEvent(false);
		private bool IsProcessingRequest = false;
		private List<HttpRequest> PendingRequests = new List<HttpRequest>();
		private List<RequestState> RunningRequests = new List<RequestState>();
		// Others
		private bool VerboseMode;
		private int CurrentRequestTryCount = 0, CurrentLoadBalancerId = -1;
		private bool LastRequestFailed;
		private System.Random Random = new System.Random();
		private int RequestCount = 0;
		private bool Terminated = false;
		#endregion
	}
}
