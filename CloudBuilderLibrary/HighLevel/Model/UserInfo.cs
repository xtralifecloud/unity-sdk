using System;
using System.Collections.Generic;
using System.Text;

namespace CotcSdk {

	/**
	 * Info about a user.
	 * Can be enriched with information, accessible using the index operator [].
	 * Typically contains a profile field, with displayname, email and lang. You can fetch this by doing
	 * `string name = UserInfo["profile"]["displayname"];`
	 */
	public class UserInfo: PropertiesObject {
		/**
		 * Login network.
		 */
		public LoginNetwork Network { get; private set; }
		/**
		 * Gamer credential. Use it to gain access to user related tasks.
		 */
		public string NetworkId { get; private set; }
		/**
		 * Id of the user (compatible with GamerId where used).
		 */
		public string UserId { get; private set; }

		#region Private
		internal UserInfo(Bundle serverData) : base(serverData) {
			Network = Common.ParseEnum<LoginNetwork>(serverData["network"]);
			NetworkId = serverData["networkid"];
			UserId = serverData["user_id"];
		}
		#endregion
	}
}
