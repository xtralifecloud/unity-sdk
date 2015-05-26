using System;

namespace CloudBuilderLibrary {

	public class PostedGameScore {
		/**
		 * Whether the score was saved. This can be set to false if the score is not as good as a previous best for the player.
		 */
		public bool HasBeenSaved {
			get;
			private set;
		}
		/**
		 * The rank of the gamer in the leaderboard after posting this score.
		 */
		public int Rank {
			get;
			private set;
		}

		#region Private
		internal PostedGameScore(Bundle serverData) {
			HasBeenSaved = serverData["done"];
			Rank = serverData["rank"];
		}
		#endregion
	}
}
