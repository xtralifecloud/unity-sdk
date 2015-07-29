
namespace CotcSdk
{
	/**
	 * API functions acting on an user account (convert, etc.).
	 */
	public sealed class GamerAccountMethods {

		/**
		 * Changes the e-mail address of the current user. Works for e-mail type accounts.
		 * @return promise resolved when the operation has completed.
		 * @param newEmailAddress the new e-mail address to be used for signing in.
		 */
		public Promise<Done> ChangeEmailAddress(string newEmailAddress) {
			var task = new Promise<Done>();
			if (Gamer.Network != LoginNetwork.Email) {
				return task.PostResult(ErrorCode.BadParameters, "Unavailable for " + Gamer.Network.Describe() + " accounts");
			}

			Bundle config = Bundle.CreateObject();
			config["email"] = newEmailAddress;

			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/email");
			req.BodyJson = config;
			return Common.RunRequest(req, task, (HttpResponse response) => {
				task.PostResult(new Done(response.BodyJson), response.BodyJson);
			});
		}

		/**
		 * Changes the password of the current user. Works for e-mail type accounts.
		 * @return promise resolved when the operation has completed.
		 * @param newPassword the new password to be used for signing in.
		 */
		public Promise<Done> ChangePassword(string newPassword) {
			var task = new Promise<Done>();
			if (Gamer.Network != LoginNetwork.Email) {
				task.PostResult(ErrorCode.BadParameters, "Unavailable for " + Gamer.Network.Describe() + " accounts");
				return task;
			}

			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/password");
			req.BodyJson = Bundle.CreateObject("password", newPassword);
			return Common.RunRequest(req, task, (HttpResponse response) => {
				task.PostResult(new Done(response.BodyJson), response.BodyJson);
			});
		}

		/**
		 * Converts the account to sign in through another network.
		 * For instance, you might have created an anonymous account, that you later want to convert to an account
		 * logged on through a facebook account. Or, should you later want to convert this account to simply use an
		 * e-mail address, this is the method that you will want to call.
		 * In order to convert the account successfully, the provided network credentials need to be acceptable,
		 * just as when calling Cloud.Login.
		 * @return promise resolved when the operation has completed.
		 * @param network the target network to connect with later on.
		 * @param networkId the ID on the network. For example, with the facebook network, this would be the User ID.
		 *     On e-mail accounts e-mail then, this would be the e-mail address.
		 * @param networkSecret the secret for the network. For e-mail accounts, this would be the passord. For
		 *     facebook or other SNS accounts, this would be the user token.
		 */
		public Promise<Done> Convert(LoginNetwork network, string networkId, string networkSecret) {
			Bundle config = Bundle.CreateObject();
			config["network"] = network.Describe();
			config["id"] = networkId;
			config["secret"] = networkSecret;

			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/convert");
			req.BodyJson = config;
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson), response.BodyJson);
			});
		}

		/**
		 * Meant to be called for push notifications.
		 * @param os operating system (should be determined by the native implementation: "ios", "android", "macos", …).
		 * @param token push notification token (device specific).
		 */
		public Promise<Done> RegisterDevice(string os, string token) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/device").QueryParam("os", os).QueryParam("token", token);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "POST";
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson), response.BodyJson);
			});
		}

		/**
		 * Unregisters a previously registered device (see #RegisterDevice).
		 * @param os operating system (should be determined by the native implementation: "ios", "android", "macos", …).
		 * @param token push notification token (device specific).
		 */
		public Promise<Done> UnregisterDevice(string os, string token) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/device").QueryParam("os", os).QueryParam("token", token);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "DELETE";
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson), response.BodyJson);
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
