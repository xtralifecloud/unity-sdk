using System;

namespace CotcSdk.InappPurchase {

	/**
	 * Purchased product as returned by LaunchPurchase.
	 */
	public class PurchasedProduct {
		/**
		 * Store on which the product was purchased.
		 */
		public StoreType StoreType { get; private set; }
		/**
		 * ID of product as configured on the backoffice.
		 */
		public string CotcProductId { get; private set; }
		/**
		 * ID of product as purchased on the platform-specific store (depending on StoreType).
		 */
		public string InternalProductId { get; private set; }
		/**
		 * Paid price (units).
		 */
		public float PaidPrice { get; private set; }
		/**
		 * ISO code of currency used to pay the product.
		 */
		public string PaidCurrency { get; private set; }
		/**
		 * Receipt string. Store-dependent.
		 */
		public string Receipt { get; private set; }
		/**
		 * Complete purchase (consumption) token.
		 */
		public string Token { get; private set; }

		internal PurchasedProduct(StoreType storeType, string cotcProductId, string internalProductId, float paidPrice, string paidCurrency, string receipt, string token) {
			StoreType = storeType;
			CotcProductId = cotcProductId;
			InternalProductId = internalProductId;
			PaidPrice = paidPrice;
			PaidCurrency = paidCurrency;
			Receipt = receipt;
			Token = token;
		}
	}
}

