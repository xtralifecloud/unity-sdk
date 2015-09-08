using System.Collections.Generic;

namespace CotcSdk
{
	/// <summary>API functions related to the achievements.</summary>
	public class GamerAchievements {

		/// <summary>
		/// Changes the domain affected by the next operations.
		/// You should typically use it this way: `gamer.Achievements.Domain("private").List(...);`
		/// </summary>
		/// <param name="domain">Domain on which to scope the next operations.</param>
		/// <returns>This object for operation chaining.</returns>
		public GamerAchievements Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/// <summary>
		/// Allows to store arbitrary data for a given achievement and the current player (appears in the
		/// 'gamerData' node of achievements).
		/// </summary>
		/// <returns>Promise resolved when the operation has completed. The attached value contains the updated definition
		///     of the achievement.</returns>
		/// <param name="achName">Name of the achievement to update.</param>
		/// <param name="data">Data to associate with the achievement, merged with the current data (that is, existing keys
		///     are not affected)</param>
		public Promise<AchievementDefinition> AssociateData(string achName, Bundle data) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/achievements").Path(domain).Path(achName).Path("gamerdata");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = data;
			return Common.RunInTask<AchievementDefinition>(req, (response, task) => {
				task.PostResult(new AchievementDefinition(achName, response.BodyJson["achievement"]));
			});
		}

		/// <summary>Fetches information about the status of the achievements configured for this game.</summary>
		/// <returns>Promise resolved when the operation has completed. The attached value is the list of achievements
		///     with their current state.</returns>
		public Promise<Dictionary<string, AchievementDefinition>> List() {
			UrlBuilder url = new UrlBuilder("/v1/gamer/achievements").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			return Common.RunInTask<Dictionary<string, AchievementDefinition>>(req, (response, task) => {
				Dictionary<string, AchievementDefinition> result = new Dictionary<string,AchievementDefinition>();
				foreach (var pair in response.BodyJson["achievements"].AsDictionary()) {
					result[pair.Key] = new AchievementDefinition(pair.Key, pair.Value);
				}
				task.PostResult(result);
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
