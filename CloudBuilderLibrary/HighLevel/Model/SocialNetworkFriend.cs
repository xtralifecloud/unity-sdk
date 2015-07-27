
namespace CotcSdk {

	/**
	 * Data about a friend on the social network.
	 * The most important field is the id, which allows to recognize the gamer uniquely among the given social network.
	 */
	public class SocialNetworkFriend {
		/**
		 * Required. The ID given by the social network, allowing to uniquely identify the friend in question.
		 */
		public string Id;
		/**
		 * If you have either a name (composite of name/first name regardless of order), either the two components
		 * (name, first name), pass them here. You should pass at least one of these.
		 */
		public string FirstName, LastName, Name;
		/**
		 * Additional info that might have been enriched by the CotC servers.
		 * You should never guess or create this info by yourself.
		 */
		public GamerInfo ClanInfo { get; private set; }

		/**
		 * User constructor.
		 */
		public SocialNetworkFriend(string id, string firstName, string lastName, string name) {
			Id = id;
			FirstName = firstName;
			LastName = lastName;
			Name = name;
		}

		/** Default constructor for convenience. */
		public SocialNetworkFriend() { }

		/**
		 * Build from existing JSON data.
		 */
		internal SocialNetworkFriend(Bundle serverData) {
			Id = serverData["id"];
			Name = serverData["name"];
			FirstName = serverData["first_name"];
			LastName = serverData["last_name"];
			if (serverData.Has("clan")) {
				ClanInfo = new GamerInfo(serverData["clan"]);
			}
		}

		internal Bundle ToBundle() {
			Bundle result = Bundle.CreateObject();
			result["id"] = Id;
			result["name"] = Name;
			result["first_name"] = FirstName;
			result["last_name"] = LastName;
			if (ClanInfo != null) result["clan"] = ClanInfo.AsBundle();
			return result;
		}
	}
}
