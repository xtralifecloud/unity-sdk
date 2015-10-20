
namespace CotcSdk {

	/// @ingroup model_classes
	/// <summary>
	/// Data about a friend on the social network.
	/// The most important field is the id, which allows to recognize the gamer uniquely among the given social network.
	/// </summary>
	public class SocialNetworkFriend {
		/// <summary>Required. The ID given by the social network, allowing to uniquely identify the friend in question.</summary>
		public string Id;
		/// <summary>
		/// If you have either a name (composite of name/first name regardless of order), either the two components
		/// (name, first name), pass them here. You should pass at least one of these.
		/// </summary>
		public string FirstName, LastName, Name;
		/// <summary>
		/// Additional info that might have been enriched by the CotC servers.
		/// You should never guess or create this info by yourself.
		/// </summary>
		public GamerInfo ClanInfo { get; private set; }

		/// <summary>User constructor.</summary>
		public SocialNetworkFriend(string id, string firstName, string lastName, string name) {
			Id = id;
			FirstName = firstName;
			LastName = lastName;
			Name = name;
		}

		/// <summary>Default constructor for convenience.</summary>
		public SocialNetworkFriend() { }

		/// <summary>Build from existing JSON data.</summary>
		public SocialNetworkFriend(Bundle serverData) {
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
