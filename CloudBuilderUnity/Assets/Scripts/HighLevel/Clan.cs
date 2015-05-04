using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public sealed class Clan {

		/**
		 * Logs the current user in anonymously.
		 * @param done callback invoked when the login has finished, either successfully or not.
		 */
		public void LoginAnonymously(ResultHandler<Gamer> done) {
			Bundle config = Bundle.CreateObject();
			config["device"] = Directory.SystemFunctions.CollectDeviceInformation();
			
			HttpRequest req = MakeUnauthenticatedHttpRequest("/v1/login/anonymous");
			req.BodyJson = config;
			Directory.HttpClient.Run(req, (HttpResponse response) => {
				if (Common.HasFailed(response)) {
					Common.InvokeHandler(done, response);
					return;
				}

				Gamer gamer = new Gamer(this, response.BodyJson);
				Common.InvokeHandler(done, gamer, response.BodyJson);
			});
		}

		/**
		 * Logs back in with existing credentials. Should be used for users who have already been logged in
		 * previously and the application has been quit for instance.
		 * @param gamerId credentials of the previous session (Gamer.GamerId)
		 * @param gamerSecret credentials of the previous session (Gamer.GamerSecret)
		 */
		public void ResumeSession(ResultHandler<Gamer> done, string gamerId, string gamerSecret) {
			Bundle config = Bundle.CreateObject();
			config["network"] = "anonymous";
			config["id"] = gamerId;
			config["secret"] = gamerSecret;
			config["device"] = Directory.SystemFunctions.CollectDeviceInformation();

			HttpRequest req = MakeUnauthenticatedHttpRequest("/v1/login");
			req.BodyJson = config;
			Directory.HttpClient.Run(req, (HttpResponse response) => {
				if (Common.HasFailed(response)) {
					Common.InvokeHandler(done, response);
					return;
				}

				Gamer gamer = new Gamer(this, response.BodyJson);
				Common.InvokeHandler(done, gamer, response.BodyJson);
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
			UserAgent = String.Format(Common.UserAgent, Directory.SystemFunctions.GetOsName(), Common.SdkVersion);
		}
		#endregion

		#region Members
		private const string SdkVersion = "1";
		private string ApiKey, ApiSecret, Server;
		private int HttpTimeoutMillis;
		public int LoadBalancerCount {
			get; private set;
		}
		public int PopEventDelay {
			get; private set;
		}
		public string UserAgent {
			get; private set;
		}
		#endregion
	}
}
