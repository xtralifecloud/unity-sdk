using System;
using System.Collections.Generic;
using System.Text;

namespace CloudBuilderLibrary {

	/**
	 * Info about a player.
	 * Can be enriched with information, accessible using the index operator [].
	 * Typically contains a profile field, with displayname, email and lang. You can fetch this by doing
	 * `string name = GamerInfo["profile"]["displayname"];`
	 */
	public class GamerInfo {
		/**
		 * Id of the gamer.
		 */
		public string GamerId;
		/**
		 * Additional properties (can be enriched via hooks).
		 */
		public Bundle this[string key] {
			get { return Properties[key]; }
		}

		#region Private
		internal GamerInfo(Bundle serverData) {
			GamerId = serverData["gamer_id"];
			Properties = serverData;
		}
		private Bundle Properties;
		#endregion
	}
}
