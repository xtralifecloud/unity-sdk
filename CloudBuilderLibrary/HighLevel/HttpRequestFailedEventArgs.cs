using System;

namespace CotcSdk {

	/**
	 * Delegate for failed HTTP requests. See Cloud.HttpRequestFailedHandler.
	 */
	public delegate void HttpRequestFailedHandler(HttpRequestFailedEventArgs e);

	/**
	 * This class is passed to the HttpRequestFailed handler as set on the Cloud.
	 * You need to do something with it, either call Abort or RetryIn else the http service will
	 * throw an exception.
	 */
	public class HttpRequestFailedEventArgs {
		/**
		 * The original URL that the request failed to reach.
		 */
		public string Url {
			get;
			private set;
		}
		/**
		 * You can set this member from the handler; in case the request fails again, this data will be set
		 * to the same value as set last time. It is always set to null when the request fails for the first time.
		 */
		public object UserData;

		/**
		 * Call this to abort the request. It won't be tried ever again.
		 */
		public void Abort() {
			RetryDelay = 0;
		}

		/**
		 * Call this to retry the request later.
		 * @param milliseconds time in which to try again. No other request will be executed during this time
		 *     (they will be queued) as to respect the issuing order. Please keep this in mind when setting a
		 *     high delay.
		 */
		public void RetryIn(int milliseconds) {
			RetryDelay = milliseconds;
		}

		#region Private
		internal HttpRequestFailedEventArgs(HttpRequest req, object previousUserData) {
			Url = req.Url;
			UserData = previousUserData;
		}
		internal int RetryDelay = -1;
		#endregion
	}
}
