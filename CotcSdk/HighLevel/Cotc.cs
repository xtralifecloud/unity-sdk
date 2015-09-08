using System;
using System.Collections.Generic;

namespace CotcSdk {
	public static partial class Cotc {

		static Cotc() {
			// Bypass checks TODO
/*			System.Net.ServicePointManager.ServerCertificateValidationCallback += (s, ce, ca, p) => {
				return true;
			};*/
		}

		/// <summary>Call this at the very beginning to start using the library.</summary>
		/// <returns>Promise resolved when the process has finished, with the Cloud to be used for your operations (most
		///     likely synchronously).</returns>
		/// <param name="apiKey">The community key.</param>
		/// <param name="apiSecret">The community secret (credentials when registering to CotC).</param>
		/// <param name="environment">The URL of the server. Should use one of the predefined constants.</param>
		/// <param name="httpVerbose">Set to true to output detailed information about the requests performed to CotC servers. Can be used
		///     for debugging, though it does pollute the logs.</param>
		/// <param name="httpTimeout">Sets a custom timeout for all requests in seconds. Defaults to 1 minute.</param>
		public static Promise<Cloud> Setup(string apiKey, string apiSecret, string environment, int loadBalancerCount, bool httpVerbose, int httpTimeout) {
			var task = new Promise<Cloud>();
			lock (SpinLock) {
				Cloud cloud = new Cloud(apiKey, apiSecret, environment, loadBalancerCount, httpVerbose, httpTimeout);
				return task.PostResult(cloud);
			}
		}

		/// <summary>
		/// Please call this in an override of OnApplicationFocus on your main object (e.g. scene).
		/// http://docs.unity3d.com/ScriptReference/MonoBehaviour.OnApplicationFocus.html
		/// </summary>
		public static void OnApplicationFocus(bool focused) {
			foreach (DomainEventLoop loop in RunningEventLoops) {
				if (focused) {
					loop.Resume();
				}
				else {
					loop.Suspend();
				}
			}
			NotifyFocusChanged(null, focused);
		}

		/// <summary>
		/// Shuts off the existing instance of the Cloud and its descendent objects.
		/// Works synchronously so might take a bit of time.
		/// </summary>
		public static void OnApplicationQuit() {
			// Stop all running loops (in case the developer forgot to do it).
			// The loops will remove themselves from the list so make a copy.
			var copy = new List<DomainEventLoop>();
			copy.AddRange(RunningEventLoops);
			foreach (DomainEventLoop loop in copy) {
				loop.Stop();
			}
			Managers.HttpClient.Terminate();
		}

		/// <summary>
		/// Needs to be called from the update method of your main game object.
		/// Not needed if the CotcGameObject is used...
		/// </summary>
		public static void Update() {
			// Run pending actions
			lock (PendingForMainThread) {
				CurrentActions.Clear();
				CurrentActions.AddRange(PendingForMainThread);
				PendingForMainThread.Clear();
			}
			foreach (Action a in CurrentActions) {
				a();
			}
		}
		
		/// <summary>Runs a method on the main thread (actually at the next update).</summary>
		public static void RunOnMainThread(Action action) {
			lock (PendingForMainThread) {
				PendingForMainThread.Add(action);
			}
		}

		internal static DomainEventLoop GetEventLoopFor(string gamerId, string domain) {
			foreach (DomainEventLoop loop in RunningEventLoops) {
				if (loop.Domain == domain && loop.Gamer.GamerId == gamerId) {
					return loop;
				}
			}
			return null;
		}

		internal static List<Action> PendingForMainThread = new List<Action>();
		// For cleanup upon terminate
		internal static List<DomainEventLoop> RunningEventLoops = new List<DomainEventLoop>();

		#region Private
		private static object SpinLock = new object();
		private static List<Action> CurrentActions = new List<Action>();
		#endregion
	}
}
