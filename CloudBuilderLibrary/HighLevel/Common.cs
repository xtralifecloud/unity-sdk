using System;
using System.Collections.Generic;

namespace CloudBuilderLibrary
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

		public static void InvokeHandler<T>(ResultHandler<T> handler, Result<T> result) {
			if (handler != null) {
				handler(result);
			}
		}
		public static void InvokeHandler<T>(ResultHandler<T> handler, ErrorCode code, string reason = null) {
			if (handler != null) {
				handler(new Result<T>(code, reason));
			}
		}
		internal static void InvokeHandler<T>(ResultHandler<T> handler, HttpResponse response, string reason = null) {
			if (handler != null) {
				Result<T> result = new Result<T>(response);
				result.ErrorInformation = reason;
				handler(result);
			}
		}
		public static void InvokeHandler<T>(ResultHandler<T> handler, T value, Bundle serverData = null) {
			if (handler != null) {
				handler(new Result<T>(value, serverData));
			}
		}

		internal static T ParseEnum<T>(string value) {
			if (value != null) return (T)Enum.Parse(typeof(T), value, true);
			else return default(T);
		}

		internal static DateTime ParseHttpDate(string httpDate) {
			return httpDate != null ? DateTime.Parse(httpDate) : DateTime.MinValue;
		}

		/**
		 * Wrapper around our standard work on Managers.HttpClient.Run. Automatically notifies the passed handler
		 * of a failure.
		 * @param req request to perform.
		 * @param handler handler to call in case of failure.
		 * @param onSuccess callback called in case of success only (handler untouched in that case).
		 */
		internal static void RunHandledRequest<T>(HttpRequest req, ResultHandler<T> handler, Action<HttpResponse> onSuccess) {
			Managers.HttpClient.Run(req, (HttpResponse response) => {
				if (HasFailed(response)) {
					InvokeHandler(handler, response);
					return;
				}
				if (onSuccess != null) onSuccess(response);
			});
		}

		internal static string ToHttpDateString(this DateTime d) {
			return d.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
		}

		// TODO Replace by a soft value (changed upon branching?)
		public const string SdkVersion = "2.11";
		public const string PrivateDomain = "private";
		public const string UserAgent = "cloudbuilder-unity-{0}-{1}";	// os, sdkversion
	}

	/**
	 * Standard delegates for most of the API calls. Returns an object wrapped into a Result object, which contains information about the error.
	 * To obtain the wrapped object, fetch the Value member of the result.
	 */
	public delegate void ResultHandler<T>(Result<T> obj);
	
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
