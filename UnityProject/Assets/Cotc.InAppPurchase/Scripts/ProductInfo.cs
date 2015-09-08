using System;

namespace CotcSdk.InappPurchase {

	/// <summary>
	/// Information about a given product. Returned by CotC when querying the product list and used throughout
	/// platform specific purchase flows.
	/// </summary>
	public class ProductInfo : PropertiesObject {
		/// <summary>The product identifier as formatted in the query.</summary>
		public string ProductId {
			get { return Props["productId"]; }
			internal set { Props["productId"] = value; }
		}
		/// <summary>The price, amount in price units. For instance, 0.69 (€) or 99 (¥).</summary>
		public float Price {
			get { return Props["price"]; }
			internal set { Props["price"] = value; }
		}
		/// <summary>The currency represented by the price. Should be the ISO code for this currency, whenever supplied by the store.</summary>
		public string Currency {
			get { return Props["currency"]; }
			internal set { Props["currency"] = value; }
		}
		/// <summary>ID of the product on the platform-dependent Store (mapping with ProductId on CotC).</summary>
		public string InternalProductId {
			get { return Props["internalProductId"]; }
			internal set { Props["internalProductId"] = value; }
		}

		internal ProductInfo() : base(Bundle.CreateObject()) { }

		// Clone because the properties are modified
		internal ProductInfo(Bundle serverData) : base(serverData.Clone()) { }
	}
}

