using System;
using System.Collections.Generic;

namespace CotcSdk {

	/**
	 * Information about a configured product on the BO.
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
}
