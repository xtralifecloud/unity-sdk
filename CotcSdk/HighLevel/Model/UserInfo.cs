
namespace CotcSdk {

	/// <summary>
	/// Info about a user.
	/// Can be enriched with information, accessible using the index operator [].
	/// Typically contains a profile field, with displayname, email and lang. You can fetch this by doing
	/// `string name = UserInfo["profile"]["displayname"];`
	/// </summary>
	public class UserInfo: PropertiesObject {
		/// <summary>Login network.</summary>
		public LoginNetwork Network { get; private set; }
		/// <summary>Gamer credential. Use it to gain access to user related tasks.</summary>
		public string NetworkId { get; private set; }
		/// <summary>Id of the user (compatible with GamerId where used).</summary>
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
