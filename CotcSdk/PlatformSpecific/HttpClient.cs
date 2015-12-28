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
	 * Base for an HTTP client. Handles many useful things (timeout, calculating the URL, altering the load balancer…).
	 */
	internal abstract class HttpClient {
		#region Public (API)
		public void Abort(HttpRequest request) {
			foreach (WebRequest state in RunningRequests) {
				if (state.OriginalRequest == request) {
					state.Aborted = true;
					state.AbortRequest();
					return;
				}
			}
			Common.LogError("Unable to abort " + request.ToString() + ", probably not running anymore");
		}

		public void Run(HttpRequest request, Action<HttpResponse> callback) {
			request.Callback = callback;
			request.DoNotEnqueue = true;
			EnqueueRequest(request);
		}

		public void Terminate() {
			// Abort all pending requests
			lock (this) {
				Terminated = true;
				foreach (WebRequest state in RunningRequests) {
					state.Aborted = true;
					state.AbortRequest();
				}
				RunningRequests.Clear();
			}
		}

		public bool VerboseMode { get; set; }
		#endregion

		#region To be overriden
		protected abstract WebRequest CreateRequest(HttpRequest request, string url, object previousUserData);
		#endregion

		#region Private
		/// <summary>Asynchronous request state.</summary>
		protected abstract class WebRequest {
			public bool Aborted, AlreadyFinished;
			public HttpClient Client;
			public HttpRequest OriginalRequest;
			public int RequestId;
			public object PreviousUserData;

			protected WebRequest(HttpClient client, HttpRequest originalRequest) {
				Client = client;
				OriginalRequest = originalRequest;
			}
			public abstract void AbortRequest();
			public abstract void Start();
		}

		private void ChooseLoadBalancer(HttpRequest req) {
			CurrentLoadBalancerId = Random.Next(1, req.LoadBalancerCount + 1);
		}

		/// <summary>Enqueues a request to make it processed asynchronously. Will potentially wait for the other requests enqueued to finish.</summary>
		private void EnqueueRequest(HttpRequest req) {
			// On the first time, choose a load balancer
			if (CurrentLoadBalancerId == -1) {
				ChooseLoadBalancer(req);
			}
			lock (this) {
				// Dismiss additional requests
				if (Terminated) {
					Common.LogError("Attempted to run a request after having terminated");
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

		/// <summary>Called after an HTTP request has been processed in any way (error or failure). Decides what to do next.</summary>
		protected void FinishWithRequest(WebRequest state, HttpResponse response) {
			// This "hack" is needed because sometimes the callback is called multiple times for a single request (notably from RespCallback)
			if (state.AlreadyFinished) return;
			state.AlreadyFinished = true;
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
							Common.LogWarning("[" + state.RequestId + "] Request failed, retrying in " + eventArgs.RetryDelay + "ms.");
							Thread.Sleep(eventArgs.RetryDelay);
							ChooseLoadBalancer(state.OriginalRequest);
							ProcessRequest(state.OriginalRequest, eventArgs.UserData);
							return;
						}
					}
					catch (Exception e) {
						Common.LogError("Error in failure handler: " + e.ToString());
					}
				}
				// Maximum failure count reached, will simply process the next request
				Common.LogWarning("[" + state.RequestId + "] Request failed.");
			}
			// Final result for this request
			if (state.OriginalRequest.Callback != null) {
				Cotc.RunOnMainThread(() => state.OriginalRequest.Callback(response));
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

		/// <summary>Processes a single request asynchronously. Will continue to FinishWithRequest in some way.</summary>
		private void ProcessRequest(HttpRequest request, object previousUserData = null) {
			// Configure & perform the request
			String url = request.Url.Replace("[id]", CurrentLoadBalancerId.ToString("00"));
			WebRequest state = CreateRequest(request, url, previousUserData);
			state.Client = this;

			lock (this) {
				RunningRequests.Add(state);
			}
			AllDone.Reset();
			state.Start();

			// Setup timeout
			if (request.TimeoutMillisec > 0) {
				ThreadPool.RegisterWaitForSingleObject(AllDone, new WaitOrTimerCallback(TimeoutCallback), state, request.TimeoutMillisec, true);
			}
		}

		/// <summary>Called upon timeout.</summary>
		private static void TimeoutCallback(object state, bool timedOut) { 
			if (timedOut) {
				WebRequest requestState = state as WebRequest;
				requestState.AbortRequest();
				HttpResponse response = new HttpResponse(new HttpTimeoutException());
				Common.LogWarning("Request timed out");
				requestState.Client.FinishWithRequest(requestState, response);
			}
		}

		// Request processing
		private ManualResetEvent AllDone = new ManualResetEvent(false);
		private bool IsProcessingRequest = false;	// Only affected for enqueued requests
		private List<HttpRequest> PendingRequests = new List<HttpRequest>();
		private List<WebRequest> RunningRequests = new List<WebRequest>();
		// Others
		private int CurrentLoadBalancerId = -1;
		private System.Random Random = new System.Random();
		private bool Terminated = false;
		#endregion
	}
}
