using System;
using System.Collections.Generic;
using System.Text;

namespace CloudBuilderLibrary {
	internal class HttpResponse {
		public byte[] Body {
			get { return body; }
			set { body = value; CachedBundle = null; CachedString = null; }
		}
		public string BodyString {
			get {
				if (CachedString == null && body != null) {
					CachedString = Encoding.UTF8.GetString(Body);
				}
				return CachedString;
			}
		}
		public Bundle BodyJson {
			get { CachedBundle = CachedBundle ?? Bundle.FromJson(BodyString); return CachedBundle; }
		}
		public Exception Exception;
		public bool HasBody {
			get { return body != null; }
		}
		/** If true, means that the request has completely failed, not that it received an error code such as 400.
		 * This will appear as completely normal. Use Common.HasFailed in that case. */
		public bool HasFailed {
			get { return Exception != null; }
		}
		public Dictionary<String, String> Headers = new Dictionary<string, string>();
		public HttpRequest OriginalRequest;
		/** Returns whether this response is in an error state that should be retried according to the request configuration. */
		public bool ShouldBeRetried(HttpRequest request) {
			switch (request.RetryPolicy) {
			case HttpRequest.Policy.NonpermanentErrors:
				return HasFailed || StatusCode < 100 || (StatusCode >= 300 && StatusCode < 400) || StatusCode >= 500;
			case HttpRequest.Policy.AllErrors:
				return HasFailed || StatusCode < 100 || StatusCode >= 300;
			case HttpRequest.Policy.Never:
			default:
				return false;
			}
		}
		public int StatusCode;

		public HttpResponse() {}
		public HttpResponse(Exception e) { Exception = e; }

		private byte[] body;
		private Bundle CachedBundle;
		private string CachedString;
	}
}

