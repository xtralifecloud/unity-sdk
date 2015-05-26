using System;
using System.Collections.Generic;
using System.Text;

namespace CloudBuilderLibrary {

	public class GamerInfo {
		public string GamerId;
		public GamerProfile Profile {
			get; private set;
		}

		internal GamerInfo(Bundle serverData) {
			GamerId = serverData["gamer_id"];
			Profile = new GamerProfile(serverData["profile"]);
		}
	}
}
