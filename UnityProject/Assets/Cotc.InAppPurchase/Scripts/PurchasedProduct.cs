using System;

namespace CotcSdk.InappPurchase {

	/// <summary>Purchased product as returned by LaunchPurchase.</summary>
	public class PurchasedProduct {
		/// <summary>Store on which the product was purchased.</summary>
		public StoreType StoreType { get; private set; }
		/// <summary>ID of product as configured on the backoffice.</summary>
		public string CotcProductId { get; private set; }
		/// <summary>ID of product as purchased on the platform-specific store (depending on StoreType).</summary>
		public string InternalProductId { get; private set; }
		/// <summary>Paid price (units).</summary>
		public float PaidPrice { get; private set; }
		/// <summary>ISO code of currency used to pay the product.</summary>
		public string PaidCurrency { get; private set; }
		/// <summary>Receipt string. Store-dependent.</summary>
		public string Receipt { get; private set; }
		/// <summary>Complete purchase (consumption) token.</summary>
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

