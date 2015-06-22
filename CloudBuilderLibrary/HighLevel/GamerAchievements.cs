using System;
using System.Collections.Generic;

namespace CotcSdk {
	public class GamerAchievements {

		/**
		 * Changes the domain affected by the next operations.
		 * You should typically use it this way: `gamer.Achievements.Domain("private").List(...);`
		 * @param domain domain on which to scope the next operations.
		 */
		public GamerAchievements Domain(string domain) {
			this.domain = domain;
			return this;
		}

		#region Private
		internal GamerAchievements(Gamer parent) {
			Gamer = parent;
		}
		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		#endregion
	}
}
