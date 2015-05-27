using System;
using System.Collections.Generic;

namespace CloudBuilderLibrary {

	public abstract class MatchEvent {
		/**
		 * The unique ID of the event. Might match the last event ID of an existing match.
		 */
		public string MatchEventId { get; private set; }
		public string MatchId { get; private set; }

		/**
		 * Builds the right type of event based on the server data received.
		 */
		internal static MatchEvent Make(Bundle serverData) {
			switch (serverData["type"].AsString()) {
				case "match.join": return new MatchJoinEvent(serverData);
				case "match.leave": return new MatchLeaveEvent(serverData);
				case "match.finish": return new MatchFinishEvent(serverData);
				case "match.move": return new MatchMoveEvent(serverData);
				case "match.invite": return new MatchInviteEvent(serverData);
			}
			CloudBuilder.LogError("Unknown match event type " + serverData["type"]);
			return null;
		}

		protected MatchEvent(Bundle serverData) {
			MatchEventId = serverData["event"]["_id"];
			MatchId = serverData["event"]["match_id"];
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

		internal MatchJoinEvent(Bundle serverData) : base(serverData) {
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
		public List<GamerInfo> PlayersLeft;

		internal MatchLeaveEvent(Bundle serverData) : base(serverData) {
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

		internal MatchFinishEvent(Bundle serverData) : base(serverData) {
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

		internal MatchMoveEvent(Bundle serverData) : base(serverData) {
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

		internal MatchInviteEvent(Bundle serverData) : base(serverData) {
			Inviter = new GamerInfo(serverData["event"]["inviter"]);
		}
	}
}
