using System;
using System.Collections.Generic;

namespace CloudBuilderLibrary {

	/**
	 * Represents a match with which you can interact through high level functionality.
	 * A match object is returned when you create a match, join it and so on.
	 */
	public class Match {
		/**
		 * Describes the creator of the match.
		 */
		public GamerInfo Creator { get; private set; }
		/**
		 * Custom properties, as passed at the creation of the match.
		 */
		public Bundle CustomProperties { get; private set; }
		/**
		 * The domain to which the match belongs (default is `private`).
		 */
		public string Domain { get; private set; }
		/**
		 * Description of the match, as defined by the user upon creation.
		 */
		public string Description { get; private set; }
		/**
		 * List of existing events, which may be used to reproduce the state of the game.
		 */
		public List<MatchEvent> Events { get; private set; }
		/**
		 * Parent gamer object.
		 */
		public Gamer Gamer { get; private set; }
		/**
		 * The global state of the game, which may be modified using a move.
		 */
		public Bundle GlobalState { get; private set; }
		/**
		 * @return whether you are the creator of the match, and as such have special privileges (like the ability
		 *     to finish and delete a match).
		 */
		public bool IsCreator {
			get { return Creator.GamerId == Gamer.GamerId; }
		}
		/**
		 * The ID of the last event happened during this game; keep this for later, you might need it for some calls.
		 */
		public string LastEventId { get; private set; }
		/**
		 * The ID of the match. Keep this for later as it is useful to continue a match.
		 */
		public string MatchId { get; private set; }
		/**
		 * Maximum number of players, as passed.
		 */
		public int MaxPlayers { get; private set; }
		/**
		 * IDs of players participating to the match, including the creator (which is reported alone there at creation).
		 */
		public List<GamerInfo> Players { get; private set; }
		/**
		 * A random seed that can be used to ensure consistent state across players of the game.
		 * This is a 31 bit number.
		 */
		public int Seed { get; private set; }
		/**
		 * The current state of the match (running, finished).
		 */
		public MatchStatus Status { get; private set; }
		/**
		 * An array of objects that are shuffled when the match starts. You can put anything you want inside and use
		 * it as values for your next game. This field is only returned when finishing a match.
		 */
		public Bundle Shoe { get; private set; }

		/**
		 * Draws an item from the shoe.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached bundle
		 *     contains an array of items drawn from the shoe. You may do `(int)result.Value[0]` to fetch the first
		 *     value as integer.
		 * @param count the number of items to draw from the shoe.
		 * @param notification a notification that can be sent to all players currently playing the match (except you).
		 */
		public void DrawFromShoe(ResultHandler<Bundle> done, int count = 1, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(MatchId).Path("shoe").Path("draw");
			url.QueryParam("count", count).QueryParam("lastEventId", LastEventId);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("osn", notification != null ? notification.Data : null);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				UpdateWithServerData(response.BodyJson["match"]);
				Common.InvokeHandler(done, response.BodyJson["drawnItems"], response.BodyJson);
			});
		}

		/**
		 * Termintates the match. You need to be the creator of the match to perform this operation.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached boolean
		 *     indicates success when true.
		 * @param deleteToo if true, deletes the match if it finishes successfully or is already finished.
		 * @param notification a notification that can be sent to all players currently playing the match (except you).
		 */
		public void Finish(ResultHandler<bool> done, bool deleteToo = false, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(MatchId).Path("finish");
			url.QueryParam("lastEventId", LastEventId);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("osn", notification != null ? notification.Data : null);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				UpdateWithServerData(response.BodyJson["match"]);
				// Affect match
				Events.Add(new MatchFinishEvent(this, MakeLocalEvent(Bundle.CreateObject("finished", true))));
				Status = MatchStatus.Finished;
				// Also delete match
				if (deleteToo) {
					Gamer.Matches.DeleteMatch(done, MatchId);
				}
				else {
					Common.InvokeHandler(done, true, response.BodyJson);
				}
			});
		}

		/**
		 * Allows to invite a player to join a match. You need to be part of the match to send an invitation.
		 * This can be used to invite an opponent to a match that is not shown publicly.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached boolean
		 *     indicates success when true.
		 * @param playerId ID of the player to invite to the match. Player IDs can be found in the properties of the
		 *     match (GamerInfo.GamerId).
		 * @param notification a push notification that can be sent to the invitee.
		 */
		public void InvitePlayer(ResultHandler<bool> done, string playerId, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(MatchId).Path("invite").Path(playerId);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("osn", notification != null ? notification.Data : null);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				UpdateWithServerData(response.BodyJson["match"]);
				// This event doesn't generate another last event ID
				Bundle eventData = MakeLocalEvent(Bundle.CreateObject("inviter", Bundle.CreateObject("gamer_id", Gamer.GamerId)));
				eventData["event"].Remove("_id");
				Events.Add(new MatchInviteEvent(this, eventData));
				Common.InvokeHandler(done, true, response.BodyJson);
			});
		}

		/**
		 * Leaves the match.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached boolean
		 *     indicates success when true.
		 * @param notification a push notification that can be sent to all players except you.
		 */
		public void Leave(ResultHandler<bool> done, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(MatchId).Path("leave");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("osn", notification != null ? notification.Data : null);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				UpdateWithServerData(response.BodyJson["match"]);
				Common.InvokeHandler(done, true, response.BodyJson);
			});
		}

		/**
		 * Posts a move to other players.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached boolean
		 *     indicates success when true.
		 * @param moveData a freeform object indicating the move data to be posted and transfered to other players. This
		 *     move data will be kept in the events, and new players should be able to use it to reproduce the local game
		 *     state.
		 * @param updatedGameState a freeform object replacing the global game state, to be used by players who join from
		 *     now on. Passing a non null value clears the pending events in the match.
		 * @param notification a push notification that can be sent to all players except you.
		 */
		public void PostMove(ResultHandler<bool> done, Bundle moveData, Bundle updatedGameState = null, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(MatchId).Path("move").QueryParam("lastEventId", LastEventId);
			Bundle config = Bundle.CreateObject();
			config["move"] = moveData;
			config["globalState"] = updatedGameState;
			if (notification != null) config["osn"] = notification.Data;

			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = config;
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				UpdateWithServerData(response.BodyJson["match"]);
				// Record event
				if (updatedGameState != null) {
					Events.Clear();
					GlobalState = updatedGameState;
				}
				Bundle eventData = Bundle.CreateObject("move", moveData, "player_id", Gamer.GamerId);
				Events.Add(new MatchMoveEvent(this, MakeLocalEvent(eventData)));
				Common.InvokeHandler(done, true, response.BodyJson);
			});
		}

		/**
		 * Registers an event listener linked to a running event loop.
		 * @param action the delegate to be called when an event related to the match is received.
		 * @param loop domain event loop to subscribe to. It should be the event loop matching the domain on which the
		 *     match is running. Typically Common.PrivateDomain by default.
		 */
		public void RegisterEventListener(Action<MatchEvent> action, DomainEventLoop loop) {
			EventHandlers.Add(action);
			if (!AlreadyRegisteredHandler) {
				AlreadyRegisteredHandler = true;
				loop.ReceivedEvent += ReceivedLoopEvent;
			}
		}

		/**
		 * Unregisters a previously registered event listener.
		 * @param action the delegate that was previously passed to RegisterEventListener.
		 */
		public void UnregisterEventListener(Action<MatchEvent> action) {
			if (!EventHandlers.Contains(action)) throw new ArgumentException("This event handler is not registered");
			EventHandlers.Remove(action);
		}

		#region Private
		internal Match(Gamer gamer, Bundle serverData) {
			Gamer = gamer;
			CustomProperties = Bundle.Empty;
			Events = new List<MatchEvent>();
			GlobalState = Bundle.Empty;
			Players = new List<GamerInfo>();
			Shoe = Bundle.Empty;
			UpdateWithServerData(serverData);
		}

		private Bundle MakeLocalEvent(Bundle additionalData) {
			Bundle result = Bundle.CreateObject("event", additionalData);
			additionalData["_id"] = LastEventId;
			additionalData["match_id"] = MatchId;
			return result;
		}

		private void ReceivedLoopEvent(DomainEventLoop sender, EventLoopArgs e) {
			// Ignore messages not for us
			if (!e.Message["type"].AsString().StartsWith("match.")) return;
			// Make and notify event
			MatchEvent me = MatchEvent.Make(this, e.Message);
			foreach (var action in EventHandlers) {
				action(me);
			}
		}

		private void UpdateWithServerData(Bundle serverData) {
			if (serverData.Has("creator")) Creator = new GamerInfo(serverData["creator"]);
			if (serverData.Has("customProperties")) CustomProperties = serverData["customProperties"];
			if (serverData.Has("domain")) Domain = serverData["domain"];
			if (serverData.Has("description")) Description = serverData["description"];
			if (serverData.Has("globalState")) GlobalState = serverData["globalState"];
			MatchId = serverData["_id"];
			if (serverData.Has("maxPlayers")) MaxPlayers = serverData["maxPlayers"];
			if (serverData.Has("seed")) Seed = serverData["seed"];
			Status = Common.ParseEnum<MatchStatus>(serverData["status"]);
			if (serverData.Has("shoe")) Shoe = serverData["shoe"];
			// Process pending events
			if (serverData.Has("events")) {
				Events.Clear();
				foreach (var b in serverData["events"].AsArray()) {
					MatchEvent e = MatchEvent.Make(this, b);
					if (e != null) Events.Add(e);
				}
			}
			// Players
			if (serverData.Has("players")) {
				Players.Clear();
				foreach (var b in serverData["players"].AsArray()) {
					Players.Add(new GamerInfo(b));
				}
			}
			// Last event ID (null if 0; =first time)
			string lastEvent = serverData["lastEventId"];
			if (lastEvent != "0") LastEventId = lastEvent;
		}

		private bool AlreadyRegisteredHandler = false;
		private List<Action<MatchEvent>> EventHandlers = new List<Action<MatchEvent>>();
		#endregion
	}

	public enum MatchStatus {
		Running,
		Finished,
	}
}
