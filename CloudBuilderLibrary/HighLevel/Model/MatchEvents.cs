using System;
using System.Collections.Generic;

namespace CotcSdk {

	/**
	 * Basis for a match event. An event is actually always one of the subclasses (Match*Event).
	 */
	public abstract class MatchEvent: PropertiesObject {
		/**
		 * The unique ID of the event. Might match the last event ID of an existing match.
		 */
		public string MatchEventId { get; private set; }
		public MatchInfo Match { get; private set; }

		protected MatchEvent(Gamer gamer, Bundle serverData) : base(serverData) {
			MatchEventId = serverData["event"]["_id"];
			Match = new MatchInfo(gamer, serverData["event"]["match_id"]);
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

		internal MatchJoinEvent(Gamer gamer, Bundle serverData) : base(gamer, serverData) {
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

		internal MatchLeaveEvent(Gamer gamer, Bundle serverData) : base(gamer, serverData) {
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

		internal MatchFinishEvent(Gamer gamer, Bundle serverData) : base(gamer, serverData) {
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

		internal MatchMoveEvent(Gamer gamer, Bundle serverData) : base(gamer, serverData) {
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

		internal MatchInviteEvent(Gamer gamer, Bundle serverData) : base(gamer, serverData) {
			Inviter = new GamerInfo(serverData["event"]["inviter"]);
		}
	}
}
