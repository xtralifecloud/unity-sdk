using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudBuilderLibrary {

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
		 * Default constructor for convenience.
		 */
		public SocialNetworkFriend(string id, string firstName, string lastName, string name) {
			Id = id;
			FirstName = firstName;
			LastName = lastName;
			Name = name;
		}
		public SocialNetworkFriend() { }

		internal Bundle ToBundle() {
			Bundle result = Bundle.CreateObject();
			result["id"] = Id;
			result["name"] = Name;
			result["first_name"] = FirstName;
			result["last_name"] = LastName;
			return result;
		}
	}

}
