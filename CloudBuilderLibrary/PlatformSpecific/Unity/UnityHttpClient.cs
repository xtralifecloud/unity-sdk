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
			CloudBuilder.LogError("Unable to abort " + request.ToString() + ", probably not running anymore");
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
			public MemoryStream ResponseBuffer;
			public byte[] BufferRead;
			public int RequestId;
			public HttpRequest OriginalRequest;
			public HttpWebRequest Request;
			public HttpWebResponse Response;
			public Stream StreamResponse;
			public UnityHttpClient self;
			public bool Aborted;
			public object PreviousUserData;
			public RequestState(UnityHttpClient inst, HttpRequest originalReq, HttpWebRequest req, object previousUserData) {
				self = inst;
				BufferRead = new byte[BufferSize];
				OriginalRequest = originalReq;
				ResponseBuffer = new MemoryStream();
				Request = req;
				StreamResponse = null;
				RequestId = (self.RequestCount += 1);
				PreviousUserData = previousUserData;
			}
		}

		private void ChooseLoadBalancer(HttpRequest req) {
			CurrentLoadBalancerId = Random.Next(1, req.LoadBalancerCount + 1);
		}

		/** Enqueues a request to make it processed asynchronously. Will potentially wait for the other requests enqueued to finish. */
		private void EnqueueRequest(HttpRequest req) {
			// On the first time, choose a load balancer
			if (CurrentLoadBalancerId == -1) {
				ChooseLoadBalancer(req);
			}
			lock (this) {
				// Dismiss additional requests
				if (Terminated) {
					CloudBuilder.LogError("Attempted to run a request after having terminated");
					return;
				}
				// Need to enqueue process?
				if (!req.DoNotEnqueue) {
					if (IsProcessingRequest) {
						PendingRequests.Add(req);
						return;
					}
					IsProcessingRequest = true;
				}
			}
			// Or start immediately (if the previous request failed, use the last delay directly)
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
			// Has failed?
			if (response.ShouldBeRetried(state.OriginalRequest) && !state.Aborted) {
				if (state.OriginalRequest.FailedHandler != null) {
					// See whether to try again
					var eventArgs = new HttpRequestFailedEventArgs(state.OriginalRequest, state.PreviousUserData);
					// Invoke the failure handler
					try {
						state.OriginalRequest.FailedHandler(eventArgs);
						if (eventArgs.RetryDelay < 0) throw new InvalidOperationException("HTTP request failed handler called but didn't tell what to do next.");
						if (eventArgs.RetryDelay > 0) {
							CloudBuilder.LogWarning("[" + state.RequestId + "] Request failed, retrying in " + eventArgs.RetryDelay + "ms.");
							Thread.Sleep(eventArgs.RetryDelay);
							ChooseLoadBalancer(state.OriginalRequest);
							ProcessRequest(state.OriginalRequest, eventArgs.UserData);
							return;
						}
					}
					catch (Exception e) {
						CloudBuilder.LogError("Error in failure handler: " + e.ToString());
					}
				}
				// Maximum failure count reached, will simply process the next request
				CloudBuilder.LogWarning("[" + state.RequestId + "] Request failed.");
			}
			// Final result for this request
			if (state.OriginalRequest.Callback != null) {
				CloudBuilder.RunOnMainThread(() => state.OriginalRequest.Callback(response));
			}
			// Was independent?
			if (state.OriginalRequest.DoNotEnqueue) return;
			
			// Process next request
			lock (this) {
				// Note: currently another request is only launched after synchronously processed by the callback. This behavior is slower but might be safer.
				if (!state.OriginalRequest.DoNotEnqueue && PendingRequests.Count == 0) {
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
				// Write to the request stream.
				postStream.Write(state.OriginalRequest.Body, 0, state.OriginalRequest.Body.Length);
				postStream.Close();
				// Start the asynchronous operation to get the response
				state.Request.BeginGetResponse(new AsyncCallback(RespCallback), state);
			}
			catch (WebException e) {
				CloudBuilder.Log("Failed to send data: " + e.Message + ", status=" + e.Status);
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
		private void ProcessRequest(HttpRequest request, object previousUserData = null) {
			String url = request.Url.Replace("[id]", CurrentLoadBalancerId.ToString("00"));
			HttpWebRequest req = HttpWebRequest.Create(url) as HttpWebRequest;

			// Auto choose HTTP method
			req.Method = request.Method ?? (request.Body != null ? "POST" : "GET");
			req.UserAgent = request.UserAgent;
			foreach (var pair in request.Headers) {
				if (String.Compare(pair.Key, "Content-Type", true) == 0)
					req.ContentType = pair.Value;
				else
					req.Headers[pair.Key] = pair.Value;
			}

			// Configure & perform the request
			RequestState state = new RequestState(this, request, req, previousUserData);
			LogRequest(state);
			lock (this) {
				RunningRequests.Add(state);
			}

			AllDone.Reset();
			if (request.Body != null) {
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
					CloudBuilder.Log("Error: " + e.Message);
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
				CloudBuilder.Log("Error: " + e.Message);
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
					LogResponse(state, result);
					FinishWithRequest(state, result);
				}
			}
			catch (Exception e) {
				CloudBuilder.LogWarning("Failed to read response: " + e.ToString());
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
				CloudBuilder.LogWarning("Request timed out");
				requestState.self.FinishWithRequest(requestState, response);
			}
		}

		// Request processing
		private ManualResetEvent AllDone = new ManualResetEvent(false);
		private bool IsProcessingRequest = false;	// Only affected for enqueued requests
		private List<HttpRequest> PendingRequests = new List<HttpRequest>();
		private List<RequestState> RunningRequests = new List<RequestState>();
		// Others
		private bool VerboseMode;
		private int CurrentLoadBalancerId = -1;
		private System.Random Random = new System.Random();
		private int RequestCount = 0;
		private bool Terminated = false;
		#endregion
	}
}
