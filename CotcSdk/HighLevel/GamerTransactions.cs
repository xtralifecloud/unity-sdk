
namespace CotcSdk {

	/// @ingroup gamer_classes
	/// <summary>
	/// Class allowing to manipulate the transactions and perform tasks related to achievements.
	/// This class is scoped by domain, meaning that you can call .Domain("yourdomain") and perform
	/// additional calls that are scoped.
	/// </summary>
	public class GamerTransactions {

		/// <summary>
		/// Retrieves the balance of the user. That is, the amount of "items" remaining after the various executed
		/// transactions.
		/// </summary>
		/// <returns>Promise resolved when the operation has completed. The attached bundle contains the balance.
		///     You can query the individual items by doing `result.Value["gold"]` for instance.</returns>
		public Promise<Bundle> Balance() {
			UrlBuilder url = new UrlBuilder("/v1/gamer/tx").Path(domain).Path("balance");
			HttpRequest req = Gamer.MakeHttpRequest(url);
			return Common.RunInTask<Bundle>(req, (response, task) => {
				task.PostResult(response.BodyJson);
			});
		}

		/// <summary>
		/// Changes the domain affected by the next operations.
		/// You should typically use it this way: `gamer.Transactions.Domain("private").Post(...);`
		/// </summary>
		/// <param name="domain">Domain on which to scope the transactions. Default to `private` if unmodified.</param>
		/// <returns>This object for operation chaining.</returns>
		public GamerTransactions Domain(string domain) {
			this.domain = domain;
			return this;
		}

		/// <summary>
		/// Fetches the history of transactions run for this user. The current balance (resulting values) needs to be queried
		/// via a different call (Balance).
		/// </summary>
		/// <returns>Promise resolved when the operation has completed. The result is paginated, for more information see
		///     #CotcSdk.PagedList<DataType>.</returns>
		/// <param name="unit">If specified, retrieves only the transactions matching a given unit (e.g. "gold").</param>
		/// <param name="limit">For pagination, allows to set a greater or smaller page size than the default 30.</param>
		/// <param name="offset">For pagination, avoid using it explicitly.</param>
		public Promise<PagedList<Transaction>> History(string unit = null, int limit = 30, int offset = 0) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/tx").Path(domain).QueryParam("skip", offset).QueryParam("limit", limit);
			if (unit != null) url.QueryParam("unit", unit);
			// Request for current results
			return Common.RunInTask<PagedList<Transaction>>(Gamer.MakeHttpRequest(url), (response, task) => {
				PagedList<Transaction> transactions = new PagedList<Transaction>(response.BodyJson, offset, response.BodyJson["count"]);
				foreach (Bundle b in response.BodyJson["history"].AsArray()) {
					transactions.Add(new Transaction(b));
				}
				// Handle pagination
				if (offset > 0) {
					transactions.Previous = () => History(unit, limit, offset - limit);
				}
				if (offset + transactions.Count < transactions.Total) {
					transactions.Next = () => History(unit, limit, offset + limit);
				}
				task.PostResult(transactions);
			});
		}

		/// <summary>Executes a transaction on the behalf of the user.</summary>
		/// <returns>Promise resolved when the operation has completed. The attached result contains the new balance
		///     and the possibly triggered achievements.</returns>
		/// <param name="transaction">Transaction to run. Consists of keys and associated integer values. A negative value
		///     indicates that the associated balance should be decremented. The special value "-auto" resets the value
		///     to zero.</param>
		/// <param name="description">Description of the transaction. Will appear in the back office.</param>
		public Promise<TransactionResult> Post(Bundle transaction, string description = null) {
			UrlBuilder url = new UrlBuilder("/v2.2/gamer/tx").Path(domain);
			HttpRequest req = Gamer.MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("transaction", transaction, "description", description);
			return Common.RunInTask<TransactionResult>(req, (response, task) => {
				task.PostResult(new TransactionResult(response.BodyJson));
			});
		}

		#region Private
		internal GamerTransactions(Gamer gamer) {
			Gamer = gamer;
		}
		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		#endregion
	}

}
