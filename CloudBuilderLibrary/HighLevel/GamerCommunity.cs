using System;
using System.Collections.Generic;

namespace CotcSdk {

	public class GamerCommunity {

		/**
		 * Easy way to add a friend knowing his gamer ID inside the CotC community.
		 * @param done callback invoked when the operation has finished, either successfully or not. The boolean value indicates success.
		 * @param gamerId ID of the gamer to add as a friend (fetched using ListFriends for instance).
		 * @param notification optional OS notification to be sent to indicate the player that the status has changed.
		 */
		public void AddFriend(ResultHandler<bool> done, string gamerId, PushNotification notification = null) {
			ChangeRelationshipStatus(done, gamerId, FriendRelationshipStatus.Add, notification);
		}

		/**
		 * Allows to change the relation of a friendship inside the application.
		 * @param done callback invoked when the operation has finished, either successfully or not. The boolean value indicates success.
		 * @param gamerId ID of the gamer to change the relationship (fetched using ListFriends for instance).
		 * @param state the new state to set.
		 * @param notification optional OS notification to be sent to indicate the player that the status has changed.
		 */
		public void ChangeRelationshipStatus(ResultHandler<bool> done, string gamerId, FriendRelationshipStatus state, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/friends").Path(domain).Path(gamerId).QueryParam("status", state.ToString().ToLower());
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("osn", notification != null ? notification.Data : null);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["done"], response.BodyJson);
			});
		}

		/**
		 * Changes the domain affected by the next operations.
		 * You should typically use it this way: `gamer.Community.Domain("private").ListFriends(...);`
		 * @param domain domain on which to scope the next operations.
		 */
		public GamerCommunity Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/**
		 * Method used to retrieve the application's friends of the currently logged in profile.
		 * @param done callback invoked when the operation has finished, either successfully or not, with the fetched
		 *     list of friends.
		 * @param filterBlacklisted when set to true, restricts to blacklisted friends.
		 */
		public void ListFriends(ResultHandler<List<GamerInfo>> done, bool filterBlacklisted = false) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/friends").Path(domain);
			if (filterBlacklisted) url.QueryParam("status", "blacklist");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				List<GamerInfo> result = new List<GamerInfo>();
				foreach (Bundle f in response.BodyJson["friends"].AsArray()) {
					result.Add(new GamerInfo(f));
				}
				Common.InvokeHandler(done, result, response.BodyJson);
			});
		}

		/**
		 * When you have data about friends from another social network, you can post them using these function.
		 * This will automatically add them as a friend on CotC as they get recognized on our servers.
		 * The friends get associated to the domain of this object.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached
		 *     value is the same list as passed, enriched with potential information about the gamer (member
		 *     #SocialNetworkFriend.ClanInfo) for gamers who are already registered on CotC servers.
		 * @param network the network with which these friends are associated
		 * @param friends a list of data about the friends fetched on the social network.
		 */
		public void PostSocialNetworkFriends(ResultHandler<SocialNetworkFriendResponse> done, LoginNetwork network, List<SocialNetworkFriend> friends) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/friends").Path(domain).QueryParam("network", network.Describe());
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Bundle friendData = Bundle.CreateObject();
			foreach (SocialNetworkFriend f in friends) {
				friendData[f.Id] = f.ToBundle();
			}

			req.BodyJson = friendData;
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, new SocialNetworkFriendResponse(response.BodyJson), response.BodyJson);
			});
		}
		
		/**
		 * Use this method to send a message to another user from your game.
		 * 
		 * Messages are sent to a specific user, in a specific domain. You can use domains to send messages
		 * across games (or use private for messages sent to your game only).
		 * 
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached
		 *     boolean indicates success if true.
		 * @param gamerId ID of the recipient gamer.
		 * @param eventData JSON object representing the event to be sent. The recipient will receive it as is
		 *     when subscribed to a #DomainEventLoop (ReceivedEvent property). If the application is not active,
		 *     the message will be queued and transmitted the next time the domain event loop is started.
		 * @param notification push notification to send to the recipient player if not currently active.
		 */
		public void SendEvent(ResultHandler<bool> done, string gamerId, Bundle eventData, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/event").Path(domain).Path(gamerId);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = eventData;
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, true, response.BodyJson);
			});
		}

		#region Private
		internal GamerCommunity(Gamer parent) {
			Gamer = parent;
		}
		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		#endregion
	}

	public enum FriendRelationshipStatus {
		Add,
		Blacklist,
		Forget
	}
}
