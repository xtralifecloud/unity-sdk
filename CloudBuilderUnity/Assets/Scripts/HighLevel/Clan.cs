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
		public void LoginAnonymously(ResultHandler<User> done) {
			if (LoggedInUser != null) {
				Common.InvokeHandler(done, ErrorCode.AlreadyLoggedIn);
				return;
			}

			Bundle config = Bundle.CreateObject();
			config["device"] = Directory.SystemFunctions.CollectDeviceInformation();
			
			HttpRequest req = MakeUnauthenticatedHttpRequest("/v1/login/anonymous");
			req.BodyJson = config;
			Directory.HttpClient.Run(req, (HttpResponse response) => {
				if (response.HasFailed) {
					Common.InvokeHandler(done, response);
					return;
				}

				LoggedInUser = new User(this, response.BodyJson);
				Common.InvokeHandler(done, LoggedInUser, response.BodyJson);
			});
		}

		/**
		 * As the Clan allows only one logged in user at a time (you must call Logout before being able to log again),
		 * you can use this property to fetch the currently logged in user. You can also use it as a test: it will
		 * return null in case the user is not logged in.
		 */
		public User LoggedInUser {
			get; private set;
		}

		/**
		 * Tells whether the network is only. Only works once an user is logged.
		 */
		public bool NetworkIsOnline {
			get; private set;
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
			NetworkIsOnline = true;
		}

		/**
		 * This function may be called many times with the same value.
		 * It will only trigger the required work if the status actually changes.
		 * @param currentState state of the connection (true=up, false=down).
		 */
		internal void NotifyNetworkState(bool currentState) {
			// Nothing changes
			if (currentState == NetworkIsOnline) {
				return;
			}

			NetworkIsOnline = currentState;
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
