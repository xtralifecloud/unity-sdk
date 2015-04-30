using System;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public static class Common {
		internal static void InvokeHandler(Action handler) {
			if (handler != null) {
				handler();
			}
		}
		internal static void InvokeHandler<T>(Action<CloudResult, T> handler, CloudResult result) where T: class {
			if (handler != null) {
				handler(result, null);
			}
		}
		internal static void InvokeHandler<T>(Action<CloudResult, T> handler, ErrorCode code, string description = null) where T: class {
			if (handler != null) {
				handler(new CloudResult(code, description), null);
			}
		}
		internal static void InvokeHandler<T>(Action<CloudResult, T> handler, T value) where T: class {
			if (handler != null) {
				handler(new CloudResult(ErrorCode.enNoErr), value);
			}
		}
		internal static void InvokeHandler<T>(Action<CloudResult, T> handler, CloudResult result, T value) {
			if (handler != null) {
				handler(result, value);
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

	public delegate void ResultHandler(CloudResult result);
}
