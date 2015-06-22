using System;
using System.Text;
using System.Collections.Generic;

namespace CotcSdk
{
	public sealed partial class Cloud {

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
