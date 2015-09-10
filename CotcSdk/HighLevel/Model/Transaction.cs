using System;

namespace CotcSdk {

	/// @ingroup model_classes
	/// <summary>Transaction as archived on the CotC servers.</summary>
	public sealed class Transaction {
		public string Description;
		public string Domain;
		public DateTime RunDate;
		/// <summary>The transaction itself (e.g. {"gold": 100}).</summary>
		public Bundle TxData;

		internal Transaction(Bundle serverData) {
			Description = serverData["desc"];
			Domain = serverData["domain"];
			RunDate = Common.ParseHttpDate(serverData["ts"]);
			TxData = serverData["tx"];
		}
	}
}
