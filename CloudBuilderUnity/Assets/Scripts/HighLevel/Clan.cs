using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public sealed class Clan {
		// TODO Move login methods to an own partial class?
		
		/************************************ Login methods ************************************/
		/**
		 * Logs the current user in anonymously.
		 * @param done callback invoked when the login has finished, either successfully or not.
		 */
		public void LoginAnonymously(ResultHandler<Gamer> done) {
			Bundle config = Bundle.CreateObject();
			config["device"] = Managers.SystemFunctions.CollectDeviceInformation();
			
			HttpRequest req = MakeUnauthenticatedHttpRequest("/v1/login/anonymous");
			req.BodyJson = config;
			Managers.HttpClient.Run(req, (HttpResponse response) => {
				if (Common.HasFailed(response)) {
					Common.InvokeHandler(done, response);
					return;
				}

				Gamer gamer = new Gamer(this, response.BodyJson);
				Common.InvokeHandler(done, gamer, response.BodyJson);
			});
		}

		public void Login(ResultHandler<Gamer> done, LoginNetwork network, string networkId, string networkSecret) {
			Bundle config = Bundle.CreateObject();
			config["network"] = network.ToString().ToLower();
			config["id"] = networkId;
			config["secret"] = networkSecret;
			config["device"] = Managers.SystemFunctions.CollectDeviceInformation();

			HttpRequest req = MakeUnauthenticatedHttpRequest("/v1/login");
			req.BodyJson = config;
			Managers.HttpClient.Run(req, (HttpResponse response) => {
				if (Common.HasFailed(response)) {
					Common.InvokeHandler(done, response);
					return;
				}

				Gamer gamer = new Gamer(this, response.BodyJson);
				Common.InvokeHandler(done, gamer, response.BodyJson);
			});
		}

		public void LoginWithFacebook(ResultHandler<Gamer> done) {
			FB.Login("public_profile,email,user_friends", (FBResult result) => {
				if (result.Error != null) {
					Common.InvokeHandler(done, ErrorCode.SocialNetworkError, "Facebook/ " + result.Error);
				}
				else if (!FB.IsLoggedIn) {
					Common.InvokeHandler(done, ErrorCode.LoginCanceled);
				}
				else {
					string userId = FB.UserId, token = FB.AccessToken;
					CloudBuilder.Log("Logged in through facebook");
					Login(done, LoginNetwork.Facebook, userId, token);
				}
			});
		}

		/**
		 * Logs back in with existing credentials. Should be used for users who have already been logged in
		 * previously and the application has been quit for instance.
		 * @param done callback invoked when the login has finished, either successfully or not.
		 * @param gamerId credentials of the previous session (Gamer.GamerId).
		 * @param gamerSecret credentials of the previous session (Gamer.GamerSecret).
		 */
		public void ResumeSession(ResultHandler<Gamer> done, string gamerId, string gamerSecret) {
			Bundle config = Bundle.CreateObject();
			config["network"] = "anonymous";
			config["id"] = gamerId;
			config["secret"] = gamerSecret;
			config["device"] = Managers.SystemFunctions.CollectDeviceInformation();

			HttpRequest req = MakeUnauthenticatedHttpRequest("/v1/login");
			req.BodyJson = config;
			Managers.HttpClient.Run(req, (HttpResponse response) => {
				if (Common.HasFailed(response)) {
					Common.InvokeHandler(done, response);
					return;
				}

				Gamer gamer = new Gamer(this, response.BodyJson);
				Common.InvokeHandler(done, gamer, response.BodyJson);
			});
		}

		/************************************ General purpose ************************************/
		/**
		 * Executes a "ping" request to the server. Allows to know whether the server is currently working as expected.
		 * You should hardly ever need this.
		 * @param done callback invoked when the request has finished, either successfully or not. The boolean value inside is not important.
		 */
		public void Ping(ResultHandler<bool> done) {
			HttpRequest req = MakeUnauthenticatedHttpRequest("/v1/ping");
			Managers.HttpClient.Run(req, (HttpResponse response) => {
				Result<bool> result = new Result<bool>(response);
				result.Value = Common.HasFailed(response);
				Common.InvokeHandler(done, result);
			});
		}

		#region Internal HTTP helpers
		internal HttpRequest MakeUnauthenticatedHttpRequest(string path) {
			HttpRequest result = new HttpRequest();
			if (path.StartsWith("/")) {
				result.Url = Server + path;
			}
			else {
				result.Url = path;
			}
			result.Headers["x-apikey"] = ApiKey;
			result.Headers["x-sdkversion"] = SdkVersion;
			result.Headers["x-apisecret"] = ApiSecret;
			result.TimeoutMillisec = HttpTimeoutMillis;
			return result;
		}
		#endregion

		#region Private
		internal Clan(string apiKey, string apiSecret, string environment, bool httpVerbose, int httpTimeout, int eventLoopTimeout) {
			this.ApiKey = apiKey;
			this.ApiSecret = apiSecret;
			this.Server = environment;
			LoadBalancerCount = 2;
			Managers.HttpClient.VerboseMode = httpVerbose;
			HttpTimeoutMillis = httpTimeout * 1000;
			PopEventDelay = eventLoopTimeout * 1000;
			UserAgent = String.Format(Common.UserAgent, Managers.SystemFunctions.GetOsName(), Common.SdkVersion);
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
