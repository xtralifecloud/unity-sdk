using System;
using System.Collections.Generic;


namespace CloudBuilderLibrary
{
	/**
	 * Platform-specific HTTP client.
	 */
	internal interface IHttpClient
	{
		bool VerboseMode { get; set; }

		void Run(HttpRequest request, Action<HttpResponse> callback);
		HttpResponse RunSynchronously(HttpRequest request);
		/**
		 * Should abort all requests.
		 */
		void Terminate();
	}
}
