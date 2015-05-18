using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public sealed partial class Clan {

		/**
		 * Logs the current user in anonymously.
		 * @param done callback invoked when the login has finished, either successfully or not. The resulting Gamer
		 *     object can then be used for many purposes related to the signed in account.
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

		/**
		 * Logs the current user in, using any supported social network.
		 * @param done callback invoked when the login has finished, either successfully or not. The resulting Gamer
		 *     object can then be used for many purposes related to the signed in account.
		 * @param network the network to connect with. If an user is recognized on a given network (same network ID),
		 *     then it will be signed back in and its user data will be used.
		 * @param networkId the ID on the network. For example, with the facebook network, this would be the User ID.
		 *     On e-mail accounts e-mail then, this would be the e-mail address.
		 * @param networkSecret the secret for the network. For e-mail accounts, this would be the passord. For
		 *     facebook or other SNS accounts, this would be the user token.
		 */
		public void Login(ResultHandler<Gamer> done, LoginNetwork network, string networkId, string networkSecret, bool preventRegistration = false) {
			Bundle config = Bundle.CreateObject();
			config["network"] = network.Describe();
			config["id"] = networkId;
			config["secret"] = networkSecret;
			config["device"] = Managers.SystemFunctions.CollectDeviceInformation();
			if (preventRegistration) {
				Bundle options = Bundle.CreateObject();
				options["preventRegistration"] = preventRegistration;
				config["options"] = options;
			}

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

		/**
		 * Logs back in with existing credentials. Should be used for users who have already been logged in
		 * previously and the application has been quit for instance.
		 * @param done callback invoked when the login has finished, either successfully or not.
		 * @param gamerId credentials of the previous session (Gamer.GamerId).
		 * @param gamerSecret credentials of the previous session (Gamer.GamerSecret).
		 */
		public void ResumeSession(ResultHandler<Gamer> done, string gamerId, string gamerSecret) {
			Login(done, LoginNetwork.Anonymous, gamerId, gamerSecret);
		}

		/**
		 * Can be used to send an e-mail to a user registered by 'email' network in order to help him
		 * recover his/her password.
		 * @param done callback invoked when the operation has finished, either successfully or not.
		 * @param userEmail the user as identified by his e-mail address.
		 * @param mailSender the sender e-mail address as it will appear on the e-mail.
		 * @param mailTitle the title of the e-mail.
		 * @param mailBody the body of the mail. You should include the string [[SHORTCODE]], which will
		 *     be replaced by the generated short code.
		 */
		public void SendMailPassword(ResultHandler<bool> done, string userEmail, string mailSender, string mailTitle, string mailBody) {
			UrlBuilder url = new UrlBuilder("/v1/login").Subpath(userEmail);
			HttpRequest req = MakeUnauthenticatedHttpRequest(url);
			Bundle config = Bundle.CreateObject();
			config["from"] = mailSender;
			config["title"] = mailTitle;
			config["body"] = mailBody;
			req.BodyJson = config;

			Managers.HttpClient.Run(req, (HttpResponse response) => {
				if (Common.HasFailed(response)) {
					Common.InvokeHandler(done, response);
					return;
				}
				Common.InvokeHandler(done, response.BodyJson["done"], response.BodyJson);
			});
		}

		/**
		 * Checks that an user exists on a given network.
		 * @param done callback invoked when the request has finished, either successfully or not.
		 *     The boolean value inside indicates whether the user exists.
		 * @param networkId the ID of the user on the network, like the e-mail address.
		 */
		public void UserExists(ResultHandler<bool> done, LoginNetwork network, string networkId) {
			UrlBuilder url = new UrlBuilder("/v1/users")
				.Subpath(network.Describe()).Subpath(networkId);
			HttpRequest req = MakeUnauthenticatedHttpRequest(url);
			Managers.HttpClient.Run(req, (HttpResponse response) => {
				if (Common.HasFailed(response)) {
					Common.InvokeHandler(done, response);
					return;
				}
				Common.InvokeHandler(done, true, response.BodyJson);
			});
		}
	}
}
