using System;
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
		 * @return promise resolved when the operation has completed. The attached value describes a list of products,
		 *     without pagination functionality.
		 */
		public Promise<List<ConfiguredProduct>> ListConfiguredProducts() {
			return Common.RunInTask<List<ConfiguredProduct>>(Gamer.MakeHttpRequest("/v1/gamer/store/products"), (response, task) => {
				List<ConfiguredProduct> products = new List<ConfiguredProduct>();
				foreach (Bundle b in response.BodyJson["products"].AsArray()) {
					products.Add(new ConfiguredProduct(b));
				}
				task.PostResult(products, response.BodyJson);
			});
		}

		/**
		 * Fetches the list of transactions made by the logged in user. Only successful transactions
		 * show here.
		 * @return promise resolved when the operation has completed. The attached value describes a list of purchase
		 *     transactions,without pagination functionality.
		 */
		public Promise<List<PurchaseTransaction>> GetPurchaseHistory() {
			return Common.RunInTask<List<PurchaseTransaction>>(Gamer.MakeHttpRequest("/v1/gamer/store/purchaseHistory"), (response, task) => {
				List<PurchaseTransaction> products = new List<PurchaseTransaction>();
				foreach (Bundle b in response.BodyJson["transactions"].AsArray()) {
					products.Add(new PurchaseTransaction(b));
				}
				task.PostResult(products, response.BodyJson);
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
		public Promise<Done> ValidateReceipt(StoreType storeType, string cotcProductId, float paidPrice, string paidCurrency, string receipt) {
			HttpRequest req = Gamer.MakeHttpRequest("/v1/gamer/store/validateReceipt");
			Bundle data = Bundle.CreateObject();
			data["store"] = storeType.ToString().ToLower();
			data["productId"] = cotcProductId;
			data["price"] = paidPrice;
			data["currency"] = paidCurrency;
			data["receipt"] = receipt;
			req.BodyJson = data;
			return Common.RunInTask<Done>(req, (response, task) => {
				task.PostResult(new Done(response.BodyJson), response.BodyJson);
			});
		}


		#region Private
		internal GamerStore(Gamer parent) {
			Gamer = parent;
		}
		private Gamer Gamer;
		#endregion
	}

	/**
	 * Information about a configured product on the BO.
	 * TODO Move to own file
	 */
	public class ConfiguredProduct : PropertiesObject {
		/**
		 * The product identifier as formatted in the query.
		 */
		public string ProductId {
			get { return Props["productId"]; }
		}
		/**
		 * ID of the product on the Google Play Store (mapping with ProductId on CotC).
		 */
		public string AppStoreId {
			get { return Props["appStoreId"]; }
		}
		/**
		 * ID of the product on the Google Play Store (mapping with ProductId on CotC).
		 */
		public string GooglePlayId {
			get { return Props["googlePlayId"]; }
		}

		internal ConfiguredProduct(Bundle serverData) : base(serverData) {}
	}

	/**
	 * Information about a purchased product transaction.
	 */
	public class PurchaseTransaction : PropertiesObject {
		/**
		 * The type of Store on which the purchase has been made.
		 */
		public StoreType Store {
			get { return Common.ParseEnum<StoreType>(Props["store"]); }
		}

		/**
		 * The ID of the product purchased.
		 */
		public string ProductId {
			get { return Props["productId"]; }
		}

		/**
		 * Time of purchase.
		 */
		public DateTime PurchaseDate {
			get { return Common.ParseHttpDate(Props["dateTime"]); }
		}

		/**
		 * The price paid (the price might have been changed since then on iTunes Connect; any such change does not reflect here).
		 */
		public float Price {
			get { return Props["price"]; }
		}

		/**
		 * Currency unit of the price paid.
		 */
		public string Currency {
			get { return Props["currency"]; }
		}

		/**
		 * The ID of transaction on the original store. You might want to use it for customer service. Depends on the store type.
		 */
		public string StoreTransactionId {
			get { return Props["storeTransactionId"]; }
		}

		internal PurchaseTransaction(Bundle serverData) : base(serverData) { }
	}

	/**
	 * Type of store in which products are purchased.
	 */
	public enum StoreType {
		Appstore,
		Macstore,
		Googleplay,
	}

}
