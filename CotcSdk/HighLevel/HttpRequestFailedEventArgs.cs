
namespace CotcSdk {

	/// <summary>Delegate for failed HTTP requests. See Cloud.HttpRequestFailedHandler.</summary>
	public delegate void HttpRequestFailedHandler(HttpRequestFailedEventArgs e);

	/// <summary>
	/// This class is passed to the HttpRequestFailed handler as set on the Cloud.
	/// You need to do something with it, either call Abort or RetryIn else the http service will
	/// throw an exception.
	/// </summary>
	public class HttpRequestFailedEventArgs {
		/// <summary>The original URL that the request failed to reach.</summary>
		public string Url {
			get;
			private set;
		}
		/// <summary>
		/// You can set this member from the handler; in case the request fails again, this data will be set
		/// to the same value as set last time. It is always set to null when the request fails for the first time.
		/// </summary>
		public object UserData;

		/// <summary>Call this to abort the request. It won't be tried ever again.</summary>
		public void Abort() {
			RetryDelay = 0;
		}

		/// <summary>Call this to retry the request later.</summary>
		/// <param name="milliseconds">Time in which to try again. No other request will be executed during this time
		///     (they will be queued) as to respect the issuing order. Please keep this in mind when setting a
		///     high delay.</param>
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
