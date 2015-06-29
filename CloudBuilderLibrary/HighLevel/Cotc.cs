using UnityEngine;
using System;
using System.Collections.Generic;

namespace CotcSdk {
	public static class Cotc {

		/**
		 * Call this at the very beginning to start using the library.
		 * @param done called when the process has finished with the Cloud to be used for your operations (most likely synchronously).
		 * @param apiKey the community key.
		 * @param apiSecret The community secret (credentials when registering to CotC).
		 * @param environment the URL of the server. Should use one of the predefined constants.
		 * @param httpVerbose set to true to output detailed information about the requests performed to CotC servers. Can be used
		 *	 for debugging, though it does pollute the logs.
		 * @param httpTimeout sets a custom timeout for all requests in seconds. Defaults to 1 minute.
		 */
		public static IPromise<Cloud> Setup(string apiKey, string apiSecret, string environment, int loadBalancerCount, bool httpVerbose, int httpTimeout) {
			var task = new Promise<Cloud>();
			lock (SpinLock) {
				Cloud cloud = new Cloud(apiKey, apiSecret, environment, loadBalancerCount, httpVerbose, httpTimeout);
				return task.PostResult(cloud, Bundle.Empty);
			}
		}

		/**
		 * Please call this in an override of OnApplicationFocus on your main object (e.g. scene).
		 * http://docs.unity3d.com/ScriptReference/MonoBehaviour.OnApplicationFocus.html
		 */
		public static void OnApplicationFocus(bool focused) {
			foreach (DomainEventLoop loop in RunningEventLoops) {
				if (focused) {
					loop.Resume();
				}
				else {
					loop.Suspend();
				}
			}
		}

		/**
		 * Shuts off the existing instance of the Cloud and its descendent objects.
		 * Works synchronously so might take a bit of time.
		 */
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

		/**
		 * Needs to be called from the update method of your main game object.
		 * Not needed if the CotcGameObject is used...
		 */
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
		
		/**
		 * Runs a method on the main thread (actually at the next update).
		 */
		internal static void RunOnMainThread(Action action) {
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
