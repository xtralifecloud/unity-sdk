using System;

namespace CotcSdk {
	
	/**
	 * Allows to run batches authenticated as a user.
	 */
	public class GamerBatches {

		/**
		 * Changes the domain affected by the next operations.
		 * You should typically use it this way: `gamer.Batches.Domain("private").Run(...);`
		 * @param domain domain on which to scope the next operations.
		 */
		public GamerBatches Domain(string domain) {
			this.domain = domain;
			return this;
		}

		#region Private
		internal GamerBatches(Gamer parent) {
			Gamer = parent;
		}
		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		#endregion
	}
}
