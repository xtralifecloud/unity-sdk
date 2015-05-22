using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudBuilderLibrary {

	public class GamerTransactions {

		/**
		 * Retrieves the balance of the user. That is, the amount of "items" remaining after the various executed
		 * transactions.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached bundle
		 *     contains the balance. You can query the individual items by doing result.Value["gold"] for instance.
		 */
		public void Balance(ResultHandler<Bundle> done) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/tx").Path(Domain).Path("balance");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson, response.BodyJson);
			});
		}

		/**
		 * Fetches the history of transactions run for this user. The current balance (resulting values) needs to be queried
		 * via a different call (Balance).
		 * @param done callback invoked when the operation has finished, either successfully or not. The result is paginated,
		 * for more information see #PagedResultHandler.
		 * @param unit if specified, retrieves only the transactions matching a given unit (e.g. "gold").
		 * @param limit for pagination, allows to set a greater or smaller page size than the default 30.
		 * @param offset for pagination, avoid using it explicitly.
		 */
		public void History(PagedResultHandler<Transaction> done, string unit = null, int limit = 30, int offset = 0) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/tx").Path(Domain).QueryParam("skip", offset).QueryParam("limit", limit);
			if (unit != null) url.QueryParam("unit", unit);
			// Request for current results
			Common.RunHandledRequest(Gamer.MakeHttpRequest(url), done, (HttpResponse response) => {
				List<Transaction> transactions = new List<Transaction>();
				foreach (Bundle b in response.BodyJson["history"].AsArray()) {
					transactions.Add(new Transaction(b));
				}
				// Handle pagination
				PagedResult<Transaction> result = new PagedResult<Transaction>(transactions, response.BodyJson, offset);
				if (offset > 0) {
					result.Previous = () => History(done, unit, offset - limit, limit);
				}
				if (offset + transactions.Count < result.Total) {
					result.Next = () => History(done, unit, offset + limit, limit);
				}
				Common.InvokeHandler(done, result);
			});
		}

		/**
		 * Executes a transaction on the behalf of the user.
		 * @param done callback invoked when the operation has finished, either successfully or not. The attached result
		 *     contains the new balance and the possibly triggered achievements.
		 * @param transaction transaction to run. Consists of keys and associated integer values. A negative value
		 *     indicates that the associated balance should be decremented. The special value "-auto" resets the value
		 *     to zero.
		 * @param description description of the transaction. Will appear in the back office.
		 */
		public void Post(ResultHandler<TransactionResult> done, Bundle transaction, string description = null) {
			UrlBuilder url = new UrlBuilder("/v2.2/gamer/tx").Path(Domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("transaction", transaction, "description", description);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, new TransactionResult(response.BodyJson), response.BodyJson);
			});
		}

		#region Private
		internal GamerTransactions(Gamer gamer, string domain) {
			Gamer = gamer;
			Domain = domain;
		}
		private string Domain;
		private Gamer Gamer;
		#endregion
	}

}
