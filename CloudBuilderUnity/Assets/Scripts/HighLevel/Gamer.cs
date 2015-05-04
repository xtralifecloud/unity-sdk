using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public enum LoginNetwork {
		Anonymous,
		Facebook,
		GooglePlus,
	}

	public sealed partial class Gamer {

		public List<string> Domains { get; private set; }
		/**
		 * Gamer credential. Use it to gain access to user related tasks.
		 */
		public string GamerId { get; private set; }
		/**
		 * Gamer credential (secret). Same purpose as GamerId, and you will need those in pair.
		 */
		public string GamerSecret { get; private set; }
		public LoginNetwork Network { get; private set; }
		public string NetworkId { get; private set; }
		public ProfileMethods Profile {
			get { return profileMethods.Get(() => new ProfileMethods(this)); }
		}
		public DateTime RegisterTime { get; private set; }

		#region Internal
		/**
		 * Only instantiated internally.
		 * @param gamerData Gamer data as returned by our API calls (loginanonymous, etc.).
		 */
		internal Gamer(Clan parent, Bundle gamerData) {
			Clan = parent;
			Network = Common.ParseEnum<LoginNetwork>(gamerData["network"]);
			NetworkId = gamerData["networkid"];
			GamerId = gamerData["gamer_id"];
			GamerSecret = gamerData["gamer_secret"];
			RegisterTime = Common.ParseHttpDate(gamerData["registerTime"]);
		}

		internal HttpRequest MakeHttpRequest(string path) {
			HttpRequest result = Clan.MakeUnauthenticatedHttpRequest(path);
			string authInfo = GamerId + ":" + GamerSecret;
			result.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
			return result;
		}

		/**
		 * Starts a thread watching for messages on a given domain. Only starts the thread once.
		 */
/*		internal void RegisterPopEventLoop(string domain) {
			SystemPopEventLoopThread thread;
			// Only register threads once
			lock (this) {
				if (PopEventThreads.ContainsKey(domain)) {
					return;
				}
				thread = new SystemPopEventLoopThread(this, domain);
			}
			thread.Start();
		}*/
		#endregion

		#region Private
		internal Clan Clan;
		private CachedMember<ProfileMethods> profileMethods;
		#endregion
	}
}
