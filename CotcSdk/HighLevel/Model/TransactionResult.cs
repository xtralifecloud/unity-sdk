using System.Collections.Generic;

namespace CotcSdk {

	/// @ingroup model_classes
	/// <summary>
	/// Result of a transaction call. Contains the new balance (after the transaction has been
	/// executed atomically) and the list of triggered achievements.
	/// </summary>
	public sealed class TransactionResult: PropertiesObject {
		public Bundle Balance;
		public Dictionary<string, AchievementDefinition> TriggeredAchievements = new Dictionary<string,AchievementDefinition>();

		internal TransactionResult(Bundle serverData) : base(serverData) {
			Balance = serverData["balance"];
			foreach (var pair in serverData["achievements"].AsDictionary()) {
				TriggeredAchievements[pair.Key] = new AchievementDefinition(pair.Key, pair.Value);
			}
		}
	}

}
