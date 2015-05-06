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
		public DateTime RegisterTime { get; private set; }

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
		private void PostNetworkFriends(ResultHandler<int> done, string network, Bundle friendData) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/friends").QueryParam("network", network);
			HttpRequest req = MakeHttpRequest(url);
			req.BodyJson = friendData;
			// TODO
			Managers.HttpClient.Run(req, (HttpResponse response) => {
				if (Common.HasFailed(response)) {
					Common.InvokeHandler(done, response);
					return;
				}

				Common.InvokeHandler(done, friendData.AsArray().Count);
			});
		}

		internal Clan Clan;
		#endregion
	}
}
