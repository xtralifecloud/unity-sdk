using System;


namespace CotcSdk
{
	/**
	 * Platform-specific HTTP client.
	 */
	internal interface IHttpClient
	{
		bool VerboseMode { get; set; }

		void Abort(HttpRequest request);
		void Run(HttpRequest request, Action<HttpResponse> callback);
		/**
		 * Should abort all requests.
		 */
		void Terminate();
	}
}
