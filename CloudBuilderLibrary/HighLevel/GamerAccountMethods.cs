using System;
using System.Text;
using System.Collections.Generic;

namespace CotcSdk
{
	public sealed class GamerAccountMethods {

		/**
		 * Changes the e-mail address of the current user. Works for e-mail type accounts.
		 * @param done callback invoked when the operation has completed, either successfully or not. The boolean inside
		 *     indicates success.
		 * @param newEmailAddress the new e-mail address to be used for signing in.
		 */
		public ResultTask<bool> ChangeEmailAddress(ResultHandler<bool> done, string newEmailAddress) {
			var task = new ResultTask<bool>();
			if (Gamer.Network != LoginNetwork.Email) {
				return task.PostResult(ErrorCode.BadParameters, "Unavailable for " + Gamer.Network.Describe() + " accounts");
			}

			Bundle config = Bundle.CreateObject();
			config["email"] = newEmailAddress;

			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/email");
			req.BodyJson = config;
			return Common.RunHandledRequest(req, task, (HttpResponse response) => {
				task.PostResult(response.BodyJson["done"], response.BodyJson);
			});
		}

		/**
		 * Changes the password of the current user. Works for e-mail type accounts.
		 * @param done callback invoked when the operation has completed, either successfully or not. The boolean inside
		 *     indicates success.
		 * @param newPassword the new password to be used for signing in.
		 */
		public ResultTask<bool> ChangePassword(string newPassword) {
			var task = new ResultTask<bool>();
			if (Gamer.Network != LoginNetwork.Email) {
				task.PostResult(ErrorCode.BadParameters, "Unavailable for " + Gamer.Network.Describe() + " accounts");
				return task;
			}

			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/password");
			req.BodyJson = Bundle.CreateObject("password", newPassword);
			return Common.RunHandledRequest(req, task, (HttpResponse response) => {
				task.PostResult(response.BodyJson["done"], response.BodyJson);
			});
		}

		/**
		 * Converts the account to sign in through another network.
		 * For instance, you might have created an anonymous account, that you later want to convert to an account
		 * logged on through a facebook account. Or, should you later want to convert this account to simply use an
		 * e-mail address, this is the method that you will want to call.
		 * In order to convert the account successfully, the provided network credentials need to be acceptable,
		 * just as when calling Cloud.Login.
		 * @param done callback invoked when the operation has completed, either successfully or not. The boolean inside
		 *     is not important.
		 * @param network the target network to connect with later on.
		 * @param networkId the ID on the network. For example, with the facebook network, this would be the User ID.
		 *     On e-mail accounts e-mail then, this would be the e-mail address.
		 * @param networkSecret the secret for the network. For e-mail accounts, this would be the passord. For
		 *     facebook or other SNS accounts, this would be the user token.
		 */
		public ResultTask<bool> Convert(LoginNetwork network, string networkId, string networkSecret) {
			var task = new ResultTask<bool>();
			Bundle config = Bundle.CreateObject();
			config["network"] = network.Describe();
			config["id"] = networkId;
			config["secret"] = networkSecret;

			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/convert");
			req.BodyJson = config;
			return Common.RunHandledRequest(req, task, (HttpResponse response) => {
				task.PostResult(response.BodyJson["done"], response.BodyJson);
			});
		}

		#region Private
		internal GamerAccountMethods(Gamer parent) {
			Gamer = parent;
		}
		private Gamer Gamer;
		#endregion
	}
}
