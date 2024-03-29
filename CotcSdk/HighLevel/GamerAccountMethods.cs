﻿using System;

namespace CotcSdk
{
	/// @ingroup gamer_classes
	/// <summary>API functions acting on an user account (convert, etc.).</summary>
	public sealed class GamerAccountMethods {

		/// <summary>Changes the e-mail address of the current user. Works for e-mail type accounts.</summary>
		/// <returns>Promise resolved when the operation has completed.</returns>
		/// <param name="newEmailAddress">The new e-mail address to be used for signing in.</param>
		public Promise<Done> ChangeEmailAddress(string newEmailAddress) {
			var task = new Promise<Done>();
			if (Gamer.Network != LoginNetwork.Email.Describe()) {
				return task.PostResult(ErrorCode.BadParameters, "Unavailable for " + Gamer.Network + " accounts");
			}

			Bundle config = Bundle.CreateObject();
			config["email"] = newEmailAddress;

			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/email");
			req.BodyJson = config;
			return Common.RunRequest(req, task, (HttpResponse response) => {
				task.PostResult(new Done(response.BodyJson));
			});
		}

		/// <summary>Changes the password of the current user. Works for e-mail type accounts.</summary>
		/// <returns>Promise resolved when the operation has completed.</returns>
		/// <param name="newPassword">The new password to be used for signing in.</param>
		public Promise<Done> ChangePassword(string newPassword) {
			var task = new Promise<Done>();
			if (Gamer.Network != LoginNetwork.Email.Describe()) {
				task.PostResult(ErrorCode.BadParameters, "Unavailable for " + Gamer.Network + " accounts");
				return task;
			}

			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/password");
			req.BodyJson = Bundle.CreateObject("password", newPassword);
			return Common.RunRequest(req, task, (HttpResponse response) => {
				task.PostResult(new Done(response.BodyJson));
			});
		}

		/// <summary>
		/// Converts the account to sign in through another network.
		/// For instance, you might have created an anonymous account, that you later want to convert to an account
		/// logged on through a facebook account. Or, should you later want to convert this account to simply use an
		/// e-mail address, this is the method that you will want to call.
		/// In order to convert the account successfully, the provided network credentials need to be acceptable,
		/// just as when calling #CotcSdk.Cloud.Login.
		/// </summary>
		/// <returns>Promise resolved when the operation has completed.</returns>
		/// <param name="network">The target network to connect with later on.</param>
		/// <param name="credentials">Contains the necessary credentials for the required network (usually contains id and
		///		secret for anonymous and email, or auth_token for platforms like google, firebase, facebook, steam, apple...</param>
		/// <param name="options">An optional JSON to customize the convert process.</param>
		public Promise<Done> Convert(string network, Bundle credentials, Bundle options = null) {
			Bundle config = Bundle.CreateObject();
			config["network"] = network;
			config["credentials"] = credentials;
            config["options"] = options;

			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/convert");
			req.BodyJson = config;
			return Common.RunInTask<Done>(req, (response, task) => {
				// Update the linked gamer instance with the additional parameters
				if (response.BodyJson.Has("gamer")) {
					Gamer.Update(response.BodyJson["gamer"]);
				}
				task.PostResult(new Done(response.BodyJson));
			});
		}

        [Obsolete("Old method to convert to a network. Better now to use the method taking the network parameter as a string")]
        public Promise<Done> Convert(LoginNetwork network, string networkId, string networkSecret)
        {
            return Convert(network.Describe(), networkId, networkSecret);
        }

        /// <summary>
        /// Links the account with another social network. Note that unlike Convert, this doesn't change the way the
        /// user would then sign in (the credentials remain the same).
        /// For instance, one may want to link their facebook account while keeping e-mail credentials in order to
        /// be able to share and play against gamers from their facebook social circle.
        /// In order to link the account successfully, the provided network credentials need to be acceptable,
        /// just as when calling #CotcSdk.Cloud.Login.
        /// </summary>
        /// <returns>Promise resolved when the operation has completed.</returns>
        /// <param name="network">The target network to link the account with.</param>
        /// <param name="networkId">The ID on the network. For example, with the facebook network, this would be the User ID.
        ///     On e-mail accounts e-mail then, this would be the e-mail address.</param>
        /// <param name="networkSecret">The secret for the network. For e-mail accounts, this would be the passord. For
        ///     facebook or other SNS accounts, this would be the user token.</param>
		/// <param name="options">An optional JSON to customize the convert process.</param>
        public Promise<Done> Link(string network, string networkId, string networkSecret, Bundle options = null)
        {
            Bundle config = Bundle.CreateObject();
            config["network"] = network;
            config["id"] = networkId;
            config["secret"] = networkSecret;
            config["options"] = options;

            HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/link");
            req.BodyJson = config;
            return Common.RunInTask<Done>(req, (response, task) => {
                task.PostResult(new Done(response.BodyJson));
            });
        }

        [Obsolete("Old method to link to a network. Better now to use the method taking the network parameter as a string")]
        public Promise<Done> Link(LoginNetwork network, string networkId, string networkSecret)
        {
            return Link(network.Describe(), networkId, networkSecret);
        }

        /// <summary>
        /// Unlinks the account with a social network.
        /// </summary>
        /// <returns>Promise resolved when the operation has completed.</returns>
        /// <param name="network">The target network to unlink the account from.</param>
        public Promise<Done> Unlink(string network)
        {
            Bundle config = Bundle.CreateObject();
            config["network"] = network;

            HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/unlink");
            req.BodyJson = config;
            return Common.RunInTask<Done>(req, (response, task) => {
                task.PostResult(new Done(response.BodyJson));
            });
        }

        [Obsolete("Old method to unlink to a network. Better now to use the method taking the network parameter as a string")]
        public Promise<Done> Unlink(LoginNetwork network)
        {
            return Unlink(network.Describe());
        }

        /// <summary>Meant to be called for push notifications.</summary>
        /// <param name="os">Operating system (should be determined by the native implementation: "ios", "android", "macos", …).</param>
        /// <param name="token">Push notification token (device specific).</param>
        public Promise<Done> RegisterDevice(string os, string token) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/device").QueryParam("os", os).QueryParam("token", token);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "POST";
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson));
			});
		}

		/// <summary>Unregisters a previously registered device (see #RegisterDevice).</summary>
		/// <param name="os">Operating system (should be determined by the native implementation: "ios", "android", "macos", …).</param>
		/// <param name="token">Push notification token (device specific).</param>
		public Promise<Done> UnregisterDevice(string os, string token) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/device").QueryParam("os", os).QueryParam("token", token);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "DELETE";
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson));
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
