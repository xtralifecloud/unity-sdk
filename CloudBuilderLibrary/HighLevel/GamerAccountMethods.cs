using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public sealed class GamerAccountMethods {

		/**
		 * Changes the e-mail address of the current user. Works for e-mail type accounts.
		 * @param done callback invoked when the operation has completed, either successfully or not. The boolean inside
		 *     indicates success.
		 * @param newEmailAddress the new e-mail address to be used for signing in.
		 */
		public void ChangeEmailAddress(ResultHandler<bool> done, string newEmailAddress) {
			if (Gamer.Network != LoginNetwork.Email) {
				Common.InvokeHandler(done, ErrorCode.BadParameters, "Unavailable for " + Gamer.Network.Describe() + " accounts");
				return;
			}

			Bundle config = Bundle.CreateObject();
			config["email"] = newEmailAddress;

			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/email");
			req.BodyJson = config;
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["done"], response.BodyJson);
			});
		}

		/**
		 * Changes the password of the current user. Works for e-mail type accounts.
		 * @param done callback invoked when the operation has completed, either successfully or not. The boolean inside
		 *     indicates success.
		 * @param newPassword the new password to be used for signing in.
		 */
		public void ChangePassword(ResultHandler<bool> done, string newPassword) {
			if (Gamer.Network != LoginNetwork.Email) {
				Common.InvokeHandler(done, ErrorCode.BadParameters, "Unavailable for " + Gamer.Network.Describe() + " accounts");
				return;
			}

			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/password");
			req.BodyJson = Bundle.CreateObject("password", newPassword);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["done"], response.BodyJson);
			});
		}

		/**
		 * Converts the account to sign in through another network.
		 * For instance, you might have created an anonymous account, that you later want to convert to an account
		 * logged on through a facebook account. Or, should you later want to convert this account to simply use an
		 * e-mail address, this is the method that you will want to call.
		 * In order to convert the account successfully, the provided network credentials need to be acceptable,
		 * just as when calling Clan.Login.
		 * @param done callback invoked when the operation has completed, either successfully or not. The boolean inside
		 *     is not important.
		 * @param network the target network to connect with later on.
		 * @param networkId the ID on the network. For example, with the facebook network, this would be the User ID.
		 *     On e-mail accounts e-mail then, this would be the e-mail address.
		 * @param networkSecret the secret for the network. For e-mail accounts, this would be the passord. For
		 *     facebook or other SNS accounts, this would be the user token.
		 */
		public void Convert(ResultHandler<bool> done, LoginNetwork network, string networkId, string networkSecret) {
			Bundle config = Bundle.CreateObject();
			config["network"] = network.Describe();
			config["id"] = networkId;
			config["secret"] = networkSecret;

			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/convert");
			req.BodyJson = config;
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["done"], response.BodyJson);
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
