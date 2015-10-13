using System;

namespace CotcSdk {

	/// @ingroup model_classes
	/// <summary>Represents a score fetched from a leaderboard.</summary>
	public class Score: PropertiesObject {
		/// <summary>
		/// Info about the gamer who posted the score.
		/// This information may not be present for some calls (calls scoped to the current user) and the member be null.
		/// Please read the documentation to find out which calls apply this policy.
		/// </summary>
		public GamerInfo GamerInfo { get; internal set; }
		/// <summary>Info about the score (passed when posted).</summary>
		public string Info { get; private set; }
		/// <summary>Time at which the score was processed by the server (posted).</summary>
		public DateTime PostedAt { get; private set; }
		/// <summary>One-based rank of this score on the board.</summary>
		public int Rank { get; private set; }
		/// <summary>Actual score value.</summary>
		public long Value { get; private set; }

		#region Private
		internal Score(Bundle serverData) : base(serverData) {
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
