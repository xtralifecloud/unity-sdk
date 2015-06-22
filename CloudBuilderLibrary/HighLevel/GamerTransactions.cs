using System;
using System.Collections.Generic;

namespace CotcSdk {

	/**
	 * Class allowing to manipulate the transactions and perform tasks related to achievements.
	 * This class is scoped by domain, meaning that you can call .Domain("yourdomain") and perform
	 * additional calls that are scoped.
	 */
	public class GamerTransactions {

		#region Private
		internal GamerTransactions(Gamer gamer) {
			Gamer = gamer;
		}
		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		#endregion
	}

}
