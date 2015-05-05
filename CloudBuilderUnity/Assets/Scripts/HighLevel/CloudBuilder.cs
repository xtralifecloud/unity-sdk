using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;

namespace CloudBuilderLibrary {
	public static class CloudBuilder {
		private const int DefaultTimeoutSec = 60, DefaultPopEventTimeoutSec = 590;
		public const string DevEnvironment = "http://195.154.227.44:8000";
		public const string SandboxEnvironment = "https://sandbox-api[id].clanofthecloud.mobi";
		public const string ProdEnvironment = "https://prod-api[id].clanofthecloud.mobi";

		/**
		 * Call this at the very beginning to start using the library.
		 * @param done called when the process has finished with the Clan to be used for your operations (most likely synchronously).
		 * @param apiKey the community key.
		 * @param apiSecret The community secret (credentials when registering to CotC).
		 * @param environment the URL of the server. Should use one of the predefined constants.
		 * @param httpVerbose set to true to output detailed information about the requests performed to CotC servers. Can be used
		 *	 for debugging, though it does pollute the logs.
		 * @param httpTimeout sets a custom timeout for all requests in seconds. Defaults to 1 minute.
		 * @param eventLoopTimeout sets a custom timeout in seconds for the long polling event loop. Should be used with care
		 *	 and set to a high value (at least 60). Defaults to 590 (~10 min).
		 */
		public static void Setup(ResultHandler<Clan> done, string apiKey, string apiSecret, string environment = SandboxEnvironment, bool httpVerbose = false, int httpTimeout = DefaultTimeoutSec, int eventLoopTimeout = DefaultPopEventTimeoutSec) {
			lock (SpinLock) {
				if (ClanInstance != null) {
					Common.InvokeHandler(done, ErrorCode.AlreadySetup);
					return;
				}
				ClanInstance = new Clan(apiKey, apiSecret, environment, httpVerbose, httpTimeout, eventLoopTimeout);
				Common.InvokeHandler(done, ClanInstance);
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

		#region Internal
		internal static void Log(string text) {
			Managers.Logger.Log(LogLevel.Verbose, text);
		}
		internal static void Log(LogLevel level, string text) {
			Managers.Logger.Log(level, text);
		}
		internal static void TEMP(string text) {
			// All references to this should be removed at some point
			Managers.Logger.Log(LogLevel.Verbose, text);
		}
		internal static void StartLogTime(string description = null) {
			InitialTicks = DateTime.UtcNow.Ticks;
			LogTime(description);
		}
		internal static void LogTime(string description = null) {
			TimeSpan span = new TimeSpan(DateTime.UtcNow.Ticks - InitialTicks);
			Managers.Logger.Log(LogLevel.Verbose, "[" + span.TotalMilliseconds + "/" + Thread.CurrentThread.ManagedThreadId + "] " + description);
		}

		// For cleanup upon terminate
		internal static List<DomainEventLoop> RunningEventLoops = new List<DomainEventLoop>();
		#endregion

		#region Private
		private static object SpinLock = new object();
		internal static Clan ClanInstance { get; private set; }
		private static long InitialTicks;
		#endregion
	}
}
