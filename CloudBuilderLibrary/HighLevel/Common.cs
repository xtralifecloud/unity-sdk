using System;
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

		public static void Log(string text) {
			Log(LogLevel.Verbose, text);
		}

		public static void LogWarning(string text) {
			Log(LogLevel.Warning, text);
		}

		public static void LogError(string text) {
			Log(LogLevel.Error, text);
		}

		public static void TEMP(string text) {
			// All references to this should be removed at some point
			Log(LogLevel.Warning, "TEMP: " + text);
		}

		public static void StartLogTime(string description = null) {
			InitialTicks = DateTime.UtcNow.Ticks;
			LogTime(description);
		}

		public static void LogTime(string description = null) {
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
		internal static Promise<T> RunRequest<T>(HttpRequest req, Promise<T> task, Action<HttpResponse> onSuccess) {
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
		internal static Promise<T> RunInTask<T>(HttpRequest req, Action<HttpResponse, Promise<T>> onSuccess) {
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

		private static void Log(LogLevel level, string text) {
			if (LoggedLine != null) {
				LoggedLine(typeof(Common), new LogEventArgs(level, text));
			}
			else {
				Managers.Logger.Log(level, text);
			}
		}

		// TODO Replace by a soft value (changed upon branching?)
		public const string SdkVersion = "0.01";
		public const string PrivateDomain = "private";
		public const string UserAgent = "cloudbuilder-unity-{0}-{1}";	// os, sdkversion
		public static event EventHandler<LogEventArgs> LoggedLine;
		
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

	/**
	 * Information about a log entry.
	 */
	public class LogEventArgs : EventArgs {
		public LogLevel Level;
		public string Text;

		internal LogEventArgs(LogLevel level, string text) {
			this.Level = level;
			this.Text = text;
		}
	}
}
