using System;
using System.Collections.Generic;

namespace CotcSdk {

	/**
	 * Represents a match with which you can interact through high level functionality.
	 * A match object is returned when you create a match, join it and so on.
	 * You should subscribe to ReceivedEvent right after you got this object.
	 */
	public class Match : PropertiesObject {
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
		 * List of existing events, which may be used to reproduce the state of the game.
		 */
		public List<MatchMove> Moves { get; private set; }
		// Subscribe to these events to be notified of events related to the match, such as when a move was posted.
		public event Action<Match, MatchFinishEvent> OnMatchFinished {
			add { onMatchFinished += value; CheckEventLoopNeeded(); }
			remove { onMatchFinished -= value; CheckEventLoopNeeded(); }
		}
		public event Action<Match, MatchJoinEvent> OnPlayerJoined {
			add { onPlayerJoined += value; CheckEventLoopNeeded(); }
			remove { onPlayerJoined -= value; CheckEventLoopNeeded(); }
		}
		public event Action<Match, MatchLeaveEvent> OnPlayerLeft {
			add { onPlayerLeft += value; CheckEventLoopNeeded(); }
			remove { onPlayerLeft -= value; CheckEventLoopNeeded(); }
		}
		public event Action<Match, MatchMoveEvent> OnMovePosted {
			add { onMovePosted += value; CheckEventLoopNeeded(); }
			remove { onMovePosted -= value; CheckEventLoopNeeded(); }
		}
		public event Action<Match, MatchShoeDrawnEvent> OnShoeDrawn {
			add { onShoeDrawn += value; CheckEventLoopNeeded(); }
			remove { onShoeDrawn -= value; CheckEventLoopNeeded(); }
		}
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
		 * Clears all event handlers subscribed, ensuring that a match object can be dismissed without causing further
		 * actions in the background.
		 */
		public void DiscardEventHandlers() {
			foreach (Action<Match, MatchFinishEvent> e in onMatchFinished.GetInvocationList()) onMatchFinished -= e;
			foreach (Action<Match, MatchJoinEvent> e in onPlayerJoined.GetInvocationList()) onPlayerJoined -= e;
			foreach (Action<Match, MatchLeaveEvent> e in onPlayerLeft.GetInvocationList()) onPlayerLeft -= e;
			foreach (Action<Match, MatchMoveEvent> e in onMovePosted.GetInvocationList()) onMovePosted -= e;
			foreach (Action<Match, MatchShoeDrawnEvent> e in onShoeDrawn.GetInvocationList()) onShoeDrawn -= e;
			CheckEventLoopNeeded();
		}

		/**
		 * Draws an item from the shoe.
		 * @return promise resolved when the operation has completed. The attached bundle contains an array of items drawn
		 *     from the shoe. You may do `(int)result.Value[0]` to fetch the first value as integer.
		 * @param count the number of items to draw from the shoe.
		 * @param notification a notification that can be sent to all players currently playing the match (except you).
		 */
		public Promise<DrawnItemsResult> DrawFromShoe(int count = 1, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(MatchId).Path("shoe").Path("draw");
			url.QueryParam("count", count).QueryParam("lastEventId", LastEventId);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("osn", notification != null ? notification.Data : null);
			return Common.RunInTask<DrawnItemsResult>(req, (response, task) => {
				UpdateWithServerData(response.BodyJson["match"]);
				task.PostResult(new DrawnItemsResult(response.BodyJson), response.BodyJson);
			});
		}

		/**
		 * Terminates the match. You need to be the creator of the match to perform this operation.
		 * @return promise resolved when the operation has completed.
		 * @param deleteToo if true, deletes the match if it finishes successfully or is already finished.
		 * @param notification a notification that can be sent to all players currently playing the match (except you).
		 */
		public Promise<Done> Finish(bool deleteToo = false, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(MatchId).Path("finish");
			url.QueryParam("lastEventId", LastEventId);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("osn", notification != null ? notification.Data : null);
			return Common.RunInTask<Done>(req, (response, task) => {
				UpdateWithServerData(response.BodyJson["match"]);
				// Affect match
				Status = MatchStatus.Finished;
				// Also delete match
				if (deleteToo) {
					Gamer.Matches.Delete(MatchId).ForwardTo(task);
				}
				else {
					task.PostResult(new Done(true, response.BodyJson), response.BodyJson);
				}
			});
		}

		/**
		 * Allows to invite a player to join a match. You need to be part of the match to send an invitation.
		 * This can be used to invite an opponent to a match that is not shown publicly.
		 * @return promise resolved when the operation has completed.
		 * @param playerId ID of the player to invite to the match. Player IDs can be found in the properties of the
		 *     match (GamerInfo.GamerId).
		 * @param notification a push notification that can be sent to the invitee.
		 */
		public Promise<Done> InvitePlayer(string playerId, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(MatchId).Path("invite").Path(playerId);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("osn", notification != null ? notification.Data : null);
			return Common.RunInTask<Done>(req, (response, task) => {
				UpdateWithServerData(response.BodyJson["match"]);
				task.PostResult(new Done(true, response.BodyJson), response.BodyJson);
			});
		}

		/**
		 * Leaves the match.
		 * @return promise resolved when the operation has completed.
		 * @param notification a push notification that can be sent to all players except you.
		 */
		public Promise<Done> Leave(PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(MatchId).Path("leave");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("osn", notification != null ? notification.Data : null);
			return Common.RunInTask<Done>(req, (response, task) => {
				UpdateWithServerData(response.BodyJson["match"]);
				task.PostResult(new Done(true, response.BodyJson), response.BodyJson);
			});
		}

		/**
		 * Protects the match against concurrent modification. Please look at the tutorial for more information on this
		 * subject. Basically, you should use it to protect your game state from race conditions.
		 */
		public void Lock(Action block) {
			lock (this) {
				block();
			}
		}

		/**
		 * Posts a move to other players.
		 * @return promise resolved when the operation has completed.
		 * @param moveData a freeform object indicating the move data to be posted and transfered to other players. This
		 *     move data will be kept in the events, and new players should be able to use it to reproduce the local game
		 *     state.
		 * @param updatedGameState a freeform object replacing the global game state, to be used by players who join from
		 *     now on. Passing a non null value clears the pending events in the match.
		 * @param notification a push notification that can be sent to all players except you.
		 */
		public Promise<Done> PostMove(Bundle moveData, Bundle updatedGameState = null, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(MatchId).Path("move").QueryParam("lastEventId", LastEventId);
			Bundle config = Bundle.CreateObject();
			config["move"] = moveData;
			config["globalState"] = updatedGameState;
			if (notification != null) config["osn"] = notification.Data;

			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = config;
			return Common.RunInTask<Done>(req, (response, task) => {
				UpdateWithServerData(response.BodyJson["match"]);
				// Record event
				if (updatedGameState != null) {
					Moves.Clear();
					GlobalState = updatedGameState;
				}
				Moves.Add(new MatchMove(Gamer.GamerId, moveData));
				task.PostResult(new Done(true, response.BodyJson), response.BodyJson);
			});
		}

		#region Private
		// Modify the CheckEventLoopNeeded method when adding an event!
		private Action<Match, MatchFinishEvent> onMatchFinished;
		private Action<Match, MatchJoinEvent> onPlayerJoined;
		private Action<Match, MatchLeaveEvent> onPlayerLeft;
		private Action<Match, MatchMoveEvent> onMovePosted;
		private Action<Match, MatchShoeDrawnEvent> onShoeDrawn;
		private DomainEventLoop RegisteredEventLoop;

		internal Match(Gamer gamer, Bundle serverData) : base(serverData) {
			Gamer = gamer;
			CustomProperties = Bundle.Empty;
			Moves = new List<MatchMove>();
			GlobalState = Bundle.Empty;
			Players = new List<GamerInfo>();
			Shoe = Bundle.Empty;
			UpdateWithServerData(serverData);
		}

		private void CheckEventLoopNeeded() {
			// One event registered?
			if (onMatchFinished != null || onPlayerJoined != null || onPlayerLeft != null || onMovePosted != null || onShoeDrawn != null) {
				// Register event loop if not already
				if (RegisteredEventLoop == null) {
					RegisteredEventLoop = Cotc.GetEventLoopFor(Gamer.GamerId, Domain);
					if (RegisteredEventLoop == null) {
						Common.LogWarning("No pop event loop for match " + MatchId + " on domain " + Domain + ", match events/updates will not work");
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
			// Ignore events not for us
			if (!e.Message["type"].AsString().StartsWith("match.")) return;
			Lock(() => {
				// Update last event ID
				if (e.Message["event"].Has("_id")) {
					LastEventId = e.Message["event"]["_id"];
				}

				switch (e.Message["type"].AsString()) {
					case "match.join":
						var joinEvent = new MatchJoinEvent(Gamer, e.Message);
						Players.AddRange(joinEvent.PlayersJoined);
						if (onPlayerJoined != null) onPlayerJoined(this, joinEvent);
						break;
					case "match.leave":
						var leaveEvent = new MatchLeaveEvent(Gamer, e.Message);
						foreach (var p in leaveEvent.PlayersLeft) Players.Remove(p);
						if (onPlayerLeft != null) onPlayerLeft(this, leaveEvent);
						break;
					case "match.finish":
						Status = MatchStatus.Finished;
						if (onMatchFinished != null) onMatchFinished(this, new MatchFinishEvent(Gamer, e.Message));
						break;
					case "match.move":
						var moveEvent = new MatchMoveEvent(Gamer, e.Message);
						Moves.Add(new MatchMove(moveEvent.PlayerId, moveEvent.MoveData));
						if (onMovePosted != null) onMovePosted(this, moveEvent);
						break;
					case "match.shoedraw":
						if (onShoeDrawn != null) onShoeDrawn(this, new MatchShoeDrawnEvent(Gamer, e.Message));
						break;
					case "match.invite":	// Do not notify them since we are already playing the match
						break;
					default:
						Common.LogError("Unknown match event type " + e.Message["type"]);
						break;
				}
			});
		}

		private void UpdateWithServerData(Bundle serverData) {
			Lock(() => {
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
					Moves.Clear();
					foreach (var b in serverData["events"].AsArray()) {
						if (b["type"] == "match.move") {
							Moves.Add(new MatchMove(serverData["event"]["player_id"], serverData["event"]["move"]));
						}
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
			});
		}
		#endregion
	}

	public enum MatchStatus {
		Running,
		Finished,
	}

	public class MatchMove {
		/**
		 * The data passed by the player when performing the move.
		 */
		public Bundle MoveData;
		/**
		 * The ID of the player who made the move.
		 */
		public string PlayerId;

		internal MatchMove(string playerId, Bundle moveData) {
			MoveData = moveData;
			PlayerId = playerId;
		}
	}

	/**
	 * Response resulting from a #CotcSdk.Match.DrawFromShoe call.
	 */
	public class DrawnItemsResult : PropertiesObject {
		public List<Bundle> Items;

		public DrawnItemsResult(Bundle serverData) : base(serverData) {
			Items = serverData["drawnItems"].AsArray();
		}
	}
}
