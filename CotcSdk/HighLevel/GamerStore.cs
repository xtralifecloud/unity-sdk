using System;
using System.Collections.Generic;

namespace CotcSdk {

	/// @ingroup gamer_classes
	/// <summary>(App) Store API.</summary>
	public class GamerStore {

		/// <summary>
		/// Fetch the list of products as configured on the backoffice. Note that this doesn't include any information
		/// about pricing and so on: the external store plugin is required to do so.
		/// Note that this call returns the catalog as configured on the CotC server, which may not be exhaustive if
		/// additional products are configured on iTunes Connect but not reported to the CotC servers.
		/// </summary>
		/// <param name="limit">The maximum number of results to return per page.</param>
		/// <param name="offset">Number of the first result.</param>
		/// <returns>Promise resolved when the operation has completed. The attached value describes a list of products,
		///     with pagination functionality.</returns>
		public Promise<PagedList<ConfiguredProduct>> ListConfiguredProducts(int limit = 30, int offset = 0) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/store/products").QueryParam("limit", limit).QueryParam("skip", offset);
			return Common.RunInTask<PagedList<ConfiguredProduct>>(Gamer.MakeHttpRequest(url), (response, task) => {
				PagedList<ConfiguredProduct> products = new PagedList<ConfiguredProduct>(response.BodyJson, offset, response.BodyJson["count"]);
				foreach (Bundle b in response.BodyJson["products"].AsArray()) {
					products.Add(new ConfiguredProduct(b));
				}
				// Handle pagination
				if (offset > 0) {
					products.Previous = () => ListConfiguredProducts(limit, offset - limit);
				}
				if (offset + products.Count < products.Total) {
					products.Next = () => ListConfiguredProducts(limit, offset + limit);
				}
				task.PostResult(products);
			});
		}

		/// <summary>
		/// Fetches the list of transactions made by the logged in user. Only successful transactions
		/// show here.
		/// </summary>
		/// <returns>Promise resolved when the operation has completed. The attached value describes a list of purchase
		///     transactions, without pagination functionality.</returns>
		public Promise<NonpagedList<PurchaseTransaction>> GetPurchaseHistory() {
			return Common.RunInTask<NonpagedList<PurchaseTransaction>>(Gamer.MakeHttpRequest("/v1/gamer/store/purchaseHistory"), (response, task) => {
				var products = new NonpagedList<PurchaseTransaction>(response.BodyJson);
				foreach (Bundle b in response.BodyJson["purchases"].AsArray()) {
					products.Add(new PurchaseTransaction(b));
				}
				task.PostResult(products);
			});
		}

		/// <summary>
		/// Last step in the purchase. Validates the receipt received by a native purchase. You may have to do additional
		/// steps to close your purchase process.
		/// </summary>
		/// <returns>Promise indicating whether the recceipt was validated properly. In case of exception, you can inspect why
		///     the receipt failed to verify.</returns>
		/// <param name="storeType">Type of Store, should be provided by the store plugin. Valid are appstore, macstore, googleplay.</param>
		/// <param name="cotcProductId">ID of the product purchased (as configured on the backoffice).</param>
		/// <param name="paidPrice">Paid price in units.</param>
		/// <param name="paidCurrency">Currency of paid price (ISO code).</param>
		/// <param name="receipt">Receipt string, dependent on the store type.</param>
		public Promise<ValidateReceiptResult> ValidateReceipt(StoreType storeType, string cotcProductId, float paidPrice, string paidCurrency, string receipt) {
			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/store/validateReceipt");
			Bundle data = Bundle.CreateObject();
			data["store"] = storeType.ToString().ToLower();
			data["productId"] = cotcProductId;
			data["price"] = paidPrice;
			data["currency"] = paidCurrency;
			data["receipt"] = receipt;
			req.BodyJson = data;
			return Common.RunInTask<ValidateReceiptResult>(req, (response, task) => {
				task.PostResult(new ValidateReceiptResult(response.BodyJson));
			});
		}

		#region Private
		internal GamerStore(Gamer parent) {
			Gamer = parent;
		}
		private Gamer Gamer;
		#endregion
	}
}
