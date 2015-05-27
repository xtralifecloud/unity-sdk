using System;

namespace CloudBuilderLibrary {

	public class GamerMatches {

		/**
		 * Changes the domain affected by the next operations.
		 * You should typically use it this way: `gamer.Mathes.Domain("private").List(...);`
		 * @param domain domain on which to scope the matches. Default to `private` if unmodified.
		 */
		public GamerMatches Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/**
		 * Creates a match, available for join by other players. If you would like to make your match private, please read
		 * the general documentation about matches.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached Match
		 *     object allows to operate with the match.
		 * @param maxPlayers the maximum number of players who may be in the game at a time.
		 * @param description string describing the match (available for other who want to join).
		 * @param customProperties freeform object containing the properties of the match, which may be used by other players
		 *     to search for a suited match.
		 * @param shoe freeform object containing a list of objects which will be shuffled upon match creation. This offers
		 *     an easy way to make a random generator that is safe, unbiased (since made on the server) and can be verified
		 *     by all players once the game is finished. This bundle needs to be an array (use Bundle.CreateArray).
		 */
		public void Create(ResultHandler<Match> done, int maxPlayers, string description = null, Bundle customProperties = null, Bundle shoe = null) {
			if (shoe != null && shoe.Type != Bundle.DataType.Array) {
				Common.InvokeHandler(done, ErrorCode.BadParameters, "The shoe must be an array");
				return;
			}

			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").QueryParam("domain", domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Bundle config = Bundle.CreateObject();
			config["maxPlayers"] = maxPlayers;
			config["description"] = description;
			config["customProperties"] = customProperties;
			config["shoe"] = shoe;
			req.BodyJson = config;
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, new Match(response.BodyJson["match"]), response.BodyJson);
			});
		}

		/**
		 * High level method to fetch a Match object corresponding to a match which the player already belongs to.
		 * It basically allows to continue the game in the same state as when the match has initially been created
		 * or joined to, all in a transparent manner.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached Match
		 *     object allows to operate with the match.
		 * @param matchId the ID of the match to resume. It can be fetched from the Match object (MatchId).
		 */
		public void Resume(ResultHandler<Match> done, string matchId) {
			// TODO Florian
		}


		#region Private
		internal GamerMatches(Gamer gamer) {
			Gamer = gamer;
		}
		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		#endregion
	}

}
