using UnityEngine;
using System.Collections;
using System;
using System.Threading;

namespace CloudBuilderLibrary {
	public class CloudBuilder {
		private static Clan clanInstance;
		private static IHttpClient httpClientInstance;
		private static ILogger loggerInstance;
		private static ISystemFunctions systemFunctionsInstance;
		// TODO
		internal const string Version = "1";

		public static Clan Clan {
			get { return clanInstance; }
		}

		// You need to call this from the update of your current scene!
/*		public static void Update() {
			// TODO gérer le timeout de http
		}*/

		internal static void Log(string text) {
			loggerInstance.Log(LogLevel.Verbose, text);
		}
		internal static void Log(LogLevel level, string text) {
			loggerInstance.Log(level, text);
		}
		internal static void TEMP(string text) {
			// All references to this should be removed at some point
			loggerInstance.Log (LogLevel.Verbose, text);
		}
		internal static void StartLogTime(string description = null) {
			initialTicks = DateTime.UtcNow.Ticks;
			LogTime(description);
		}
		internal static void LogTime(string description = null) {
			TimeSpan span = new TimeSpan(DateTime.UtcNow.Ticks - initialTicks);
			loggerInstance.Log(LogLevel.Verbose, "[" + span.TotalMilliseconds  + "/" + Thread.CurrentThread.ManagedThreadId + "] " + description);
		}

		#region Internal stuff
		static CloudBuilder() {
			clanInstance = new Clan();
			httpClientInstance = new UnityHttpClient();
			loggerInstance = UnityLogger.Instance;
			systemFunctionsInstance = new UnitySystemFunctions();
        }

		internal static IHttpClient HttpClient {
			get { return httpClientInstance; }
		}

		internal static ISystemFunctions SystemFunctions {
			get { return systemFunctionsInstance; }
		}
		#endregion

		#region Private
		private static long initialTicks;
		#endregion
	}
}
