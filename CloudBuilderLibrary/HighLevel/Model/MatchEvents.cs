using System;
using System.Collections.Generic;

namespace CloudBuilderLibrary {

	/**
	 * Basis for a match event. An event is actually always one of the subclasses (Match*Event).
	 */
	public abstract class MatchEvent {
		/**
		 * The unique ID of the event. Might match the last event ID of an existing match.
		 */
		public string MatchEventId { get; private set; }
		public MatchInfo Match { get; private set; }

		/**
		 * Builds the right type of event based on the server data received.
		 */
		internal static MatchEvent Make(Match match, Bundle serverData) {
			switch (serverData["type"].AsString()) {
				case "match.join": return new MatchJoinEvent(match, serverData);
				case "match.leave": return new MatchLeaveEvent(match, serverData);
				case "match.finish": return new MatchFinishEvent(match, serverData);
				case "match.move": return new MatchMoveEvent(match, serverData);
				case "match.invite": return new MatchInviteEvent(match, serverData);
			}
			CloudBuilder.LogError("Unknown match event type " + serverData["type"]);
			return null;
		}

		protected MatchEvent(Match match, Bundle serverData) {
			MatchEventId = serverData["event"]["_id"];
			Match = new MatchInfo(match.Gamer, serverData["event"]["match_id"]);
		}
	}

	/**
	 * Event of type match.join.
	 * Broadcasted when a player joins a match. The joining player himself doesn't receive the event.
	 */
	public class MatchJoinEvent : MatchEvent {
		/**
		 * The list of players who just joined the match.
		 */
		public List<GamerInfo> PlayersJoined = new List<GamerInfo>();

		internal MatchJoinEvent(Match match, Bundle serverData) : base(match, serverData) {
			foreach (Bundle b in serverData["event"]["playersJoined"].AsArray()) {
				PlayersJoined.Add(new GamerInfo(b));
			}
		}
	}

	/**
	 * Event of type match.leave.
	 * Broadcasted when a player leaves the match. The leaving player himself doesn't receive the event.
	 */
	public class MatchLeaveEvent : MatchEvent {
		/**
		 * The list of players who just joined the match.
		 */
		public List<GamerInfo> PlayersLeft = new List<GamerInfo>();

		internal MatchLeaveEvent(Match match, Bundle serverData) : base(match, serverData) {
			foreach (Bundle b in serverData["event"]["playersLeft"].AsArray()) {
				PlayersLeft.Add(new GamerInfo(b));
			}
		}
	}

	/**
	 * Event of type match.finish.
	 * Broadcasted to all participants except the one who initiated the request when a match is finished.
	 */
	public class MatchFinishEvent : MatchEvent {
		/**
		 * Whether the match has been finished.
		 */
		public bool Finished;

		internal MatchFinishEvent(Match match, Bundle serverData) : base(match, serverData) {
			Finished = serverData["event"]["finished"];
		}
	}

	/**
	 * Event of type match.move.
	 * Broadcasted when a player makes a move. The player himself doesn't receive the event.
	 */
	public class MatchMoveEvent : MatchEvent {
		/**
		 * The data passed by the player when performing the move.
		 */
		public Bundle MoveData;
		/**
		 * The ID of the player who made the move.
		 */
		public string PlayerId;

		internal MatchMoveEvent(Match match, Bundle serverData) : base(match, serverData) {
			MoveData = serverData["event"]["move"];
			PlayerId = serverData["event"]["player_id"];
		}
	}

	/**
	 * Event of type match.invite.
	 * Received by another player when someone invites him to the match.
	 */
	public class MatchInviteEvent : MatchEvent {
		/**
		 * Information about the player who sent the invitation.
		 */
		public GamerInfo Inviter;

		internal MatchInviteEvent(Match match, Bundle serverData) : base(match, serverData) {
			Inviter = new GamerInfo(serverData["event"]["inviter"]);
		}
	}
}
