using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public sealed partial class Clan {

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
	}
}
