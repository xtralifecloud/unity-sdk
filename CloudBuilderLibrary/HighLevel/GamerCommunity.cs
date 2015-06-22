using System;
using System.Collections.Generic;

namespace CotcSdk {

	public class GamerCommunity {

		#region Private
		internal GamerCommunity(Gamer parent) {
			Gamer = parent;
		}
		private string domain = Common.PrivateDomain;
		private Gamer Gamer;
		#endregion
	}

	public enum FriendRelationshipStatus {
		Add,
		Blacklist,
		Forget
	}
}
