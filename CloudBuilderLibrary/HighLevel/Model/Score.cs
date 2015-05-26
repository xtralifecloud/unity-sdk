using System;

namespace CloudBuilderLibrary {

	/**
	 * Represents a score fetched from a leaderboard.
	 */
	public class Score {
		public GamerInfo GamerInfo { get; private set; }
		/**
		 * Info about the score (passed when posted).
		 */
		public string Info { get; private set; }
		/**
		 * Time at which the score was processed by the server (posted).
		 */
		public DateTime PostedAt { get; private set; }
		/**
		 * Actual score value.
		 */
		public long Value { get; private set; }

		#region Private
		internal Score(Bundle serverData) {
			GamerInfo = new GamerInfo(serverData);
			Info = serverData["score"]["info"];
			PostedAt = Common.ParseHttpDate(serverData["score"]["timestamp"]);
			Value = serverData["score"]["score"];
		}
		#endregion
	}
}
