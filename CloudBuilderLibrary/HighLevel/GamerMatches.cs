using System;
using System.Collections.Generic;

namespace CotcSdk {

	/**
	 * Some methods accept a PushNotification parameter. This parameter can be used to forward a push notification to the
	 * users who are not active at the moment.
	 */
	public class GamerMatches {

		public event Action<MatchInviteEvent> OnMatchInvitation {
			add { onMatchInvitation += value; CheckEventLoopNeeded(); }
			remove { onMatchInvitation -= value; CheckEventLoopNeeded(); }
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
				Common.InvokeHandler(done, new Match(Gamer, response.BodyJson["match"]), response.BodyJson);
			});
		}

		/**
		 * Deletes a match. Only works if you are the one who created it and it is already finished.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached boolean
		 *     indicates success when true.
		 * @param matchId ID of the match to delete.
		 */
		public void Delete(ResultHandler<bool> done, string matchId) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(matchId);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.Method = "DELETE";
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson["done"], response.BodyJson);
			});
		}

		/**
		 * Clears all event handlers subscribed, ensuring that a match object can be dismissed without causing further
		 * actions in the background.
		 */
		public void DiscardEventHandlers() {
			foreach (Action<MatchInviteEvent> e in onMatchInvitation.GetInvocationList()) onMatchInvitation -= e;
			CheckEventLoopNeeded();
		}

		/**
		 * Changes the domain affected by the next operations.
		 * You should typically use it this way: `gamer.Matches.Domain("private").List(...);`
		 * @param domain domain on which to scope the matches. Default to `private` if unmodified.
		 */
		public GamerMatches Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/**
		 * Fetches a Match object corresponding to a match which the player already belongs to.
		 * It can be used either to obtain additional information about a running match (by inspecting the resulting
		 * match object), or to continue an existing match (by keeping the match object which corresponds to the one
		 * that was returned by the Create method).
		 * This call is not scoped by domain (it uses the Match ID directly).
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached Match
		 *     object allows to operate with the match.
		 * @param matchId the ID of an existing match to resume. It can be fetched from the Match object (MatchId).
		 */
		public void Fetch(ResultHandler<Match> done, string matchId) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(matchId);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, new Match(Gamer, response.BodyJson["match"]), response.BodyJson);
			});
		}

		/**
		 * Asks to join the match with a given ID. Do not use this if you are already part of the match.
		 * This call is not scoped by domain (it uses the Match ID directly).
		 * @param done callback invoked when the operation has finished, either successfully or not. In case of success,
		 *     you get the exact same match object that would be returned by a call to Create or Fetch. It can be used
		 *     to interact with the match as the user who just joined.
		 * @param matchId the ID of an existing match to join. It can be fetched from the Match object (MatchId).
		 * @param notification optional push notification to be sent to inactive players (see class definition).
		 */
		public void Join(ResultHandler<Match> done, string matchId, PushNotification notification = null) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches").Path(matchId).Path("join");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("osn", notification != null ? notification.Data : null);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, new Match(Gamer, response.BodyJson["match"]), response.BodyJson);
			});
		}

		/**
		 * Can be used to list the active matches for this game. In general, it is not recommended to proceed this way
		 * if your goal is to display the games that may be joined. The indexing API is better suited to this use case
		 * (index the match along with properties and look for matches matching the desired properties).
		 * @param done callback invoked when the operation has finished, either successfully or not. The list of
		 *     matches filtered according to the following parameters is provided.
		 * @param participating set to true to only list matches to which this user is participating.
		 * @param invited set to true to filter by matches you are invited to (only include them).
		 * @param finished set to true to also include finished matchs (which are filtered out by default).
		 * @param full set to true to also include games where the maximum number of players has been reached.
		 * @param limit for pagination, allows to set a greater or smaller page size than the default 30.
		 * @param offset for pagination, avoid using it explicitly.
		 */
		public void List(ResultHandler<PagedList<MatchListResult>> done, bool participating = false, bool invited = false, bool finished = false, bool full = false, int limit = 30, int offset = 0) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/matches");
			url.QueryParam("domain", domain).QueryParam("offset", offset).QueryParam("limit", limit);
			if (participating) url.QueryParam("participating");
			if (finished) url.QueryParam("finished");
			if (invited) url.QueryParam("invited");
			if (full) url.QueryParam("full");
			// Request for current results
			Common.RunHandledRequest(Gamer.MakeHttpRequest(url), done, (HttpResponse response) => {
				PagedList<MatchListResult> matches = new PagedList<MatchListResult>(offset, response.BodyJson["count"]);
				foreach (Bundle b in response.BodyJson["matches"].AsArray()) {
					matches.Add(new MatchListResult(b));
				}
				// Handle pagination
				if (offset > 0) {
					matches.Previous = () => List(done, participating, invited, finished, full, limit, offset - limit);
				}
				if (offset + matches.Count < matches.Total) {
					matches.Next = () => List(done, participating, invited, finished, full, limit, offset + limit);
				}
				Common.InvokeHandler(done, matches, response.BodyJson);
			});
		}

		#region Private
		internal GamerMatches(Gamer gamer) {
			Gamer = gamer;
		}

		private void CheckEventLoopNeeded() {
			if (onMatchInvitation != null) {
				// Register if needed
				if (RegisteredEventLoop == null) {
					RegisteredEventLoop = Cotc.GetEventLoopFor(Gamer.GamerId, domain);
					if (RegisteredEventLoop == null) {
						Cotc.LogWarning("No pop event loop for domain " + domain + ", match invitations will not work");
					}
					else {
						RegisteredEventLoop.ReceivedEvent += this.ReceivedLoopEvent;
					}
				}
			}
			else if (RegisteredEventLoop != null) {
				// Unregister from event loop
				RegisteredEventLoop.ReceivedEvent -= this.ReceivedLoopEvent;
				RegisteredEventLoop = null;
			}
		}

		private void ReceivedLoopEvent(DomainEventLoop sender, EventLoopArgs e) {
			if (e.Message["type"].AsString() == "match.invite") {
				if (onMatchInvitation != null) onMatchInvitation(new MatchInviteEvent(Gamer, e.Message));
			}
		}

		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		private event Action<MatchInviteEvent> onMatchInvitation;
		private DomainEventLoop RegisteredEventLoop;
		#endregion
	}

}
