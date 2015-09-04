using System;


namespace CotcSdk
{
	/// <summary>Platform-specific HTTP client.</summary>
	internal interface IHttpClient
	{
		bool VerboseMode { get; set; }

		void Abort(HttpRequest request);
		void Run(HttpRequest request, Action<HttpResponse> callback);
		/// <summary>Should abort all requests.</summary>
		void Terminate();
	}
}
