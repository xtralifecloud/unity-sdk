using System;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel;

namespace CloudBuilderLibrary
{
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

		/**
		 * Provides account related functions for the current gamer.
		 * @return an object allowing to manipulate the account of the current gamer.
		 */
		public GamerAccountMethods Account {
			get { return new GamerAccountMethods(this); }
		}

		/**
		 * Provides an API to manipulate game data, such as key/value or leaderboards.
		 * @return an object that allow to manipulate game specific data.
		 */
		public Game Game {
			get { return new Game(this); }
		}

		/**
		 * Returns an object that allows to manipulate the key/value system associated with this user.
		 * @return an object allowing to manipulate key/values for this user/domain.
		 */
		public GamerVfs GamerVfs {
			get { return new GamerVfs(this); }
		}

		/**
		 * When you have data about friends from another social network, you can post them using these function.
		 * This will automatically add them as a friend on CotC as they get recognized on our servers.
		 * @param done callback invoked when the login has finished, either successfully or not. The attached boolean
		 *     indicates success if true.
		 * @param network the network with which these friends are associated
		 * @param friends a list of data about the friends fetched on the social network.
		 */
		public void PostSocialNetworkFriends(ResultHandler<bool> done, LoginNetwork network, List<SocialNetworkFriend> friends) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/friends").QueryParam("network", network.Describe());
			HttpRequest req = MakeHttpRequest(url);
			Bundle friendData = Bundle.CreateArray();
			foreach (SocialNetworkFriend f in friends) {
				friendData.Add(f.ToBundle());
			}

			req.BodyJson = friendData;
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, true, response.BodyJson);
			});
		}

		/**
		 * Allows to manipulate information related to the gamer profile.
		 * @return an object that allows to read and set the profile.
		 */
		public GamerProfileMethods Profile {
			get { return new GamerProfileMethods(this); }
		}

		/**
		 * Allows to manipulate the properties of the current gamer.
		 * @return an object that allows to set, delete, etc. property values.
		 */
		public GamerProperties Properties {
			get { return new GamerProperties(this); }
		}

		/**
		 * Allows to manipulate the transactions and related achievements of an user.
		 * @return an object that allows to manipulate transactions and query achievements.
		 */
		public GamerTransactions Transactions {
			get { return new GamerTransactions(this); }
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

		internal Clan Clan;
		#endregion
	}
}
