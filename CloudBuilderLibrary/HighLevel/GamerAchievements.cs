using System;
using System.Collections.Generic;

namespace CloudBuilderLibrary {
	public class GamerAchievements {

		/**
		 * Changes the domain affected by the next operations.
		 * You should typically use it this way: `gamer.Achievements.Domain("private").List(...);`
		 * @param domain domain on which to scope the next operations.
		 */
		public GamerAchievements Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/**
		 * High level method that allows easily to post a success. For this to work, you need to have an achievement
		 * that uses the same unit as the name of the achievement.
		 * Note: normally, achievements are earned by posting a transaction. See #GamerTransactions.
		 * @param done callback invoked when the operation has finished, either successfully or not. Corresponds 
		 */
		public void Earn(ResultHandler<bool> done, string unit, int increment = 1, Bundle gamerData = null, ) {

		}

		/**
		 * Fetches information about the status of the achievements configured for this game.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached value
		 *     is the list of achievements with their current state.
		 */
		public void List(ResultHandler<Dictionary<string, AchievementDefinition>> done) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/achievements").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Dictionary<string, AchievementDefinition> result = new Dictionary<string,AchievementDefinition>();
				foreach (var pair in response.BodyJson["achievements"].AsDictionary()) {
					result[pair.Key] = new AchievementDefinition(pair.Key, pair.Value);
				}
				Common.InvokeHandler(done, result, response.BodyJson);
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
