using System;
using System.Collections.Generic;

namespace CotcSdk {

	/// @ingroup match_classes
	/// <summary>
	/// Represents a match with which you can interact through high level functionality.
	/// A match object is returned when you create a match, join it and so on.
	/// You should subscribe to ReceivedEvent right after you got this object.
	/// </summary>
	public class Match : PropertiesObject {
		/// <summary>Describes the creator of the match.</summary>
		public GamerInfo Creator { get; private set; }
		/// <summary>Custom properties, as passed at the creation of the match.</summary>
		public Bundle CustomProperties { get; private set; }
		/// <summary>The domain to which the match belongs (default is `private`).</summary>
		public string Domain { get; private set; }
		/// <summary>Description of the match, as defined by the user upon creation.</summary>
		public string Description { get; private set; }
		/// <summary>Parent gamer object.</summary>
		public Gamer Gamer { get; private set; }
		/// <summary>The global state of the game, which may be modified using a move.</summary>
		public Bundle GlobalState { get; private set; }
		/// <summary></summary>
		/// <returns>Whether you are the creator of the match, and as such have special privileges (like the ability
		///     to finish and delete a match).</returns>
		public bool IsCreator {
			get { return Creator.GamerId == Gamer.GamerId; }
		}
		/// <summary>The ID of the last event happened during this game; keep this for later, you might need it for some calls.</summary>
		public string LastEventId { get; private set; }
		/// <summary>The ID of the match. Keep this for later as it is useful to continue a match.</summary>
		public string MatchId { get; private set; }
		/// <summary>Maximum number of players, as passed.</summary>
		public int MaxPlayers { get; private set; }
		/// <summary>List of existing events, which may be used to reproduce the state of the game.</summary>
		public List<MatchMove> Moves { get; private set; }
		/// <summary>
		/// Event raised when the match is marked as finished. As with most match events, this event is
		/// delivered to all users currently participating to the match except the user who initiated it
		/// (that is, oneself).
		/// </summary>
		public event Action<Match, MatchFinishEvent> OnMatchFinished {
			add { onMatchFinished += value; CheckEventLoopNeeded(); }
			remove { onMatchFinished -= value; CheckEventLoopNeeded(); }
		}
		/// <summary>Event raised when a player joins the match (excluding us obviously).</summary>
		public event Action<Match, MatchJoinEvent> OnPlayerJoined {
			add { onPlayerJoined += value; CheckEventLoopNeeded(); }
			remove { onPlayerJoined -= value; CheckEventLoopNeeded(); }
		}
		/// <summary>Event raised when a player leaves the match (excluding us obviously).</summary>
		public event Action<Match, MatchLeaveEvent> OnPlayerLeft {
			add { onPlayerLeft += value; CheckEventLoopNeeded(); }
			remove { onPlayerLeft -= value; CheckEventLoopNeeded(); }
		}
		/// <summary>Event raised when a move is posted by any player except us in the match.</summary>
		public event Action<Match, MatchMoveEvent> OnMovePosted {
			add { onMovePosted += value; CheckEventLoopNeeded(); }
			remove { onMovePosted -= value; CheckEventLoopNeeded(); }
		}
		/// <summary>Event raised when an element is drawn from the shoe.</summary>
		public event Action<Match, MatchShoeDrawnEvent> OnShoeDrawn {
			add { onShoeDrawn += value; CheckEventLoopNeeded(); }
			remove { onShoeDrawn -= value; CheckEventLoopNeeded(); }
		}
		/// <summary>IDs of players participating to the match, including the creator (which is reported alone there at creation).</summary>
		public List<GamerInfo> Players { get; private set; }
		/// <summary>
		/// A random seed that can be used to ensure consistent state across players of the game.
		/// This is a 31 bit number.
		/// </summary>
		public int Seed { get; private set; }
		/// <summary>The current state of the match (running, finished).</summary>
		public MatchStatus Status { get; private set; }
		/// <summary>
		/// An array of objects that are shuffled when the match starts. You can put anything you want inside and use
		/// it as values for your next game. This field is only returned when finishing a match.
		/// </summary>
		public Bundle Shoe { get; private set; }

		/// <summary>
		/// Clears all event handlers subscribed, ensuring that a match object can be dismissed without causing further
		/// actions in the background.
		/// </summary>
		public void DiscardEventHandlers() {
			foreach (Action<Match, MatchFinishEvent> e in onMatchFinished.GetInvocationList()) onMatchFinished -= e;
			foreach (Action<Match, MatchJoinEvent> e in onPlayerJoined.GetInvocationList()) onPlayerJoined -= e;
			foreach (Action<Match, MatchLeaveEvent> e in onPlayerLeft.GetInvocationList()) onPlayerLeft -= e;
			foreach (Action<Match, MatchMoveEvent> e in onMovePosted.GetInvocationList()) onMovePosted -= e;
			foreach (Action<Match, MatchShoeDrawnEvent> e in onShoeDrawn.GetInvocationList()) onShoeDrawn -= e;
			CheckEventLoopNeeded();
		}

		/// <summary>Draws an item from the shoe.</summary>
		/// <returns>Promise resolved when the operation has completed. The attached bundle contains an array of items drawn
		///     from the shoe. You may do `(int)result.Value[0]` to fetch the first value as integer.</returns>
		/// <param name="count">The number of items to draw from the shoe.</param>
		/// <param name="notification">A notification that can be sent to all players currently playing the match (except you).</param>
		public Promise<DrawnItemsResult> DrawFromShoe(int count = 1, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(MatchId).Path("shoe").Path("draw");
			url.QueryParam("count", count).QueryParam("lastEventId", LastEventId);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("osn", notification != null ? notification.Data : null);
			return Common.RunInTask<DrawnItemsResult>(req, (response, task) => {
				UpdateWithServerData(response.BodyJson["match"]);
				task.PostResult(new DrawnItemsResult(response.BodyJson));
			});
		}

		/// <summary>Terminates the match. You need to be the creator of the match to perform this operation.</summary>
		/// <returns>Promise resolved when the operation has completed.</returns>
		/// <param name="deleteToo">If true, deletes the match if it finishes successfully or is already finished.</param>
		/// <param name="notification">A notification that can be sent to all players currently playing the match (except you).</param>
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
					task.PostResult(new Done(true, response.BodyJson));
				}
			});
		}

		/// <summary>
		/// Allows to invite a player to join a match. You need to be part of the match to send an invitation.
		/// This can be used to invite an opponent to a match that is not shown publicly.
		/// </summary>
		/// <returns>Promise resolved when the operation has completed.</returns>
		/// <param name="playerId">ID of the player to invite to the match. Player IDs can be found in the properties of the
		///     match (GamerInfo.GamerId).</param>
		/// <param name="notification">A push notification that can be sent to the invitee.</param>
		public Promise<Done> InvitePlayer(string playerId, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(MatchId).Path("invite").Path(playerId);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("osn", notification != null ? notification.Data : null);
			return Common.RunInTask<Done>(req, (response, task) => {
				UpdateWithServerData(response.BodyJson["match"]);
				task.PostResult(new Done(true, response.BodyJson));
			});
		}

		/// <summary>Leaves the match.</summary>
		/// <returns>Promise resolved when the operation has completed.</returns>
		/// <param name="notification">A push notification that can be sent to all players except you.</param>
		public Promise<Done> Leave(PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(MatchId).Path("leave");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("osn", notification != null ? notification.Data : null);
			return Common.RunInTask<Done>(req, (response, task) => {
				UpdateWithServerData(response.BodyJson["match"]);
				task.PostResult(new Done(true, response.BodyJson));
			});
		}

		/// <summary>
		/// Protects the match against concurrent modification. Please look at the tutorial for more information on this
		/// subject. Basically, you should use it to protect your game state from race conditions.
		/// </summary>
		public void Lock(Action block) {
			lock (this) {
				block();
			}
		}

		/// <summary>Posts a move to other players.</summary>
		/// <returns>Promise resolved when the operation has completed.</returns>
		/// <param name="moveData">A freeform object indicating the move data to be posted and transfered to other players. This
		///     move data will be kept in the events, and new players should be able to use it to reproduce the local game
		///     state.</param>
		/// <param name="updatedGameState">A freeform object replacing the global game state, to be used by players who join from
		///     now on. Passing a non null value clears the pending events in the match.</param>
		/// <param name="notification">A push notification that can be sent to all players except you.</param>
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
				task.PostResult(new Done(true, response.BodyJson));
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

	/// @ingroup match_classes
	/// <summary>Status of a match.</summary>
	public enum MatchStatus {
		Running,
		Finished,
	}

	/// @ingroup match_classes
	/// <summary>Represents a move in a match.</summary>
	public class MatchMove {
		/// <summary>The data passed by the player when performing the move.</summary>
		public Bundle MoveData;
		/// <summary>The ID of the player who made the move.</summary>
		public string PlayerId;

		internal MatchMove(string playerId, Bundle moveData) {
			MoveData = moveData;
			PlayerId = playerId;
		}
	}

	/// @ingroup model_classes
	/// <summary>Response resulting from a #CotcSdk.Match.DrawFromShoe call.</summary>
	public class DrawnItemsResult : PropertiesObject {
		public List<Bundle> Items { get { return Props["drawnItems"].AsArray(); } }

		public DrawnItemsResult(Bundle serverData) : base(serverData) {}
	}
}
