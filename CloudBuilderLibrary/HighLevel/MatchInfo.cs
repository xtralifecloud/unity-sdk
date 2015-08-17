
namespace CotcSdk {

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
		 * @return promise resolved when the operation has completed.
		 */
		public Promise<Done> DismissInvitation() {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(MatchId).Path("invitation");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "DELETE";
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(true, response.BodyJson));
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
