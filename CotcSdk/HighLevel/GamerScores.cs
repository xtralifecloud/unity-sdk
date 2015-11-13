using System;
using System.Collections.Generic;

namespace CotcSdk {

	/// @ingroup gamer_classes
	/// <summary>Scoring and leaderboard related functions.</summary>
	public class GamerScores {

		/// <summary>
		/// Changes the domain affected by the next operations.
		/// You should typically use it this way: `game.Scores.Domain("private").Post(...);`
		/// </summary>
		/// <param name="domain">Domain on which to scope the next operations.</param>
		/// <returns>This object for operation chaining.</returns>
		public GamerScores Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/// <summary>Fetch the score list for a given board.</summary>
		/// <returns>Promise resolved when the operation has completed. The attached value describes a list of scores, and
		///     provides pagination functionality.</returns>
		/// <param name="board">The name of the board to fetch scores from.</param>
		/// <param name="limit">The maximum number of results to return per page.</param>
		/// <param name="offset">Number of the first result. Needs to be a multiple of `limit`. The special value of -1 can be used
		///     to auto-select the page where the current logged in user is located, including his score in the result. After
		///     that, you may use the paged result handler to fetch pages nearby.</param>
		public Promise<PagedList<Score>> List(string board, int limit = 30, int offset = 0) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/scores").Path(domain).Path(board).QueryParam("count", limit);
			if (offset == -1) url.QueryParam("page", "me");
			else              url.QueryParam("page", offset / limit + 1);
			return Common.RunInTask<PagedList<Score>>(Gamer.MakeHttpRequest(url), (response, task) => {
				// Pagination computing
				Bundle boardData = response.BodyJson[board];
				int currentItems = boardData["scores"].AsArray().Count;
				int total = Math.Min(boardData["maxpage"] * limit, offset + currentItems);
				// Fetch listed scores
				PagedList<Score> scores = new PagedList<Score>(response.BodyJson, offset, total);
				int rank = boardData["rankOfFirst"];
				foreach (Bundle b in boardData["scores"].AsArray()) {
					scores.Add(new Score(b, rank++));
				}
				// Handle pagination
				if (offset > 0) {
					scores.Previous = () => List(board, limit, offset - limit);
				}
				if (offset + scores.Count < scores.Total) {
					scores.Next = () => List(board, limit, offset + limit);
				}
				task.PostResult(scores);
			});
		}

		/// <summary>Fetch the score list for a given board, restricting to the scores made by the friends of the current user.</summary>
		/// <returns>Promise resolved when the operation has completed. The attached value describes a list of scores,
		///     without pagination functionality.</returns>
		/// <param name="board">The name of the board to fetch scores from.</param>
		public Promise<NonpagedList<Score>> ListFriendScores(string board) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/scores").Path(domain).Path(board).QueryParam("type", "friendscore");
			return Common.RunInTask<NonpagedList<Score>>(Gamer.MakeHttpRequest(url), (response, task) => {
				var scores = new NonpagedList<Score>(response.BodyJson);
				foreach (Bundle b in response.BodyJson[board].AsArray()) {
					scores.Add(new Score(b));
				}
				task.PostResult(scores);
			});
		}

		/// <summary>Retrieves the best scores of this gamer, on all board he has posted one score to.</summary>
		/// <returns>Promise resolved when the operation has completed. The attached value contains information about
		///     the best scores of the user, indexed by board name.
		///     *IMPORTANT*: in the results, the gamer information is not provided. GamerInfo is always null.</returns>
		public Promise<Dictionary<string, Score>> ListUserBestScores() {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/bestscores").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			return Common.RunInTask<Dictionary<string, Score>>(req, (response, task) => {
				Dictionary<string, Score> scores = new Dictionary<string, Score>();
				foreach (var pair in response.BodyJson.AsDictionary()) {
					Score s = new Score(pair.Value);
					s.GamerInfo = null;
					scores[pair.Key] = s;
				}
				task.PostResult(scores);
			});
		}

		/// <summary>Retrieves the rank that a given score would have on the leaderboard, without actually registering the score.</summary>
		/// <returns>Promise resolved when the operation has completed. The attached value contains the rank that the
		///     score would have (position in the board).</returns>
		/// <param name="score">The score (numeric value) to check for ranking.</param>
		/// <param name="board">The name of the board to check the ranking against. Should match the board where a score has
		///     already been posted.</param>
		public Promise<int> GetRank(long score, string board) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/scores").Path(domain).Path(board);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("score", score);
			req.Method = "PUT";
			return Common.RunInTask<int>(req, (response, task) => {
				task.PostResult(response.BodyJson["rank"]);
			});
		}

		/// <summary>Post a score.</summary>
		/// <returns>Promise resolved when the operation has completed. The attached value contains the new rank of the
		///     player as well as whether the score was saved.</returns>
		/// <param name="score">The score (numeric value) to record.</param>
		/// <param name="board">The name of the board to post the score to. You may have as many boards as you like for your
		///     game, and scores are scoped between them.</param>
		/// <param name="order">The order for this board. As board are not configured on the server, any client can create a
		///     board dynamically. This parameter serves as as a description for the board and is used only upon
		///     creation (that is, the first player posting to the named board).</param>
		/// <param name="scoreInfo">An optional string used to describe the score made by the user.</param>
		/// <param name="forceSave">When set to true, the score is saved even if its value is less than the past best score
		///     for this player.</param>
		public Promise<PostedGameScore> Post(long score, string board, ScoreOrder order, string scoreInfo = null, bool forceSave = false) {
			UrlBuilder url = new UrlBuilder("/v2.6/gamer/scores").Path(domain).Path(board);
			switch (order) {
				case ScoreOrder.HighToLow: url.QueryParam("order", "hightolow"); break;
				case ScoreOrder.LowToHigh: url.QueryParam("order", "lowtohigh"); break;
			}
			url.QueryParam("mayvary", forceSave);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("score", score, "info", scoreInfo);
			return Common.RunInTask<PostedGameScore>(req, (response, task) => {
				task.PostResult(new PostedGameScore(response.BodyJson));
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

	/// <summary>Describes the possible sorting orders for the score leaderboard.</summary>
	public enum ScoreOrder {
		HighToLow, /* Highest score first, lowest score last */
		LowToHigh, /* Lowest score first, highest score last */
	}
}
