using System;
using System.Collections.Generic;

namespace CotcSdk {

	public class GamerScores {

		/**
		 * Changes the domain affected by the next operations.
		 * You should typically use it this way: `game.Scores.Domain("private").Post(...);`
		 * @param domain domain on which to scope the next operations.
		 */
		public GamerScores Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/**
		 * Fetch the score list for a given board.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached value
		 *     describes a list of scores, and provides pagination functionality.
		 * @param board the name of the board to fetch scores from.
		 * @param limit the maximum number of results to return per page.
		 * @param offset number of the first result. Needs to be a multiple of `limit`. The special value of -1 can be used
		 * to auto-select the page where the current logged in user is located, including his score in the result. After
		 * that, you may use the paged result handler to fetch pages nearby.
		 */
		public void List(ResultHandler<PagedList<Score>> done, string board, int limit = 30, int offset = 0) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/scores").Path(domain).Path(board).QueryParam("count", limit);
			if (offset == -1) url.QueryParam("page", "me");
			else              url.QueryParam("page", offset / limit + 1);
			Common.RunHandledRequest(Gamer.MakeHttpRequest(url), done, (HttpResponse response) => {
				// Pagination computing
				Bundle boardData = response.BodyJson[board];
				int currentItems = boardData["scores"].AsArray().Count;
				int total = Math.Min(boardData["maxpage"] * limit, offset + currentItems);
				// Fetch listed scores
				PagedList<Score> scores = new PagedList<Score>(offset, total);
				int rank = boardData["rankOfFirst"];
				foreach (Bundle b in boardData["scores"].AsArray()) {
					scores.Add(new Score(b, rank++));
				}
				// Handle pagination
				if (offset > 0) {
					scores.Previous = () => List(done, board, limit, offset - limit);
				}
				if (offset + scores.Count < scores.Total) {
					scores.Next = () => List(done, board, limit, offset + limit);
				}
				Common.InvokeHandler(done, scores, response.BodyJson);
			});
		}

		/**
		 * Fetch the score list for a given board, restricting to the scores made by the friends of the current user.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached value
		 *     describes a list of scores, without pagination functionality.
		 * @param board the name of the board to fetch scores from.
		 */
		public void ListFriendScores(ResultHandler<List<Score>> done, string board) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/scores").Path(domain).Path(board).QueryParam("type", "friendscore");
			Common.RunHandledRequest(Gamer.MakeHttpRequest(url), done, (HttpResponse response) => {
				List<Score> scores = new List<Score>();
				foreach (Bundle b in response.BodyJson[board].AsArray()) {
					scores.Add(new Score(b));
				}
				Common.InvokeHandler(done, scores, response.BodyJson);
			});
		}

		/**
		 * Retrieves the best scores of this gamer, on all board he has posted one score to.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached value
		 *     contains information about the best scores of the user, indexed by board name.
		 *     IMPORTANT: in the results, the gamer information is not provided. GamerInfo is always null.
		 */
		public void ListUserBestScores(ResultHandler<Dictionary<string, Score>> done) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/bestscores").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Dictionary<string, Score> scores = new Dictionary<string, Score>();
				foreach (var pair in response.BodyJson.AsDictionary()) {
					Score s = new Score(pair.Value);
					s.GamerInfo = null;
					scores[pair.Key] = s;
				}
				Common.InvokeHandler(done, scores, response.BodyJson);
			});
		}

		/**
		 * Retrieves the rank that a given score would have on the leaderboard, without actually registering the score.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached value
		 *     contains the rank that the score would have (position in the board).
		 * @param score the score (numeric value) to check for ranking.
		 * @param board the name of the board to check the ranking against. Should match the board where a score has
		 * already been posted.
		 */
		public void GetRank(ResultHandler<int> done, long score, string board) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/scores").Path(domain).Path(board);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("score", score);
			req.Method = "PUT";
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["rank"], response.BodyJson);
			});
		}

		/**
		 * Post a score.
		 * @param done callback invoked when the operation has finished, either successfully or not.
		 *     The attached value contains the new rank of the player as well as whether the score was saved.
		 * @param score the score (numeric value) to record.
		 * @param board the name of the board to post the score to. You may have as many boards as you like for your
		 *     game, and scores are scoped between them.
		 * @param order the order for this board. As board are not configured on the server, any client can create a
		 *     board dynamically. This parameter serves as as a description for the board and is used only upon
		 *     creation (that is, the first player posting to the named board).
		 * @param scoreInfo an optional string used to describe the score made by the user.
		 * @param forceSave when set to true, the score is saved even if its value is less than the past best score
		 *     for this player.
		 */
		public void Post(ResultHandler<PostedGameScore> done, long score, string board, ScoreOrder order, string scoreInfo = null, bool forceSave = false) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/scores").Path(domain).Path(board);
			switch (order) {
				case ScoreOrder.HighToLow: url.QueryParam("order", "hightolow"); break;
				case ScoreOrder.LowToHigh: url.QueryParam("order", "lowtohigh"); break;
			}
			url.QueryParam("mayvary", forceSave);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("score", score, "info", scoreInfo);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, new PostedGameScore(response.BodyJson), response.BodyJson);
			});
		}

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
