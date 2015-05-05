using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public enum LoginNetwork {
		Anonymous,
		Facebook,
		GooglePlus,
	}

	public sealed partial class Gamer {

		public List<string> Domains { get; private set; }
		/**
		 * Gamer credential. Use it to gain access to user related tasks.
		 */
		public string GamerId { get; private set; }
		/**
		 * Gamer credential (secret). Same purpose as GamerId, and you will need those in pair.
		 */
		public string GamerSecret { get; private set; }
		public LoginNetwork Network { get; private set; }
		public string NetworkId { get; private set; }
		public ProfileMethods Profile {
			get { return profileMethods.Get(() => new ProfileMethods(this)); }
		}
		public DateTime RegisterTime { get; private set; }

		// TODO Move somewhere
		public void FetchFacebookFriends(ResultHandler<bool> done) {
			DoFacebookRequestWithPagination((Result<Bundle> result) => {
				PostNetworkFriends(done, "facebook", result.Value);
			}, "/me/friends", Facebook.HttpMethod.GET);
		}

		// Starting point
		private void DoFacebookRequestWithPagination(ResultHandler<Bundle> done, string query, Facebook.HttpMethod method) {
			FB.API(query, method, (FBResult result) => {
				DoFacebookRequestWithPagination(done, result, Bundle.CreateArray());
			});
		}

		// Recursive
		private void DoFacebookRequestWithPagination(ResultHandler<Bundle> done, FBResult result, Bundle addDataTo) {
			if (result.Error != null) {
				Common.InvokeHandler(done, ErrorCode.SocialNetworkError, "Facebook/ Network #1");
				return;
			}

			// Gather the result from the last request
			try {
				Bundle fbResult = Bundle.FromJson(result.Text);
				List<Bundle> data = fbResult["data"].AsArray();
				foreach (Bundle element in data) {
					addDataTo.Add(element);
				}
				string nextUrl = fbResult["paging"]["next"];
				// Finished
				if (data.Count == 0 || nextUrl == null) {
					Common.InvokeHandler(done, addDataTo);
					return;
				}

				FB.API(nextUrl.Replace("https://graph.facebook.com", ""), Facebook.HttpMethod.GET, (FBResult res) => {
					DoFacebookRequestWithPagination(done, res, addDataTo);
				});
			}
			catch (Exception e) {
				CloudBuilder.Log(LogLevel.Warning, "Error decoding FB data: " + e.ToString());
				Common.InvokeHandler(done, ErrorCode.SocialNetworkError, "Decoding facebook data: " + e.Message);
				return;
			}
		}

		#region Internal
		/**
		 * Only instantiated internally.
		 * @param gamerData Gamer data as returned by our API calls (loginanonymous, etc.).
		 */
		internal Gamer(Clan parent, Bundle gamerData) {
			Clan = parent;
			Network = Common.ParseEnum<LoginNetwork>(gamerData["network"]);
			NetworkId = gamerData["networkid"];
			GamerId = gamerData["gamer_id"];
			GamerSecret = gamerData["gamer_secret"];
			RegisterTime = Common.ParseHttpDate(gamerData["registerTime"]);
			Domains = new List<string>();
			foreach (Bundle domain in gamerData["domains"].AsArray()) {
				Domains.Add(domain);
			}
		}

		internal HttpRequest MakeHttpRequest(string path) {
			HttpRequest result = Clan.MakeUnauthenticatedHttpRequest(path);
			string authInfo = GamerId + ":" + GamerSecret;
			result.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
			return result;
		}
		#endregion

		#region Private
		// FriendData contains an array of objects with first_name, last_name, name, id
		private void PostNetworkFriends(ResultHandler<bool> done, string network, Bundle friendData) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/friends").QueryParam("network", network);
			HttpRequest req = MakeHttpRequest(url);
			req.BodyJson = friendData;
			// TODO
			Managers.HttpClient.Run(req, (HttpResponse response) => {
				if (Common.HasFailed(response)) {
					Common.InvokeHandler(done, response);
					return;
				}

				Common.InvokeHandler(done, response);
			});
		}

		internal Clan Clan;
		private CachedMember<ProfileMethods> profileMethods;
		#endregion
	}
}
