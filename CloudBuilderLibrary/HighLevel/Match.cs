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
		 * @param done callback invoked when the operation has finished, either successfully or not.
		 */
		public void DrawFromShoe(Result<Bundle> done) {
			// TODO
		}

		/**
		 * Allows to invite a player to join a match. You need to be part of the match to send an invitation.
		 * This can be used to invite an opponent to a match that is not shown publicly.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached boolean
		 *     indicates success when true.
		 * @param playerId ID of the player to invite to the match. Player IDs 
		 */
		public void InvitePlayer(ResultHandler<bool> done, string playerId, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(MatchId).Path("invite").Path(playerId);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("osn", notification != null ? notification.Data : null);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				UpdateWithServerData(response.BodyJson["match"]);
				Common.InvokeHandler(done, true, response.BodyJson);
			});
		}

		#region Private
		internal Match(Gamer gamer, Bundle serverData) {
			Gamer = gamer;
			UpdateWithServerData(serverData);
		}

		private void UpdateWithServerData(Bundle serverData) {
			Creator = new GamerInfo(serverData["creator"]);
			CustomProperties = serverData["customProperties"];
			Domain = serverData["domain"];
			Description = serverData["description"];
			Events = new List<MatchEvent>();
			GlobalState = serverData["globalState"];
			MatchId = serverData["_id"];
			MaxPlayers = serverData["maxPlayers"];
			Players = new List<GamerInfo>();
			Seed = serverData["seed"];
			Status = Common.ParseEnum<MatchStatus>(serverData["status"]);
			Shoe = serverData["shoe"];
			// Process pending events
			foreach (var b in serverData["events"].AsArray()) {
				MatchEvent e = MatchEvent.Make(this, b);
				if (e != null) Events.Add(e);
			}
			// Players
			foreach (var b in serverData["players"].AsArray()) {
				Players.Add(new GamerInfo(b));
			}
			// Last event ID (null if 0; =first time)
			string lastEvent = serverData["lastEventId"];
			if (lastEvent != "0") LastEventId = lastEvent;
		}
		#endregion
	}

	public enum MatchStatus {
		Running,
		Finished,
	}
}
