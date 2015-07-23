using System;
using System.Text;
using System.Collections.Generic;

namespace CotcSdk
{
	public sealed partial class Cloud {

		/**
		 * Method used to check or retrieve users from Clan of the Cloud community. The domain is not taken
		 * in account for this search.
		 * @return task returning the fetched list of users. The list is paginated (see #PagedList for more
		 *     info).
		 * @param filter may contain a nickname, a display name or e-mail address.
		 * @param limit the maximum number of results to return per page.
		 * @param offset number of the first result.
		 */
		public Promise<PagedList<UserInfo>> ListUsers(string filter, int limit = 30, int offset = 0) {
			UrlBuilder url = new UrlBuilder("/v1/gamer").QueryParam("q", filter).QueryParam("limit", limit).QueryParam("skip", offset);
			HttpRequest req = MakeUnauthenticatedHttpRequest(url);
			return Common.RunInTask<PagedList<UserInfo>>(req, (response, task) => {
				PagedList<UserInfo> result = new PagedList<UserInfo>(offset, response.BodyJson["count"]);
				foreach (Bundle u in response.BodyJson["result"].AsArray()) {
					result.Add(new UserInfo(u));
				}
				// Handle pagination
				if (offset > 0) {
					result.Previous = () => ListUsers(filter, limit, offset - limit);
				}
				if (offset + result.Count < result.Total) {
					result.Next = () => ListUsers(filter, limit, offset + limit);
				}
				task.PostResult(result, response.BodyJson);
			});
		}

		/**
		 * Logs the current user in anonymously.
		 * @return task returning when the login has finished. The resulting Gamer object can then
		 *     be used for many purposes related to the signed in account.
		 */
		public Promise<Gamer> LoginAnonymously() {
			Bundle config = Bundle.CreateObject();
			config["device"] = Managers.SystemFunctions.CollectDeviceInformation();
			
			HttpRequest req = MakeUnauthenticatedHttpRequest("/v1/login/anonymous");
			req.BodyJson = config;
			return Common.RunInTask<Gamer>(req, (response, task) => {
				Gamer gamer = new Gamer(this, response.BodyJson);
				task.PostResult(gamer, response.BodyJson);
				Cotc.NotifyLoggedIn(this, gamer);
			});
		}

		/**
		 * Logs the current user in, using any supported social network.
		 * @return task returning when the login has finished. The resulting Gamer object can then
		 *     be used for many purposes related to the signed in account.
		 * @param network the network to connect with. If an user is recognized on a given network (same network ID),
		 *     then it will be signed back in and its user data will be used.
		 * @param networkId the ID on the network. For example, with the facebook network, this would be the User ID.
		 *     On e-mail accounts e-mail then, this would be the e-mail address.
		 * @param networkSecret the secret for the network. For e-mail accounts, this would be the passord. For
		 *     facebook or other SNS accounts, this would be the user token.
		 */
		public Promise<Gamer> Login(LoginNetwork network, string networkId, string networkSecret, bool preventRegistration = false) {
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
			return Common.RunInTask<Gamer>(req, (response, task) => {
				Gamer gamer = new Gamer(this, response.BodyJson);
				task.PostResult(gamer, response.BodyJson);
				Cotc.NotifyLoggedIn(this, gamer);
			});
		}

		/**
		 * Logs back in with existing credentials. Should be used for users who have already been logged in
		 * previously and the application has been quit for instance.
		 * @return task returning when the login has finished. The resulting Gamer object can then
		 *     be used for many purposes related to the signed in account.
		 * @param gamerId credentials of the previous session (Gamer.GamerId).
		 * @param gamerSecret credentials of the previous session (Gamer.GamerSecret).
		 */
		public Promise<Gamer> ResumeSession(string gamerId, string gamerSecret) {
			return Login(LoginNetwork.Anonymous, gamerId, gamerSecret);
		}

		/**
		 * Can be used to send an e-mail to a user registered by 'email' network in order to help him
		 * recover his/her password.
		 * @return promise resolved when the request has finished.
		 * @param userEmail the user as identified by his e-mail address.
		 * @param mailSender the sender e-mail address as it will appear on the e-mail.
		 * @param mailTitle the title of the e-mail.
		 * @param mailBody the body of the mail. You should include the string [[SHORTCODE]], which will
		 *     be replaced by the generated short code.
		 */
		public Promise<Done> SendResetPasswordEmail(string userEmail, string mailSender, string mailTitle, string mailBody) {
			UrlBuilder url = new UrlBuilder("/v1/login").Path(userEmail);
			HttpRequest req = MakeUnauthenticatedHttpRequest(url);
			Bundle config = Bundle.CreateObject();
			config["from"] = mailSender;
			config["title"] = mailTitle;
			config["body"] = mailBody;
			req.BodyJson = config;

			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson), response.BodyJson);
			});
		}

		/**
		 * Checks that an user exists on a given network.
		 * @return promise resolved when the user is found. If the user does not exist, it fails with
		 *     an HTTP status code of 400.
		 * @param networkId the ID of the user on the network, like the e-mail address.
		 */
		public Promise<Done> UserExists(LoginNetwork network, string networkId) {
			UrlBuilder url = new UrlBuilder("/v1/users")
				.Path(network.Describe()).Path(networkId);
			HttpRequest req = MakeUnauthenticatedHttpRequest(url);
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(true, response.BodyJson), response.BodyJson);
			});
		}
	}
}
