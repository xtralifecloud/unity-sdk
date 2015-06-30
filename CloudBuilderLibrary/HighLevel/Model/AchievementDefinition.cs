using System;

namespace CotcSdk {

	/**
	 * Type of achievement. The rules and the "Config" member of an AchievementDefinition depends on this.
	 */
	public enum AchievementType {
		Limit
	}

	/**
	 * Definition of an achievement. Achievements are inputted on the backoffice and are triggered under
	 * defined circumstances.
	 */
	public sealed class AchievementDefinition {
		public string Name;
		public AchievementType Type;
		public Bundle Config;
		public float Progress;
		public Bundle GameData;
		public Bundle GamerData;

		public AchievementDefinition(string name, Bundle serverData) {
			Name = name;
			Type = Common.ParseEnum<AchievementType>(serverData["type"]);
			Config = serverData["config"];
			Progress = serverData["progress"];
			GameData = serverData["gameData"];
			GamerData = serverData["gamerData"];
		}
	}

}
