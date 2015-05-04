using System;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public static class Common {
		internal static void InvokeHandler<T>(ResultHandler<T> handler, Result<T> result) {
			if (handler != null) {
				handler(result);
			}
		}
		internal static void InvokeHandler<T>(ResultHandler<T> handler, ErrorCode code, string reason = null) {
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
		internal static void InvokeHandler<T>(ResultHandler<T> handler, T value, Bundle serverData = null) {
			if (handler != null) {
				handler(new Result<T>(value, serverData));
			}
		}

		internal static T ParseEnum<T>(string value) {
			return (T) Enum.Parse(typeof(T), value, true);
		}

		internal static DateTime ParseHttpDate(string httpDate) {
			return DateTime.Parse(httpDate);
		}

		internal static string ToHttpDateString(this DateTime d) {
			return d.ToString("s", System.Globalization.CultureInfo.InvariantCulture);
		}

		public const string PrivateDomain = "private";
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
