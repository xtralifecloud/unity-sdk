using System;
using System.Collections.Generic;

namespace CloudBuilderLibrary {

	internal class HttpRequest {
		public enum Policy {
			AllErrors,
			NonpermanentErrors,
			Never,
		};

		public string BodyString {
			get { return body; }
			set { body = value; }
		}
		public Bundle BodyJson {
			set { body = value.ToJson(); }
		}
		public bool HasBody {
			get { return body != null; }
		}
		public Dictionary<String, String> Headers = new Dictionary<string, string>();
		/**
		 * When not set (null), uses GET if no body is provided, or POST otherwise.
		 */
		public string Method;
		public Policy RetryPolicy = Policy.NonpermanentErrors;
		public int[] TimeBetweenTries = defaultTimeBetweenTries;
		public string Url;
		public long TimeoutMillisec;

		// Please do not access this by yourself, this is only kept track of internally and will be ignored if set by you
		internal Action<HttpResponse> callback;
		private string body;
		private static readonly int[] defaultTimeBetweenTries = {1, 100, 1000, 1500, 2000, 3000, 4000, 6000, 8000};
	}
}
