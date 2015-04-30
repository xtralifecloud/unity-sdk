using UnityEngine;
using System.Collections;
using System;
using System.Threading;

namespace CloudBuilderLibrary {
	public class CloudBuilder {
		private static Clan ClanInstance;
		private static IHttpClient HttpClientInstance;
		private static ILogger LoggerInstance;
		private static ISystemFunctions SystemFunctionsInstance;
		private static UserManager UserManagerInstance;
		// TODO
		internal const string Version = "1";

		public static Clan Clan {
			get { return ClanInstance; }
		}

		public static UserManager UserManager {
			get { return UserManagerInstance; }
		}

		// You need to call this from the update of your current scene!
/*		public static void Update() {
			// TODO gérer le timeout de http
		}*/

		internal static void Log(string text) {
			LoggerInstance.Log(LogLevel.Verbose, text);
		}
		internal static void Log(LogLevel level, string text) {
			LoggerInstance.Log(level, text);
		}
		internal static void TEMP(string text) {
			// All references to this should be removed at some point
			LoggerInstance.Log (LogLevel.Verbose, text);
		}
		internal static void StartLogTime(string description = null) {
			InitialTicks = DateTime.UtcNow.Ticks;
			LogTime(description);
		}
		internal static void LogTime(string description = null) {
			TimeSpan span = new TimeSpan(DateTime.UtcNow.Ticks - InitialTicks);
			LoggerInstance.Log(LogLevel.Verbose, "[" + span.TotalMilliseconds  + "/" + Thread.CurrentThread.ManagedThreadId + "] " + description);
		}

		#region Internal stuff
		static CloudBuilder() {
			ClanInstance = new Clan();
			HttpClientInstance = new UnityHttpClient();
			LoggerInstance = UnityLogger.Instance;
			SystemFunctionsInstance = new UnitySystemFunctions();
			UserManagerInstance = new UserManager();
        }

		internal static IHttpClient HttpClient {
			get { return HttpClientInstance; }
		}

		internal static ISystemFunctions SystemFunctions {
			get { return SystemFunctionsInstance; }
		}
		#endregion

		#region Private
		private static long InitialTicks;
		#endregion
	}
}
