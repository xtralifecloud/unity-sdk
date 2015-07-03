using System;
using System.Collections.Generic;

namespace CotcSdk
{
	/**
	 * API functions related to the achievements.
	 */
	public class GamerAchievements {

		/**
		 * Changes the domain affected by the next operations.
		 * You should typically use it this way: `gamer.Achievements.Domain("private").List(...);`
		 * @param domain domain on which to scope the next operations.
		 * @return this object for operation chaining.
		 */
		public GamerAchievements Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/**
		 * Allows to store arbitrary data for a given achievement and the current player (appears in the
		 * 'gamerData' node of achievements).
		 * @return promise resolved when the operation has completed. The attached value contains the updated definition
		 *     of the achievement.
		 * @param achName name of the achievement to update.
		 * @param data data to associate with the achievement, merged with the current data (that is, existing keys
		 *     are not affected)
		 */
		public IPromise<AchievementDefinition> AssociateData(string achName, Bundle data) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/achievements").Path(domain).Path(achName).Path("gamerdata");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = data;
			return Common.RunInTask<AchievementDefinition>(req, (response, task) => {
				task.PostResult(new AchievementDefinition(achName, response.BodyJson["achievement"]), response.BodyJson);
			});
		}

		/**
		 * Fetches information about the status of the achievements configured for this game.
		 * @return promise resolved when the operation has completed. The attached value is the list of achievements
		 *     with their current state.
		 */
		public IPromise<Dictionary<string, AchievementDefinition>> List() {
			UrlBuilder url = new UrlBuilder("/v1/gamer/achievements").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			return Common.RunInTask<Dictionary<string, AchievementDefinition>>(req, (response, task) => {
				Dictionary<string, AchievementDefinition> result = new Dictionary<string,AchievementDefinition>();
				foreach (var pair in response.BodyJson["achievements"].AsDictionary()) {
					result[pair.Key] = new AchievementDefinition(pair.Key, pair.Value);
				}
				task.PostResult(result, response.BodyJson);
			});
		}

		#region Private
		internal GamerAchievements(Gamer parent) {
			Gamer = parent;
		}
		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		#endregion
	}
}
