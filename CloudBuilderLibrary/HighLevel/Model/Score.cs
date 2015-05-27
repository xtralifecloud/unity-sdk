using System;

namespace CloudBuilderLibrary {

	/**
	 * Represents a score fetched from a leaderboard.
	 */
	public class Score {
		/**
		 * Info about the gamer who posted the score.
		 * This information may not be present for some calls (calls scoped to the current user) and the member be null.
		 * Please read the documentation to find out which calls apply this policy.
		 */
		public GamerInfo GamerInfo { get; internal set; }
		/**
		 * Info about the score (passed when posted).
		 */
		public string Info { get; private set; }
		/**
		 * Time at which the score was processed by the server (posted).
		 */
		public DateTime PostedAt { get; private set; }
		/**
		 * One-based rank of this score on the board.
		 */
		public int Rank { get; private set; }
		/**
		 * Actual score value.
		 */
		public long Value { get; private set; }

		#region Private
		internal Score(Bundle serverData) {
			GamerInfo = new GamerInfo(serverData);
			Info = serverData["score"]["info"];
			PostedAt = Common.ParseHttpDate(serverData["score"]["timestamp"]);
			Rank = serverData["rank"];
			Value = serverData["score"]["score"];
		}
		internal Score(Bundle serverData, int rank)
			: this(serverData) {
			Rank = rank;
		}
		#endregion
	}
}
