using System;
using System.Collections.Generic;

namespace CotcSdk {

	public class GamerScores {


		#region Private
		internal GamerScores(Gamer parent) {
			Gamer = parent;
		}
		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		#endregion
	}

	/**
	 * Describes the possible sorting orders for the score leaderboard.
	 */
	public enum ScoreOrder {
		HighToLow, /* Highest score first, lowest score last */
		LowToHigh, /* Lowest score first, highest score last */
	}
}
