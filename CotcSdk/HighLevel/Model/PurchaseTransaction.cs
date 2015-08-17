using System;
using System.Collections.Generic;

namespace CotcSdk {

	/**
	 * (App) Store API.
	 */

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
}
