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
		 * @param filter may contain a nickname, a displayname or e-mail address.
		 * @param limit the maximum number of results to return per page.
		 * @param offset number of the first result.
		 */
		public ResultTask<PagedList<UserInfo>> ListUsers(string filter, int limit = 30, int offset = 0) {
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
		public ResultTask<Gamer> LoginAnonymously() {
			Bundle config = Bundle.CreateObject();
			config["device"] = Managers.SystemFunctions.CollectDeviceInformation();
			
			HttpRequest req = MakeUnauthenticatedHttpRequest("/v1/login/anonymous");
			req.BodyJson = config;
			return Common.RunInTask<Gamer>(req, (response, task) => {
				Gamer gamer = new Gamer(this, response.BodyJson);
				task.PostResult(gamer, response.BodyJson);
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
		public ResultTask<Gamer> Login(LoginNetwork network, string networkId, string networkSecret, bool preventRegistration = false) {
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
			});
		}
	}
}
