using System;
using System.Text;
using System.Collections.Generic;

namespace CloudBuilderLibrary
{
	public partial class User {

		public List<string> Domains { get; private set; }
		public string GamerId { get; private set; }
		public string GamerSecret { get; private set; }
		public LoginNetwork Network { get; private set; }
		public string NetworkId { get; private set; }
		public DateTime RegisterTime { get; private set; }

		public ProfileMethods Profile {
			get { return profileMethods.Get(() => new ProfileMethods(this)); }
		}

		/**
		 * Subscribe to this to be notified when an event is raised for a given domain.
		 * @param domain the domain for which to watch. Will start an event listening thread if not running already.
		 * @param action an action to perform when an event is raised.
		 * @return whether the event listener was added done properly.
		 */
		public ErrorCode RegisterEventListener(string domain, EventReceivedDelegate action) {
			if (!CheckValidSync()) return ErrorCode.NotLoggedInAnymore;

			RegisterPopEventLoop(domain);
			return ErrorCode.Ok;
		}

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

		/**
		 * Starts a thread watching for messages on a given domain. Only starts the thread once.
		 */
		internal void RegisterPopEventLoop(string domain) {
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
		#endregion

		#region Private
		private bool CheckValid<T>(ResultHandler<T> calledOnFailure) {
			if (!CheckValidSync()) {
				Common.InvokeHandler(calledOnFailure, ErrorCode.NotLoggedInAnymore);
				return false;
			}
			return true;
		}
		private bool CheckValidSync() {
			return Clan.LoggedInUser == this;
		}

		internal Clan Clan;
		// About the logged in user
		private Bundle GamerData;
		private Dictionary<string, SystemPopEventLoopThread> PopEventThreads = new Dictionary<string, SystemPopEventLoopThread>();
		private CachedMember<ProfileMethods> profileMethods;
		#endregion
	}
}
