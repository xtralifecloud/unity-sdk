using System;
using System.Collections.Generic;

namespace CotcSdk {

	public class GamerGodfather {


		#region Private
		internal GamerGodfather(Gamer parent) {
			Gamer = parent;
		}
		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		#endregion
	}
}
