using System;
using System.Collections.Generic;

namespace CotcSdk {

	/**
	 * Result of #CotcSdk.GamerStore.ValidateReceipt.
	 */
	public class ValidateReceiptResult : PropertiesObject {
		public bool Repeated {
			get { return Props["repeated"]; }
		}
		public PurchaseTransaction Transaction {
			get { return new PurchaseTransaction(Props["purchase"]); }
		}
		public bool Validated {
			get { return Props["ok"]; }
		}

		internal ValidateReceiptResult(Bundle serverData) : base(serverData) { }
	}
}
