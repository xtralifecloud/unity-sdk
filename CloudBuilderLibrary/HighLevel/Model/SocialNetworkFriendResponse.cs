using System;
using System.Collections.Generic;
using System.Text;

namespace CotcSdk {
	
	public class SocialNetworkFriendResponse: PropertiesObject {
		public Dictionary<LoginNetwork, List<SocialNetworkFriend>> ByNetwork;

		internal SocialNetworkFriendResponse(Bundle serverData) : base(serverData) {
			ByNetwork = new Dictionary<LoginNetwork, List<SocialNetworkFriend>>();
			// Parse the response, per network
			foreach (var pair in serverData.AsDictionary()) {
				try {
					LoginNetwork net = (LoginNetwork) Enum.Parse(typeof(LoginNetwork), pair.Key, true);
					var list = new List<SocialNetworkFriend>();
					foreach (var friendPair in pair.Value.AsDictionary()) {
						list.Add(new SocialNetworkFriend(friendPair.Value));
					}
					ByNetwork[net] = list;
				}
				catch (Exception e) {
					Common.LogError("Unknown network " + pair.Key + ", ignoring. Details: " + e.ToString());
				}
			}
		}

	}
}
