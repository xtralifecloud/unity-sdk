
namespace CotcSdk
{
	public sealed partial class Cloud {

		/// <summary>
		/// Method used to check or retrieve users from Clan of the Cloud community. The domain is not taken
		/// in account for this search.
		/// </summary>
		/// <returns>Task returning the fetched list of users. The list is paginated (see
		///     #CotcSdk.PagedList<DataType> for more info).</returns>
		/// <param name="filter">May contain a nickname, a display name or e-mail address.</param>
		/// <param name="limit">The maximum number of results to return per page.</param>
		/// <param name="offset">Number of the first result.</param>
		public Promise<PagedList<UserInfo>> ListUsers(string filter, int limit = 30, int offset = 0) {
			UrlBuilder url = new UrlBuilder("/v1/gamer").QueryParam("q", filter).QueryParam("limit", limit).QueryParam("skip", offset);
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
		public Promise<Gamer> LoginAnonymously() {
			Bundle config = Bundle.CreateObject();
			config["device"] = Managers.SystemFunctions.CollectDeviceInformation();
			
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
		///     facebook or other SNS accounts, this would be the user token.</param>
		/// <param name="preventRegistration">Fail instead of silently creating an account in case it doesn't already exist on
		///     the CotC servers.</param>
		public Promise<Gamer> Login(LoginNetwork network, string networkId, string networkSecret, bool preventRegistration = false, Bundle thenBatch = null) {
			return Login(network.Describe(), networkId, networkSecret, preventRegistration, thenBatch);
		}

        /// <summary>Logs in by using a shortcode previously generated through #SendResetPasswordEmail.</summary>
        /// <param name="shortcode">The shortcode received by the user by e-mail.</param>
        /// <param name="thenBatch">Batch executed after login.</param>
        /// <returns>Promise resolved when the login has finished. The resulting Gamer object can then be used for many
        ///     purposes related to the signed in account.</returns>
        public Promise<Gamer> LoginWithShortcode(string shortcode, Bundle thenBatch = null) {
			return Login("restore", "", shortcode, true, thenBatch);
		}

        /// <summary>
        /// Logs back in with existing credentials. Should be used for users who have already been logged in
        /// previously and the application has been quit for instance.
        /// </summary>
        /// <returns>Task returning when the login has finished. The resulting Gamer object can then
        ///     be used for many purposes related to the signed in account.</returns>
        /// <param name="gamerId">Credentials of the previous session (Gamer.GamerId).</param>
        /// <param name="gamerSecret">Credentials of the previous session (Gamer.GamerSecret).</param>
        /// <param name="thenBatch">Batch executed after login.</param>
        public Promise<Gamer> ResumeSession(string gamerId, string gamerSecret, Bundle thenBatch = null) {
			return Login(LoginNetwork.Anonymous, gamerId, gamerSecret, thenBatch);
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
		public Promise<Done> UserExists(LoginNetwork network, string networkId) {
			UrlBuilder url = new UrlBuilder("/v1/users")
				.Path(network.Describe()).Path(networkId);
			HttpRequest req = MakeUnauthenticatedHttpRequest(url);
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(true, response.BodyJson));
			});
		}

		#region Private
		// See the public Login method for more info
		private Promise<Gamer> Login(string network, string networkId, string networkSecret, bool preventRegistration = false, Bundle thenBatch = null) {
			Bundle config = Bundle.CreateObject();
			config["network"] = network;
			config["id"] = networkId;
			config["secret"] = networkSecret;
			config["device"] = Managers.SystemFunctions.CollectDeviceInformation();
			if (preventRegistration) {
				Bundle options = Bundle.CreateObject();
				options["preventRegistration"] = preventRegistration;
                if(thenBatch != null)
				    options["thenBacth"] = thenBatch;
				config["options"] = options;
			}

			HttpRequest req = MakeUnauthenticatedHttpRequest("/v1/login");
			req.BodyJson = config;
			return Common.RunInTask<Gamer>(req, (response, task) => {
				Gamer gamer = new Gamer(this, response.BodyJson);
				task.PostResult(gamer);
				Cotc.NotifyLoggedIn(this, gamer);
			});
		}
		#endregion
	}
}
