using System;
using System.Collections.Generic;

namespace CotcSdk {

	/// @ingroup model_classes
	/// <summary>Information about a configured product on the BO.</summary>
	public class ConfiguredProduct : PropertiesObject {
		/// <summary>The product identifier as formatted in the query.</summary>
		public string ProductId {
			get { return Props["productId"]; }
		}
		/// <summary>ID of the product on the App Store (mapping with ProductId on CotC).</summary>
		public string AppStoreId {
			get { return Props["appStoreId"]; }
		}
		/// <summary>ID of the product on the Mac App Store (mapping with ProductId on CotC).</summary>
		public string MacAppStoreId {
			get { return Props["macStoreId"]; }
		}
		/// <summary>ID of the product on the Google Play Store (mapping with ProductId on CotC).</summary>
		public string GooglePlayId {
			get { return Props["googlePlayId"]; }
		}

		internal ConfiguredProduct(Bundle serverData) : base(serverData) {}
	}
}
