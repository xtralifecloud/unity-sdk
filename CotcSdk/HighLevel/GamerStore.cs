﻿using System;
using System.Collections.Generic;

namespace CotcSdk {

	/**
	 * (App) Store API.
	 */
	public class GamerStore {

		/**
		 * Fetch the list of products as configured on the backoffice. Note that this doesn't include any information
		 * about pricing and so on: the external store plugin is required to do so.
		 * Note that this call returns the catalog as configured on the CotC server, which may not be exhaustive if
		 * additional products are configured on iTunes Connect but not reported to the CotC servers.
		 * @param limit the maximum number of results to return per page.
		 * @param offset number of the first result.
		 * @return promise resolved when the operation has completed. The attached value describes a list of products,
		 *     with pagination functionality.
		 */
		public Promise<PagedList<ConfiguredProduct>> ListConfiguredProducts(int limit = 30, int offset = 0) {
			UrlBuilder url = new UrlBuilder("/v1/gamer/store/products").QueryParam("limit", limit).QueryParam("skip", offset);
			return Common.RunInTask<PagedList<ConfiguredProduct>>(Gamer.MakeHttpRequest(url), (response, task) => {
				PagedList<ConfiguredProduct> products = new PagedList<ConfiguredProduct>(offset, response.BodyJson["count"]);
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

		/**
		 * Fetches the list of transactions made by the logged in user. Only successful transactions
		 * show here.
		 * @return promise resolved when the operation has completed. The attached value describes a list of purchase
		 *     transactions, without pagination functionality.
		 */
		public Promise<List<PurchaseTransaction>> GetPurchaseHistory() {
			return Common.RunInTask<List<PurchaseTransaction>>(Gamer.MakeHttpRequest("/v1/gamer/store/purchaseHistory"), (response, task) => {
				List<PurchaseTransaction> products = new List<PurchaseTransaction>();
				foreach (Bundle b in response.BodyJson["transactions"].AsArray()) {
					products.Add(new PurchaseTransaction(b));
				}
				task.PostResult(products);
			});
		}

		/**
		 * Last step in the purchase. Validates the receipt received by a native purchase. You may have to do additional
		 * steps to close your purchase process.
		 * @return promise indicating whether the recceipt was validated properly. In case of exception, you can inspect why
		 *     the receipt failed to verify.
		 * @param storeType type of Store, should be provided by the store plugin. Valid are appstore, macstore, googleplay.
		 * @param cotcProductId ID of the product purchased (as configured on the backoffice).
		 * @param paidPrice paid price in units.
		 * @param paidCurrency currency of paid price (ISO code).
		 * @param receipt receipt string, dependent on the store type.
		 */
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
