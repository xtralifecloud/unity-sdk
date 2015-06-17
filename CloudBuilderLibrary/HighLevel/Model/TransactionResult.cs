using System;
using System.Collections.Generic;

namespace CotcSdk {

	/**
	 * Result of a transaction call. Contains the new balance (after the transaction has been
	 * executed atomically) and the list of triggered achievements.
	 */
	public sealed class TransactionResult {
		public Bundle Balance;
		public Dictionary<string, AchievementDefinition> TriggeredAchievements = new Dictionary<string,AchievementDefinition>();

		internal TransactionResult(Bundle serverData) {
			Balance = serverData["balance"];
			foreach (var pair in serverData["achievements"].AsDictionary()) {
				TriggeredAchievements[pair.Key] = new AchievementDefinition(pair.Key, pair.Value);
			}
		}
	}

}
