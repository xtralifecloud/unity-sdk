using UnityEngine;
using System.Collections;
using System;
using System.Threading;

namespace CloudBuilderLibrary {
	public static class CloudBuilder {
		private const int DefaultTimeoutSec = 60, DefaultPopEventTimeoutSec = 590;
		public const string DevEnvironment = "http://195.154.227.44:8000";
		public const string SandboxEnvironment = "https://sandbox-api[id].clanofthecloud.mobi";
		public const string ProdEnvironment = "https://prod-api[id].clanofthecloud.mobi";

		/**
		 * Call this at the very beginning to start using the library.
		 * @param done Called when the process has finished with the Clan to be used for your operations (most likely synchronously).
		 * @param apiKey The community key.
		 * @param apiSecret The community secret (credentials when registering to CotC).
		 * @param environment The URL of the server. Should use one of the predefined constants.
		 * @param httpVerbose Set to true to output detailed information about the requests performed to CotC servers. Can be used
		 *	 for debugging, though it does pollute the logs.
		 * @param httpTimeout Sets a custom timeout for all requests in seconds. Defaults to 1 minute.
		 * @param eventLoopTimeout Sets a custom timeout in seconds for the long polling event loop. Should be used with care
		 *	 and set to a high value (at least 60). Defaults to 590 (~10 min).
		 */
		public static void Setup(ResultHandler<Clan> done, string apiKey, string apiSecret, string environment = SandboxEnvironment, bool httpVerbose = false, int httpTimeout = DefaultTimeoutSec, int eventLoopTimeout = DefaultPopEventTimeoutSec) {
			lock (SpinLock) {
				if (ClanInstance != null) {
					Common.InvokeHandler(done, ErrorCode.enSetupAlreadyCalled);
					return;
				}
				ClanInstance = new Clan(apiKey, apiSecret, environment, httpVerbose, httpTimeout, eventLoopTimeout);
				Common.InvokeHandler(done, ClanInstance);
			}
		}

		/**
		 * Shuts off the existing instance of the Clan and its descendent objects.
		 * Works synchronously so might take a bit of time.
		 */
		public static void Terminate() {
			Directory.HttpClient.Terminate();
		}

		#region Internal
		internal static void Log(string text) {
			Directory.Logger.Log(LogLevel.Verbose, text);
		}
		internal static void Log(LogLevel level, string text) {
			Directory.Logger.Log(level, text);
		}
		internal static void TEMP(string text) {
			// All references to this should be removed at some point
			Directory.Logger.Log (LogLevel.Verbose, text);
		}
		internal static void StartLogTime(string description = null) {
			InitialTicks = DateTime.UtcNow.Ticks;
			LogTime(description);
		}
		internal static void LogTime(string description = null) {
			TimeSpan span = new TimeSpan(DateTime.UtcNow.Ticks - InitialTicks);
			Directory.Logger.Log(LogLevel.Verbose, "[" + span.TotalMilliseconds  + "/" + Thread.CurrentThread.ManagedThreadId + "] " + description);
		}
		#endregion

		#region Private
		private static object SpinLock = new object();
		internal static Clan ClanInstance { get; private set; }
		// TODO
		internal const string Version = "1";
		private static long InitialTicks;
		#endregion
	}
}
