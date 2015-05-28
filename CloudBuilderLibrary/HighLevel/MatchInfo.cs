using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudBuilderLibrary {

	/**
	 * Represents a basic match with less information associated than a real match.
	 * This is the kind of matches that you may find in sub-objects returned by some calls (list, etc.).
	 */
	public class MatchInfo {
		/**
		 * The ID of the match.
		 */
		public string MatchId { get; private set; }

		/**
		 * Dismisses a pending invitation for the current user and the match. Fails if the user has not been invited.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached boolean
		 *     indicates success when true.
		 */
		public void DismissInvitation(ResultHandler<bool> done) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(MatchId).Path("invitation");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "DELETE";
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, true, response.BodyJson);
			});
		}

		#region Private
		internal MatchInfo(Gamer gamer, string matchId) {
			Gamer = gamer;
			MatchId = matchId;
		}
		private Gamer Gamer;
		#endregion
	}
}
