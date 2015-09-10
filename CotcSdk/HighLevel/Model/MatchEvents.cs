using System.Collections.Generic;

namespace CotcSdk {

	/// @ingroup match_classes
	/// <summary>Basis for a match event. An event is actually always one of the subclasses (Match*Event).</summary>
	public abstract class MatchEvent: PropertiesObject {
		/// <summary>The unique ID of the event. Might match the last event ID of an existing match.</summary>
		public string MatchEventId { get; private set; }
		public MatchInfo Match { get; private set; }

		protected MatchEvent(Gamer gamer, Bundle serverData) : base(serverData) {
			MatchEventId = serverData["event"]["_id"];
			Match = new MatchInfo(gamer, serverData["event"]["match_id"]);
		}
	}

	/// @ingroup match_classes
	/// <summary>
	/// Event of type match.join.
	/// Broadcasted when a player joins a match. The joining player himself doesn't receive the event.
	/// </summary>
	public class MatchJoinEvent : MatchEvent {
		/// <summary>The list of players who just joined the match.</summary>
		public List<GamerInfo> PlayersJoined = new List<GamerInfo>();

		internal MatchJoinEvent(Gamer gamer, Bundle serverData) : base(gamer, serverData) {
			foreach (Bundle b in serverData["event"]["playersJoined"].AsArray()) {
				PlayersJoined.Add(new GamerInfo(b));
			}
		}
	}

	/// @ingroup match_classes
	/// <summary>
	/// Event of type match.leave.
	/// Broadcasted when a player leaves the match. The leaving player himself doesn't receive the event.
	/// </summary>
	public class MatchLeaveEvent : MatchEvent {
		/// <summary>The list of players who just joined the match.</summary>
		public List<GamerInfo> PlayersLeft = new List<GamerInfo>();

		internal MatchLeaveEvent(Gamer gamer, Bundle serverData) : base(gamer, serverData) {
			foreach (Bundle b in serverData["event"]["playersLeft"].AsArray()) {
				PlayersLeft.Add(new GamerInfo(b));
			}
		}
	}

	/// @ingroup match_classes
	/// <summary>
	/// Event of type match.finish.
	/// Broadcasted to all participants except the one who initiated the request when a match is finished.
	/// </summary>
	public class MatchFinishEvent : MatchEvent {
		/// <summary>Whether the match has been finished.</summary>
		public bool Finished;

		internal MatchFinishEvent(Gamer gamer, Bundle serverData) : base(gamer, serverData) {
			Finished = serverData["event"]["finished"];
		}
	}

	/// @ingroup match_classes
	/// <summary>
	/// Event of type match.move.
	/// Broadcasted when a player makes a move. The player himself doesn't receive the event.
	/// </summary>
	public class MatchMoveEvent : MatchEvent {
		/// <summary>The data passed by the player when performing the move.</summary>
		public Bundle MoveData;
		/// <summary>The ID of the player who made the move.</summary>
		public string PlayerId;

		internal MatchMoveEvent(Gamer gamer, Bundle serverData) : base(gamer, serverData) {
			MoveData = serverData["event"]["move"];
			PlayerId = serverData["event"]["player_id"];
		}
	}

	/// @ingroup match_classes
	/// <summary>
	/// Event of type match.shoedraw.
	/// Broadcasted when a player draws items from the shoe. The player himself does not receive the event.
	/// </summary>
	public class MatchShoeDrawnEvent : MatchEvent {
		/// <summary>Number of items that were drawn.</summary>
		public int Count;

		internal MatchShoeDrawnEvent(Gamer gamer, Bundle serverData) : base(gamer, serverData) {
			Count = serverData["event"]["count"];
		}
	}

	/// @ingroup match_classes
	/// <summary>
	/// Event of type match.invite.
	/// Received by another player when someone invites him to the match.
	/// </summary>
	public class MatchInviteEvent : MatchEvent {
		/// <summary>Information about the player who sent the invitation.</summary>
		public GamerInfo Inviter;

		internal MatchInviteEvent(Gamer gamer, Bundle serverData) : base(gamer, serverData) {
			Inviter = new GamerInfo(serverData["event"]["inviter"]);
		}
	}
}
