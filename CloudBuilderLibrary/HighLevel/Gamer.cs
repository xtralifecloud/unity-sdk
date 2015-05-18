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
		 * Converts the account to sign in through another network.
		 * For instance, you might have created an anonymous account, that you later want to convert to an account
		 * logged on through a facebook account. Or, should you later want to convert this account to simply use an
		 * e-mail address, this is the method that you will want to call.
		 * In order to convert the account successfully, the provided network credentials need to be acceptable,
		 * just as when calling Clan.Login.
		 * @param done callback invoked when the login has finished, either successfully or not. The boolean inside
		 *     is not important.
		 * @param network the target network to connect with later on.
		 * @param networkId the ID on the network. For example, with the facebook network, this would be the User ID.
		 *     On e-mail accounts e-mail then, this would be the e-mail address.
		 * @param networkSecret the secret for the network. For e-mail accounts, this would be the passord. For
		 *     facebook or other SNS accounts, this would be the user token.
		 */
		public void ConvertAccount(ResultHandler<bool> done, LoginNetwork network, string networkId, string networkSecret) {
			Bundle config = Bundle.CreateObject();
			config["network"] = network.Describe();
			config["id"] = networkId;
			config["secret"] = networkSecret;

			HttpRequest req = MakeHttpRequest("/v1/gamer/convert");
			req.BodyJson = config;
			Managers.HttpClient.Run(req, (HttpResponse response) => {
				if (Common.HasFailed(response)) {
					Common.InvokeHandler(done, response);
					return;
				}

				Common.InvokeHandler(done, true, response.BodyJson);
			});
		}

		/**
		 * Deletes a gamer account. Dangerous and not available in production.
		 * Meant to be used only with our integration tests?
		 * TODO Remove?
		 */
		public void DeleteAccount(ResultHandler<bool> done) {
			Common.InvokeHandler(done, ErrorCode.NotImplemented);
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
			Managers.HttpClient.Run(req, (HttpResponse response) => {
				if (Common.HasFailed(response)) {
					Common.InvokeHandler(done, response);
					return;
				}

				Common.InvokeHandler(done, true, response.BodyJson);
			});
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
