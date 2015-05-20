using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	/**
	 * Achievement and balance (transaction) methods.
	 */
	public sealed partial class Gamer {

		/**
		 * Retrieves the balance of the user. That is, the amount of "items" remaining after the various executed
		 * transactions.
		 * @param done callback invoked when the login has finished, either successfully or not. The attached bundle
		 *     contains the balance. You can query the individual items by doing result.Value["gold"] for instance.
		 * @param domain domain on which to scope the balance. Defaults to "private".
		 */
		public void Balance(ResultHandler<Bundle> done, string domain = Common.PrivateDomain) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/tx").Path(domain).Path("balance");
			HttpRequest req = MakeHttpRequest(url);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson, response.BodyJson);
			});
		}

		/**
		 * Executes a transaction on the behalf of the user.
		 * @param done callback invoked when the login has finished, either successfully or not. The attached bundle
		 *     may contain two keys: "balance" and "achievements". The balance is the new balance after the
		 *     transaction has been run and achievements is the list of achievements that have been triggered as a
		 *     result. For example:
		 *     {"balance": {"gold": 100, "silver": 20},
		 *      "achievements": [ {"name": "test", "type": "limit", "config": {"type": "limit", "maxValue": 100, "unit": "score"}} ]}
		 * @param transaction transaction to run. Consists of keys and associated integer values. A negative value
		 *     indicates that the associated balance should be decremented. The special value "-auto" resets the value
		 *     to zero.
		 * @param description description of the transaction. Will appear in the back office.
		 * @param domain domain on which to scope the transaction. Defaults to "private".
		 */
		public void Transaction(ResultHandler<Bundle> done, Bundle transaction, string description = null, string domain = Common.PrivateDomain) {
			UrlBuilder url = new UrlBuilder("/v2.2/gamer/tx").Path(domain);
			HttpRequest req = MakeHttpRequest(url);
			req.BodyJson = Bundle.CreateObject("transaction", transaction, "description", description);
			Common.RunHandledRequest(req, done, (HttpResponse response) => {
				Common.InvokeHandler(done, response.BodyJson, response.BodyJson);
			});
		}
	}
}
