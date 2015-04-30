using System;
using System.Text;
using System.Collections.Generic;
using CloudBuilderLibrary.Model.Gamer;

namespace CloudBuilderLibrary
{
	public class UserManager: ManagerBase {
		/**
		 * Logs the current user in anonymously.
		 * @param done callback invoked when the login has finished, either successfully or not.
		 */
		public void LoginAnonymous(Action<CloudResult, LoggedGamerData> done) {
			if (!CloudBuilder.Clan.RequireSetup(done)) return;

			Bundle config = Bundle.CreateObject();
			config["device"] = CloudBuilder.SystemFunctions.CollectDeviceInformation();
			
			HttpRequest req = MakeUnauthenticatedHttpRequest("/v1/login/anonymous");
			req.BodyJson = config;
			CloudBuilder.HttpClient.Run(req, (HttpResponse response) => {
				CloudResult result = new CloudResult(response);
				if (response.HasFailed) {
					Common.InvokeHandler(done, result);
					return;
				}
				GamerData = new LoggedGamerData(result.Data);
				DidLogin();
				Common.InvokeHandler(done, result, GamerData);
			});
		}
		
		public void TEMP_GetUserProfile(Action<CloudResult, string> done = null) {
			if (!RequireLoggedIn(done)) return;
			
			HttpRequest req = MakeHttpRequest("/v1/gamer/profile");
			CloudBuilder.HttpClient.Run(req, (HttpResponse response) => {
			});
		}

		public void RegisterPopEventLoop(string domain) {
			SystemPopEventLoopThread thread;
			// Only register threads once
			lock (this) {
				if (PopEventThreads.ContainsKey(domain)) {
					return;
				}
				thread = new SystemPopEventLoopThread(domain);
			}
			thread.Start();
		}

		#region Internal
		internal bool IsLogged {
			get { return GamerData != null; }
		}

		internal override HttpRequest MakeHttpRequest(string path) {
			HttpRequest result = MakeUnauthenticatedHttpRequest(path);
			string authInfo = GamerData.GamerId + ":" + GamerData.GamerSecret;
			result.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
			return result;
		}
		#endregion

		#region Private
		public void DidLogin() {
			RegisterPopEventLoop(Common.PrivateDomain);
		}
		#endregion

		#region Members
		// About the logged in user
		private LoggedGamerData GamerData;
		private Dictionary<string, SystemPopEventLoopThread> PopEventThreads = new Dictionary<string, SystemPopEventLoopThread>();
        #endregion
	}
}
