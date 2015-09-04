
namespace CotcSdk {

	public class PostedGameScore {
		/// <summary>Whether the score was saved. This can be set to false if the score is not as good as a previous best for the player.</summary>
		public bool HasBeenSaved {
			get;
			private set;
		}
		/// <summary>The rank of the gamer in the leaderboard after posting this score.</summary>
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
