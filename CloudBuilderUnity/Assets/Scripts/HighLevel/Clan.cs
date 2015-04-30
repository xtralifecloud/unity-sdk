using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public class Clan: ManagerBase {
		public const string DevEnvironment = "http://195.154.227.44:8000";
		public const string SandboxEnvironment = "https://sandbox-api[id].clanofthecloud.mobi";
		public const string ProdEnvironment = "https://prod-api[id].clanofthecloud.mobi";

		/**
		 * Call this at the very beginning to start using the library.
		 * @param done Called when the process has finished (most likely synchronously).
		 * @param apiKey The community key.
		 * @param apiSecret The community secret (credentials when registering to CotC).
		 * @param environment The URL of the server. Should use one of the predefined constants.
		 * @param httpVerbose Set to true to output detailed information about the requests performed to CotC servers. Can be used
		 *     for debugging, though it does pollute the logs.
		 * @param httpTimeout Sets a custom timeout for all requests in seconds. Defaults to 1 minute.
		 * @param eventLoopTimeout Sets a custom timeout in seconds for the long polling event loop. Should be used with care
		 *     and set to a high value (at least 60). Defaults to 590 (~10 min).
		 */
		public void Setup(Action done, string apiKey, string apiSecret, string environment = SandboxEnvironment, bool httpVerbose = false, int httpTimeout = DefaultTimeoutSec, int eventLoopTimeout = DefaultPopEventTimeoutSec) {
			this.ApiKey = apiKey;
			this.ApiSecret = apiSecret;
			this.Server = environment;
			LoadBalancerCount = 2;
			CloudBuilder.HttpClient.VerboseMode = httpVerbose;
			HttpTimeoutMillis = httpTimeout * 1000;
			PopEventDelay = eventLoopTimeout * 1000;
			NetworkIsOnline = true;
			Common.InvokeHandler(done);
		}

		#region Internal HTTP helpers
		internal override HttpRequest MakeUnauthenticatedHttpRequest(string path) {
			HttpRequest result = new HttpRequest();
			result.Url = Server + path;
			result.Headers["x-apikey"] = ApiKey;
			result.Headers["x-sdkversion"] = SdkVersion;
			result.Headers["x-apisecret"] = ApiSecret;
			result.TimeoutMillisec = HttpTimeoutMillis;
			return result;
        }
        #endregion

		#region Internal
		internal Clan() {}

		internal bool IsSetup {
			get { return ApiKey != null && ApiSecret != null; }
		}
		#endregion

		#region Members
		private const int DefaultTimeoutSec = 60, DefaultPopEventTimeoutSec = 590;
		private const string SdkVersion = "1";

		private string ApiKey, ApiSecret, Server;
		private int HttpTimeoutMillis;
		public int LoadBalancerCount;
		public bool NetworkIsOnline;
		public int PopEventDelay;
		public string UserAgent = "TEMP-TODO-UA";
        #endregion
	}
}
