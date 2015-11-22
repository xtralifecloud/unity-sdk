using System;
using System.Collections.Generic;

namespace CotcSdk {

	/// @ingroup gamer_classes
	/// <summary>
	/// API methods related to the friends and so on of one gamer.
	/// 
	/// You may also want to subscribe to related events (see #CotcSdk.GamerCommunity.OnFriendStatusChange).
	/// </summary>
	public class GamerCommunity {

		/// <summary>Event triggered when someone adds this gamer as a friend or changes his friendship status.</summary>
		public event Action<FriendStatusChangeEvent> OnFriendStatusChange {
			add { onFriendStatusChange += value; CheckEventLoopNeeded(); }
			remove { onFriendStatusChange -= value; CheckEventLoopNeeded(); }
		}

		/// <summary>Easy way to add a friend knowing his gamer ID inside the CotC community.</summary>
		/// <returns>Promise resolved when the operation has completed.</returns>
		/// <param name="gamerId">ID of the gamer to add as a friend (fetched using ListFriends for instance).</param>
		/// <param name="notification">Optional OS notification to be sent to indicate the player that the status has changed.</param>
		public Promise<Done> AddFriend(string gamerId, PushNotification notification = null) {
			return ChangeRelationshipStatus(gamerId, FriendRelationshipStatus.Add, notification);
		}

		/// <summary>Allows to change the relation of a friendship inside the application.</summary>
		/// <returns>Promise resolved when the operation has completed.</returns>
		/// <param name="gamerId">ID of the gamer to change the relationship (fetched using ListFriends for instance).</param>
		/// <param name="state">The new state to set.</param>
		/// <param name="notification">Optional OS notification to be sent to indicate the player that the status has changed.</param>
		public Promise<Done> ChangeRelationshipStatus(string gamerId, FriendRelationshipStatus state, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/friends").Path(domain).Path(gamerId).QueryParam("status", state.ToString().ToLower());
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("osn", notification != null ? notification.Data : null);
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson));
			});
		}

		/// <summary>
		/// Clears all event handlers subscribed, ensuring that a match object can be dismissed without causing further
		/// actions in the background.
		/// </summary>
		public void DiscardEventHandlers() {
			foreach (Action<FriendStatusChangeEvent> e in onFriendStatusChange.GetInvocationList()) onFriendStatusChange -= e;
			CheckEventLoopNeeded();
		}

		/// <summary>
		/// Changes the domain affected by the next operations.
		/// You should typically use it this way: `gamer.Community.Domain("private").ListFriends(...);`
		/// </summary>
		/// <param name="domain">Domain on which to scope the next operations.</param>
		/// <returns>This object for operation chaining.</returns>
		public GamerCommunity Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/// <summary>Method used to retrieve the application's friends of the currently logged in profile.</summary>
		/// <returns>Promise resolved when the operation has completed, with the fetched list of friends.</returns>
		/// <param name="filterBlacklisted">When set to true, restricts to blacklisted friends.</param>
		public Promise<NonpagedList<GamerInfo>> ListFriends(bool filterBlacklisted = false) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/friends").Path(domain);
			if (filterBlacklisted) url.QueryParam("status", "blacklist");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			return Common.RunInTask<NonpagedList<GamerInfo>>(req, (response, task) => {
				var result = new NonpagedList<GamerInfo>(response.BodyJson);
				foreach (Bundle f in response.BodyJson["friends"].AsArray()) {
					result.Add(new GamerInfo(f));
				}
				task.PostResult(result);
			});
		}

		/// <summary>
		/// When you have data about friends from another social network, you can post them to CotC servers using
		/// these function.
		/// This will automatically add them as a friend on CotC as they get recognized on our servers.
		/// The friends get associated to the domain of this object.
		/// Note: this function was once called PostSocialNetworkFriends but renamed due to it being misleading.
		/// </summary>
		/// <returns>Promise resolved when the operation has completed. The attached value is the same list as passed,
		///     enriched with potential information about the gamer (member #CotcSdk.SocialNetworkFriend.ClanInfo) for
		///     gamers who are already registered on CotC servers.</returns>
		/// <param name="network">The network with which these friends are associated.</param>
		/// <param name="friends">A list of data about the friends fetched on the social network.</param>
		/// <param name="automatching">If true, synchronizes the CotC friends with the list. That is, the provided
		/// social network friends become your friends on CotC as well (reported on ListFriends and such).</param>
		public Promise<SocialNetworkFriendResponse> ListNetworkFriends(LoginNetwork network, List<SocialNetworkFriend> friends, bool automatching = false) {
			var task = new Promise<SocialNetworkFriendResponse>();
			UrlBuilder url = new UrlBuilder("/v2.12/gamer/friends").Path(domain).QueryParam("network", network.Describe());
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Bundle body = Bundle.CreateObject();
			Bundle friendData = (body["friends"] = Bundle.CreateObject());
			foreach (SocialNetworkFriend f in friends) {
				friendData[f.Id] = f.ToBundle();
			}
			body["automatching"] = automatching;

			req.BodyJson = body;
			return Common.RunRequest(req, task, (HttpResponse response) => {
				task.PostResult(new SocialNetworkFriendResponse(response.BodyJson));
			});
		}

		/// <summary>
		/// Use this method to send a message to another user from your game.
		/// 
		/// Messages are sent to a specific user, in a specific domain. You can use domains to send messages
		/// across games (or use private for messages sent to your game only).
		///
		/// </summary>
		/// <returns>Promise resolved when the operation has completed.</returns>
		/// <param name="gamerId">ID of the recipient gamer.</param>
		/// <param name="eventData">JSON object representing the event to be sent. The recipient will receive it as is
		///     when subscribed to a #CotcSdk.DomainEventLoop (ReceivedEvent property). If the application is not active,
		///     the message will be queued and transmitted the next time the domain event loop is started.</param>
		/// <param name="notification">Push notification to send to the recipient player if not currently active.</param>
		public Promise<Done> SendEvent(string gamerId, Bundle eventData, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/event").Path(domain).Path(gamerId);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Bundle config = Bundle.CreateObject();
			config["type"] = "user";
			config["event"] = eventData;
			config["from"] = Gamer.GamerId;
			config["to"] = gamerId;
			config["name"] = Gamer["profile"]["displayname"];
			if (notification != null) config["osn"] = notification.Data;

			req.BodyJson = config;
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(true, response.BodyJson));
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

	/// <summary>Status of friend relationship.</summary>
	public enum FriendRelationshipStatus {
		Add,
		Blacklist,
		Forget
	}

	/// <summary>Event triggered when someone adds this gamer as a friend or changes his friendship status.</summary>
	public class FriendStatusChangeEvent {
		/// <summary>Gamer ID of the friend affected.</summary>
		public string FriendId;
		/// <summary>New relationship status.</summary>
		public FriendRelationshipStatus NewStatus;

		internal FriendStatusChangeEvent(string status, Bundle serverData) {
			FriendId = serverData["event"]["friend"];
			NewStatus = Common.ParseEnum<FriendRelationshipStatus>(status);
		}
	}
}
