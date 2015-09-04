using System;
using System.Collections.Generic;

namespace CotcSdk {

	/// <summary>(App) Store API.</summary>

	/// <summary>Information about a purchased product transaction.</summary>
	public class PurchaseTransaction : PropertiesObject {
		/// <summary>The type of Store on which the purchase has been made.</summary>
		public StoreType Store {
			get { return Common.ParseEnum<StoreType>(Props["store"]); }
		}

		/// <summary>The ID of the product purchased.</summary>
		public string ProductId {
			get { return Props["productId"]; }
		}

		/// <summary>Time of purchase.</summary>
		public DateTime PurchaseDate {
			get { return Common.ParseHttpDate(Props["dateTime"]); }
		}

		/// <summary>The price paid (the price might have been changed since then on iTunes Connect; any such change does not reflect here).</summary>
		public float Price {
			get { return Props["price"]; }
		}

		/// <summary>Currency unit of the price paid.</summary>
		public string Currency {
			get { return Props["currency"]; }
		}

		/// <summary>The ID of transaction on the original store. You might want to use it for customer service. Depends on the store type.</summary>
		public string StoreTransactionId {
			get { return Props["storeTransactionId"]; }
		}

		internal PurchaseTransaction(Bundle serverData) : base(serverData) { }
	}
}
