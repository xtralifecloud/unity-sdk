using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public class Clan {

		/**
		 * Logs the current user in anonymously.
		 * @param done callback invoked when the login has finished, either successfully or not.
		 */
		public void LoginAnonymously(Action<CloudResult, User> done) {
			if (LoggedInUser != null) {
				Common.InvokeHandler(done, ErrorCode.enAlreadyLogged);
				return;
			}

			Bundle config = Bundle.CreateObject();
			config["device"] = Directory.SystemFunctions.CollectDeviceInformation();
			
			HttpRequest req = MakeUnauthenticatedHttpRequest("/v1/login/anonymous");
			req.BodyJson = config;
			Directory.HttpClient.Run(req, (HttpResponse response) => {
				CloudResult result = new CloudResult(response);
				if (response.HasFailed) {
					Common.InvokeHandler(done, result);
					return;
				}

				LoggedInUser = new User(this, result.Data);
				Common.InvokeHandler(done, result, LoggedInUser);
			});
		}

		#region Internal HTTP helpers
		internal HttpRequest MakeUnauthenticatedHttpRequest(string path) {
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
		internal Clan(string apiKey, string apiSecret, string environment, bool httpVerbose, int httpTimeout, int eventLoopTimeout) {
			this.ApiKey = apiKey;
			this.ApiSecret = apiSecret;
			this.Server = environment;
			LoadBalancerCount = 2;
			Directory.HttpClient.VerboseMode = httpVerbose;
			HttpTimeoutMillis = httpTimeout * 1000;
			PopEventDelay = eventLoopTimeout * 1000;
			NetworkIsOnline = true;
		}
		#endregion

		#region Members
		private const string SdkVersion = "1";

		private string ApiKey, ApiSecret, Server;
		private int HttpTimeoutMillis;
		public int LoadBalancerCount;
		private User LoggedInUser;
		public bool NetworkIsOnline;
		public int PopEventDelay;
		public string UserAgent = "TEMP-TODO-UA";
		#endregion
	}
}
