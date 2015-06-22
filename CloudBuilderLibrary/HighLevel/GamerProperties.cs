using System;

namespace CotcSdk {

	/**
	 * Allows to manipulate the gamer properties.
	 */
	public sealed class GamerProperties {

		#region Private
		internal GamerProperties(Gamer parent) {
			Gamer = parent;
		}
		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		#endregion
	}
}
