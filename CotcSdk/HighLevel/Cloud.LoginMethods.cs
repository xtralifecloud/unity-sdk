
using System;

namespace CotcSdk
{
	public sealed partial class Cloud {

		/// <summary>
		/// Method used to check or retrieve users from Clan of the Cloud community. The domain is not taken
		/// in account for this search.
		/// </summary>
		/// <returns>Task returning the fetched list of users. The list is paginated (see
		///     #CotcSdk.PagedList for more info).</returns>
		/// <param name="filter">May contain a nickname, a display name or e-mail address.</param>
		/// <param name="limit">The maximum number of results to return per page.</param>
		/// <param name="offset">Number of the first result.</param>
		public Promise<PagedList<UserInfo>> ListUsers(string filter, int limit = 30, int offset = 0) {
			UrlBuilder url = new UrlBuilder("/v1/gamer").QueryParamEscaped("q", filter).QueryParam("limit", limit).QueryParam("skip", offset);
			HttpRequest req = MakeUnauthenticatedHttpRequest(url);
			return Common.RunInTask<PagedList<UserInfo>>(req, (response, task) => {
				PagedList<UserInfo> result = new PagedList<UserInfo>(response.BodyJson, offset, response.BodyJson["count"]);
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
				task.PostResult(result);
			});
		}

		/// <summary>Logs the current user in anonymously.</summary>
		/// <returns>Task returning when the login has finished. The resulting Gamer object can then
		///     be used for many purposes related to the signed in account.</returns>
		/// <param name="additionalOptions">Additional options can be passed, such as `thenBatch` to execute a batch after
		///     login. Pass it as a Bundle with the additional keys.</param>
		public Promise<Gamer> LoginAnonymously(Bundle additionalOptions = null) {
            Bundle config = Bundle.CreateObject();
			config["device"] = Managers.SystemFunctions.CollectDeviceInformation();

			Bundle options = additionalOptions != null ? additionalOptions.Clone() : Bundle.CreateObject();
			if (!options.IsEmpty) config["options"] = options;

			HttpRequest req = MakeUnauthenticatedHttpRequest("/v1/login/anonymous");
			req.BodyJson = config;

            return Common.RunInTask<Gamer>(req, (response, task) => {
                Gamer gamer = new Gamer(this, response.BodyJson);
                task.PostResult(gamer);
                Cotc.NotifyLoggedIn(this, gamer);
			});
		}

        /// <summary>Logs the current user in, using any supported social network.</summary>
        /// <returns>Promise resolved when the login has finished. The resulting Gamer object can then be used for many
        ///     purposes related to the signed in account.</returns>
        /// <param name="network">The network to connect with. If an user is recognized on a given network (same network ID),
        ///     then it will be signed back in and its user data will be used.</param>
        /// <param name="networkId">The ID on the network. For example, with the facebook network, this would be the User ID.
        ///     On e-mail accounts e-mail then, this would be the e-mail address.</param>
        /// <param name="networkSecret">The secret for the network. For e-mail accounts, this would be the passord. For
        ///     facebook or other SNS accounts, this would be the user token. For the LoginNetwork.GameCenter, the password
        ///     is not used, so you may pass "n/a".</param>
        /// <param name="additionalOptions">Additional options can be passed, such as `thenBatch` to execute a batch after
        ///     login or `preventRegistration` to accept only already created accounts. Pass it as a Bundle with the additional
        ///     keys. May not override `preventRegistration` key since it is defined by the parameter of the same name. Example
		///     options Bundle's Json: `{"preventRegistration": true,
		///     "thenBatch": {"domain": "private", "name": "TestBatch", "params": {"test": true}}}`
		/// </param>
        public Promise<Gamer> Login(string network, string networkId, string networkSecret, Bundle additionalOptions = null)
        {
            Bundle config = Bundle.CreateObject();
            config["network"] = network;
            config["id"] = networkId;
            config["secret"] = networkSecret;
            config["device"] = Managers.SystemFunctions.CollectDeviceInformation();

            if (additionalOptions != null && !additionalOptions.IsEmpty) config["options"] = additionalOptions;

            HttpRequest req = MakeUnauthenticatedHttpRequest("/v1/login");
            req.BodyJson = config;
            return Common.RunInTask<Gamer>(req, (response, task) => {
                Gamer gamer = new Gamer(this, response.BodyJson);
                task.PostResult(gamer);
                Cotc.NotifyLoggedIn(this, gamer);
            });
        }

        [Obsolete ("Old method to connect to a network. Better now to use the method taking the network parameter as a string")]
        public Promise<Gamer> Login(LoginNetwork network, string networkId, string networkSecret, bool preventRegistration = false, Bundle additionalOptions = null) {
            Bundle options = additionalOptions != null ? additionalOptions.Clone() : Bundle.CreateObject();
            if (preventRegistration) options["preventRegistration"] = preventRegistration;
            return Login(network.Describe(), networkId, networkSecret, options);
		}

		/// <summary>Logs in by using a shortcode previously generated through #SendResetPasswordEmail.</summary>
		/// <param name="shortcode">The shortcode received by the user by e-mail.</param>
		/// <param name="additionalOptions">Additional options can be passed, such as `thenBatch` to execute a batch after
		///     login. Pass it as a Bundle with the additional keys</param>
		/// <returns>Promise resolved when the login has finished. The resulting Gamer object can then be used for many
		///     purposes related to the signed in account.</returns>
		public Promise<Gamer> LoginWithShortcode(string shortcode, Bundle additionalOptions = null) {
            Bundle options = additionalOptions != null ? additionalOptions.Clone() : Bundle.CreateObject();
            options["preventRegistration"] = true;
            return Login("restore", "", shortcode, options);
		}

		/// <summary>
		/// Logs back in with existing credentials. Should be used for users who have already been logged in
		/// previously and the application has been quit for instance.
		/// </summary>
		/// <returns>Task returning when the login has finished. The resulting Gamer object can then
		///     be used for many purposes related to the signed in account.</returns>
		/// <param name="gamerId">Credentials of the previous session (Gamer.GamerId).</param>
		/// <param name="gamerSecret">Credentials of the previous session (Gamer.GamerSecret).</param>
		/// <param name="additionalOptions">Additional options can be passed, such as 'thenBatch' to execute a batch after
		///     login. Pass it as a Bundle with the additional keys</param>
		public Promise<Gamer> ResumeSession(string gamerId, string gamerSecret, Bundle additionalOptions = null) {
			return Login(LoginNetwork.Anonymous.Describe(), gamerId, gamerSecret, additionalOptions);
		}

        /// <summary>
        /// Logs out a previously logged in player.
        /// </summary>
		/// <param name="gamer">The gamer to log out.</param>
        /// <returns>Promise resolved when the request has finished.</returns>
        public Promise<Done> Logout(Gamer gamer)
        {
            if (gamer == null)
            {
                var result = new Promise<Done>();
				result.PostResult(ErrorCode.BadParameters, "The provided gamer is null");
                return result;
            }

            Bundle config = new Bundle("");
            HttpRequest req = gamer.MakeHttpRequest("/v1/gamer/logout");
            req.BodyJson = config;
            return Common.RunInTask<Done>(req, (response, task) => {
                task.PostResult(new Done(true, response.BodyJson));
            });
        }

        /// <summary>
        /// Can be used to send an e-mail to a user registered by 'email' network in order to help him
        /// recover his/her password.
        /// 
        /// The user will receive an e-mail, containing a short code. This short code can be inputted in
        /// the #LoginWithShortcode method.
        /// </summary>
        /// <returns>Promise resolved when the request has finished.</returns>
        /// <param name="userEmail">The user as identified by his e-mail address.</param>
        /// <param name="mailSender">The sender e-mail address as it will appear on the e-mail.</param>
        /// <param name="mailTitle">The title of the e-mail.</param>
        /// <param name="mailBody">The body of the mail. You should include the string [[SHORTCODE]], which will
        ///     be replaced by the generated short code.</param>
        public Promise<Done> SendResetPasswordEmail(string userEmail, string mailSender, string mailTitle, string mailBody) {
			UrlBuilder url = new UrlBuilder("/v1/login").Path(userEmail);
			HttpRequest req = MakeUnauthenticatedHttpRequest(url);
			Bundle config = Bundle.CreateObject();
			config["from"] = mailSender;
			config["title"] = mailTitle;
			config["body"] = mailBody;
			req.BodyJson = config;

			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson));
			});
		}

		/// <summary>Checks that an user exists on a given network.</summary>
		/// <returns>Promise resolved when the user is found. If the user does not exist, it fails with
		///     an HTTP status code of 400.</returns>
		/// <param name="network">Network used to log in (scoping the networkId).</param>
		/// <param name="networkId">The ID of the user on the network, like the e-mail address.</param>
		public Promise<Done> UserExists(string network, string networkId) {
			UrlBuilder url = new UrlBuilder("/v1/users")
				.Path(network).Path(networkId);
			HttpRequest req = MakeUnauthenticatedHttpRequest(url);
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(true, response.BodyJson));
			});
		}

        [Obsolete("Old method to connect to check a user. Better now to use the method taking the network parameter as a string")]
        public Promise<Done> UserExists(LoginNetwork network, string networkId)
        {
            return UserExists(network.Describe(), networkId);
        }

    }
}
