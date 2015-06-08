using System;

namespace CloudBuilderLibrary {

	public class MatchListResult {
		/**
		 * Describes the creator of the match.
		 */
		public GamerInfo Creator { get; private set; }
		/**
		 * Custom properties, as passed at the creation of the match.
		 */
		public Bundle CustomProperties { get; private set; }
		/**
		 * Description of the match, as defined by the user upon creation.
		 */
		public string Description { get; private set; }
		/**
		 * The ID of the match. Keep this for later as it is useful to continue a match.
		 */
		public string MatchId { get; private set; }
		/**
		 * Maximum number of players, as passed.
		 */
		public int MaxPlayers { get; private set; }
		/**
		 * The current state of the match (running, finished).
		 */
		public MatchStatus Status { get; private set; }

		internal MatchListResult(Bundle serverData) {
			Creator = new GamerInfo(serverData["creator"]);
			CustomProperties = serverData["customProperties"];
			Description = serverData["description"];
			MatchId = serverData["_id"];
			MaxPlayers = serverData["maxPlayers"];
			Status = Common.ParseEnum<MatchStatus>(serverData["status"]);
		}
	}
}
