using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;

namespace CloudBuilderLibrary {
	public static class CloudBuilder {

		/**
		 * Call this at the very beginning to start using the library.
		 * @param done called when the process has finished with the Clan to be used for your operations (most likely synchronously).
		 * @param apiKey the community key.
		 * @param apiSecret The community secret (credentials when registering to CotC).
		 * @param environment the URL of the server. Should use one of the predefined constants.
		 * @param httpVerbose set to true to output detailed information about the requests performed to CotC servers. Can be used
		 *	 for debugging, though it does pollute the logs.
		 * @param httpTimeout sets a custom timeout for all requests in seconds. Defaults to 1 minute.
		 */
		public static void Setup(ResultHandler<Clan> done, string apiKey, string apiSecret, string environment, bool httpVerbose, int httpTimeout) {
			lock (SpinLock) {
				Clan clan = new Clan(apiKey, apiSecret, environment, httpVerbose, httpTimeout);
				Common.InvokeHandler(done, clan, Bundle.Empty);
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
		 * Shuts off the existing instance of the Clan and its descendent objects.
		 * Works synchronously so might take a bit of time.
		 */
		public static void OnApplicationQuit() {
			// Stop all running loops (in case the developer forgot to do it)
			foreach (DomainEventLoop loop in RunningEventLoops) {
				loop.Stop();
			}
			Managers.HttpClient.Terminate();
		}

		/**
		 * Needs to be called from the update method of your main game object.
		 * Not needed if the CloudBuilderGameObject is used...
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
		internal static void Log(string text) {
			Managers.Logger.Log(LogLevel.Verbose, text);
		}
		internal static void LogWarning(string text) {
			Managers.Logger.Log(LogLevel.Warning, text);
		}
		internal static void LogError(string text) {
			Managers.Logger.Log(LogLevel.Error, text);
		}
		internal static void TEMP(string text) {
			// All references to this should be removed at some point
			Managers.Logger.Log(LogLevel.Warning, text);
		}
		internal static void StartLogTime(string description = null) {
			InitialTicks = DateTime.UtcNow.Ticks;
			LogTime(description);
		}
		internal static void LogTime(string description = null) {
			TimeSpan span = new TimeSpan(DateTime.UtcNow.Ticks - InitialTicks);
			Managers.Logger.Log(LogLevel.Verbose, "[" + span.TotalMilliseconds + "/" + Thread.CurrentThread.ManagedThreadId + "] " + description);
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
		private static long InitialTicks;
		private static List<Action> CurrentActions = new List<Action>();
		#endregion
	}
}
