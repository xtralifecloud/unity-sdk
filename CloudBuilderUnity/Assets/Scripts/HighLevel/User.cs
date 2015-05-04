using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public partial class User {

		public void RegisterPopEventLoop(string domain) {
			SystemPopEventLoopThread thread;
			// Only register threads once
			lock (this) {
				if (PopEventThreads.ContainsKey(domain)) {
					return;
				}
				thread = new SystemPopEventLoopThread(this, domain);
			}
			thread.Start();
		}

		#region Public members
		public LoginNetwork Network { get; private set; }
		public string NetworkId { get; private set; }
		public string GamerId { get; private set; }
		public string GamerSecret { get; private set; }
		public DateTime RegisterTime { get; private set; }
		public List<string> Domains { get; private set; }

		public ProfileMethods Profile {
			get { return profileMethods.Get(() => new ProfileMethods(this)); }
		}
		#endregion

		#region Internal
		/**
		 * Only instantiated internally.
		 * @param gamerData Gamer data as returned by our API calls (loginanonymous, etc.).
		 */
		internal User(Clan clan, Bundle gamerData) {
			Clan = clan;
			GamerData = gamerData;
			Network = Common.ParseEnum<LoginNetwork>(gamerData["network"]);
			NetworkId = gamerData["networkid"];
			GamerId = gamerData["gamer_id"];
			GamerSecret = gamerData["gamer_secret"];
			RegisterTime = Common.ParseHttpDate(gamerData["registerTime"]);
			RegisterPopEventLoop(Common.PrivateDomain);
		}

		internal HttpRequest MakeHttpRequest(string path) {
			HttpRequest result = Clan.MakeUnauthenticatedHttpRequest(path);
			string authInfo = GamerId + ":" + GamerSecret;
			result.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
			return result;
		}
		#endregion

		#region Private
		#endregion

		#region Members
		internal Clan Clan;
		private CachedMember<ProfileMethods> profileMethods;
		// About the logged in user
		private Bundle GamerData;
		private Dictionary<string, SystemPopEventLoopThread> PopEventThreads = new Dictionary<string, SystemPopEventLoopThread>();
		#endregion
	}
}
