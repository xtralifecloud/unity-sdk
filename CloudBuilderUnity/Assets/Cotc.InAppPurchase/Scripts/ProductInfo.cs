using System;

namespace CotcSdk.InappPurchase {

	/**
	 * Information about a given product. Returned by CotC when querying the product list and used throughout
	 * platform specific purchase flows.
	 */
	public class ProductInfo : PropertiesObject {
		/**
		 * The product identifier as formatted in the query.
		 */
		public string ProductId {
			get { return Props["productId"]; }
			internal set { Props["productId"] = value; }
		}
		/**
		 * The price, amount in price units. For instance, 0.69 (€) or 99 (¥).
		 */
		public float Price {
			get { return Props["price"]; }
			internal set { Props["price"] = value; }
		}
		/**
		 * The currency represented by the price. Should be the ISO code for this currency, whenever supplied by the store.
		 */
		public string Currency {
			get { return Props["currency"]; }
			internal set { Props["currency"] = value; }
		}
		/**
		 * ID of the product on the Google Play Store (mapping with ProductId on CotC).
		 */
		public string AppStoreId {
			get { return Props["appStoreId"]; }
			internal set { Props["appStoreId"] = value; }
		}
		/**
		 * ID of the product on the Google Play Store (mapping with ProductId on CotC).
		 */
		public string GooglePlayId {
			get { return Props["googlePlayId"]; }
			internal set { Props["googlePlayId"] = value; }
		}

		internal ProductInfo() : base(Bundle.CreateObject()) { }

		// Clone because the properties are modified
		internal ProductInfo(Bundle serverData) : base(serverData.Clone()) { }
	}
}

