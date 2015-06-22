using System;

namespace CotcSdk {
	
	/**
	 * Allows to run batches authenticated as a game (that is, unauthenticated).
	 */
	public class GameBatches {

		/**
		 * Changes the domain affected by the next operations.
		 * You should typically use it this way: `gamer.Batches.Domain("private").Run(...);`
		 * @param domain domain on which to scope the next operations.
		 */
		public GameBatches Domain(string domain) {
			this.domain = domain;
			return this;
		}

		#region Private
		internal GameBatches(Cloud parent) {
			Cloud = parent;
		}
		private string domain = Common.PrivateDomain;
		private Cloud Cloud;
		#endregion
	}
}
