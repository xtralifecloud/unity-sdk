using System;
using System.Collections.Generic;

namespace CotcSdk {

	/**
	 * API methods related to the friends and so on of one gamer.
	 * 
	 * You may also want to subscribe to related events (see #OnFriendStatusChange).
	 */
	public class GamerCommunity {

		public event Action<FriendStatusChangeEvent> OnFriendStatusChange {
			add { onFriendStatusChange += value; CheckEventLoopNeeded(); }
			remove { onFriendStatusChange -= value; CheckEventLoopNeeded(); }
		}

		/**
		 * Easy way to add a friend knowing his gamer ID inside the CotC community.
		 * @return promise resolved when the operation has completed.
		 * @param gamerId ID of the gamer to add as a friend (fetched using ListFriends for instance).
		 * @param notification optional OS notification to be sent to indicate the player that the status has changed.
		 */
		public IPromise<Done> AddFriend(string gamerId, PushNotification notification = null) {
			return ChangeRelationshipStatus(gamerId, FriendRelationshipStatus.Add, notification);
		}

		/**
		 * Allows to change the relation of a friendship inside the application.
		 * @return promise resolved when the operation has completed.
		 * @param gamerId ID of the gamer to change the relationship (fetched using ListFriends for instance).
		 * @param state the new state to set.
		 * @param notification optional OS notification to be sent to indicate the player that the status has changed.
		 */
		public IPromise<Done> ChangeRelationshipStatus(string gamerId, FriendRelationshipStatus state, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/friends").Path(domain).Path(gamerId).QueryParam("status", state.ToString().ToLower());
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("osn", notification != null ? notification.Data : null);
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson), response.BodyJson);
			});
		}

		/**
		 * Clears all event handlers subscribed, ensuring that a match object can be dismissed without causing further
		 * actions in the background.
		 */
		public void DiscardEventHandlers() {
			foreach (Action<FriendStatusChangeEvent> e in onFriendStatusChange.GetInvocationList()) onFriendStatusChange -= e;
			CheckEventLoopNeeded();
		}

		/**
		 * Changes the domain affected by the next operations.
		 * You should typically use it this way: `gamer.Community.Domain("private").ListFriends(...);`
		 * @param domain domain on which to scope the next operations.
		 * @return this object for operation chaining.
		 */
		public GamerCommunity Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/**
		 * Method used to retrieve the application's friends of the currently logged in profile.
		 * @return promise resolved when the operation has completed, with the fetched list of friends.
		 * @param filterBlacklisted when set to true, restricts to blacklisted friends.
		 */
		public IPromise<List<GamerInfo>> ListFriends(bool filterBlacklisted = false) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/friends").Path(domain);
			if (filterBlacklisted) url.QueryParam("status", "blacklist");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			return Common.RunInTask<List<GamerInfo>>(req, (response, task) => {
				List<GamerInfo> result = new List<GamerInfo>();
				foreach (Bundle f in response.BodyJson["friends"].AsArray()) {
					result.Add(new GamerInfo(f));
				}
				task.PostResult(result, response.BodyJson);
			});
		}

		/**
		 * When you have data about friends from another social network, you can post them using these function.
		 * This will automatically add them as a friend on CotC as they get recognized on our servers.
		 * The friends get associated to the domain of this object.
		 * @return promise resolved when the operation has completed. The attached value is the same list as passed,
		 *     enriched with potential information about the gamer (member #SocialNetworkFriend.ClanInfo) for
		 *     gamers who are already registered on CotC servers.
		 * @param network the network with which these friends are associated
		 * @param friends a list of data about the friends fetched on the social network.
		 */
		public IPromise<SocialNetworkFriendResponse> PostSocialNetworkFriends(LoginNetwork network, List<SocialNetworkFriend> friends) {
			var task = new Promise<SocialNetworkFriendResponse>();
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/friends").Path(domain).QueryParam("network", network.Describe());
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Bundle friendData = Bundle.CreateObject();
			foreach (SocialNetworkFriend f in friends) {
				friendData[f.Id] = f.ToBundle();
			}

			req.BodyJson = friendData;
			return Common.RunRequest(req, task, (HttpResponse response) => {
				task.PostResult(new SocialNetworkFriendResponse(response.BodyJson), response.BodyJson);
			});
		}

		/**
		 * Use this method to send a message to another user from your game.
		 * 
		 * Messages are sent to a specific user, in a specific domain. You can use domains to send messages
		 * across games (or use private for messages sent to your game only).
		 * 
		 * @return promise resolved when the operation has completed.
		 * @param gamerId ID of the recipient gamer.
		 * @param eventData JSON object representing the event to be sent. The recipient will receive it as is
		 *     when subscribed to a #DomainEventLoop (ReceivedEvent property). If the application is not active,
		 *     the message will be queued and transmitted the next time the domain event loop is started.
		 * @param notification push notification to send to the recipient player if not currently active.
		 */
		public IPromise<Done> SendEvent(string gamerId, Bundle eventData, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/event").Path(domain).Path(gamerId);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = eventData;
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(true, response.BodyJson), response.BodyJson);
			});
		}

		#region Private
		internal GamerCommunity(Gamer parent) {
			Gamer = parent;
		}

		private void CheckEventLoopNeeded() {
			if (onFriendStatusChange != null) {
				// Register if needed
				if (RegisteredEventLoop == null) {
					RegisteredEventLoop = Cotc.GetEventLoopFor(Gamer.GamerId, domain);
					if (RegisteredEventLoop == null) {
						Common.LogWarning("No pop event loop for domain " + domain + ", community events will not work");
					}
					else {
						RegisteredEventLoop.ReceivedEvent += this.ReceivedLoopEvent;
					}
				}
			}
			else if (RegisteredEventLoop != null) {
				// Unregister from event loop
				RegisteredEventLoop.ReceivedEvent -= this.ReceivedLoopEvent;
				RegisteredEventLoop = null;
			}
		}

		private void ReceivedLoopEvent(DomainEventLoop sender, EventLoopArgs e) {
			string type = e.Message["type"];
			if (type.StartsWith("friend.") && onFriendStatusChange != null) {
				string status = type.Substring(7 /* friend. */);
				onFriendStatusChange(new FriendStatusChangeEvent(status, e.Message));
			}
		}

		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		private Action<FriendStatusChangeEvent> onFriendStatusChange;
		private DomainEventLoop RegisteredEventLoop;
		#endregion
	}

	public enum FriendRelationshipStatus {
		Add,
		Blacklist,
		Forget
	}

	/**
	 * Event triggered when someone adds this gamer as a friend or changes his friendship status.
	 */
	public class FriendStatusChangeEvent {
		/**
		 * Gamer ID of the friend affected.
		 */
		public string FriendId;
		/**
		 * New relationship status.
		 */
		public FriendRelationshipStatus NewStatus;

		internal FriendStatusChangeEvent(string status, Bundle serverData) {
			FriendId = serverData["event"]["friend"];
			NewStatus = Common.ParseEnum<FriendRelationshipStatus>(status);
		}
	}
}
