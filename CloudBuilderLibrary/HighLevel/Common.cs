using System;
using System.Collections.Generic;
using System.Threading;

namespace CotcSdk
{
	public static class Common {
		/**
		 * Checks whether the response is negative (either it has failed completely,
		 * either it has given an error status code. You should not attempt to process
		 * the entity from the response if this method return yes. Just build a Result
		 * object with the response in question, add an error message and invoke the
		 * result handler with it.
		 * @return whether the server response is considered as failed
		 */
		internal static bool HasFailed(HttpResponse response) {
			return response.HasFailed || response.StatusCode < 200 || response.StatusCode >= 300;
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
			Managers.Logger.Log(LogLevel.Warning, "TEMP: " + text);
		}
		
		internal static void StartLogTime(string description = null) {
			InitialTicks = DateTime.UtcNow.Ticks;
			LogTime(description);
		}

		internal static void LogTime(string description = null) {
			TimeSpan span = new TimeSpan(DateTime.UtcNow.Ticks - InitialTicks);
			Managers.Logger.Log(LogLevel.Verbose, "[" + span.TotalMilliseconds + "/" + Thread.CurrentThread.ManagedThreadId + "] " + description);
		}
		
		internal static T ParseEnum<T>(string value, T defaultValue = default(T)) {
			try {
				return (T)Enum.Parse(typeof(T), value, true);
			}
			catch (Exception) {
				Common.Log("Failed to parse enum " + value);
			}
			return defaultValue;
		}

		internal static DateTime ParseHttpDate(string httpDate) {
			return httpDate != null ? DateTime.Parse(httpDate) : DateTime.MinValue;
		}

		/**
		 * Wrapper around our standard work on Managers.HttpClient.Run. Automatically notifies the passed handler
		 * of a failure.
		 * @param req request to perform.
		 * @param task task that is resolved in case of failure, else the onSuccess callback is called and you'll
		 *     have to resolve it from inside.
		 * @param onSuccess callback called in case of success only.
		 */
		internal static IPromise<T> RunRequest<T>(HttpRequest req, Promise<T> task, Action<HttpResponse> onSuccess) {
			Managers.HttpClient.Run(req, (HttpResponse response) => {
				if (HasFailed(response)) {
					task.PostResult(response, "Request failed");
					return;
				}
				if (onSuccess != null) onSuccess(response);
			});
			return task;
		}
		/**
		 * Wrapper around our standard work on Managers.HttpClient.Run. Automatically notifies the passed handler
		 * of a failure.
		 * @param req request to perform.
		 * @return a task that is resolved in case of failure (the onSuccess callback is not called) or to be resolved
		 *     from the onSuccess block in case of success.
		 * @param onSuccess callback called in case of success only, with the response and a new task that needs to
		 *     be resolved from there.
		 */
		internal static IPromise<T> RunInTask<T>(HttpRequest req, Action<HttpResponse, Promise<T>> onSuccess) {
			var task = new Promise<T>();
			Managers.HttpClient.Run(req, (HttpResponse response) => {
				if (HasFailed(response)) {
					task.PostResult(response, "Request failed");
					return;
				}
				if (onSuccess != null) onSuccess(response, task);
			});
			return task;
		}

		internal static string ToHttpDateString(this DateTime d) {
			return d.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
		}

		// TODO Replace by a soft value (changed upon branching?)
		public const string SdkVersion = "2.11";
		public const string PrivateDomain = "private";
		public const string UserAgent = "cloudbuilder-unity-{0}-{1}";	// os, sdkversion
		
		// Other variables
		private static long InitialTicks;
	}
	
	/**
	 * Holds a cached single-time-instantiated member.
	 */
	internal struct CachedMember <T> where T: class {
		public void Clear() { Instance = null; }
		public T Get(Func<T> instantiate) {
			return Instance = Instance ?? instantiate();
		}
		private T Instance;
	}

}
