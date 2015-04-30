using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary.Model.Gamer
{
	public enum LoginNetwork {
		Anonymous,
		Facebook,
		GooglePlus,
	}

	public class LoggedGamerData {
		public LoginNetwork Network;
		public string NetworkId;
		public string GamerId, GamerSecret;
		public DateTime RegisterTime;
		public List<string> Domains;

		internal LoggedGamerData(Bundle bundle) {
			Network = Common.ParseEnum<LoginNetwork>(bundle["network"]);
			NetworkId = bundle["networkid"];
			GamerId = bundle["gamer_id"];
			GamerSecret = bundle["gamer_secret"];
			RegisterTime = Common.ParseHttpDate(bundle["registerTime"]);
		}
	}
}
