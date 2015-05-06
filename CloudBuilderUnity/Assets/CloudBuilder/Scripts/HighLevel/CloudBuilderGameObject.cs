using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloudBuilderLibrary
{
	class CloudBuilderGameObject : MonoBehaviour {

		private static Clan clan = null;
		private List<Action<Clan>> pendingClanHandlers = new List<Action<Clan>>();

		public void GetClan(Action<Clan> done) {
			if (clan == null) {
				pendingClanHandlers.Add(done);
			}
			else {
				done(clan);
			}
		}

		void Start() {
			CloudBuilderSettings s = CloudBuilderSettings.Instance;

			// No need to initialize it once more
			if (clan != null) {
				return;
			}
			if (string.IsNullOrEmpty(s.ApiKey) || string.IsNullOrEmpty(s.ApiSecret)) {
				Debug.LogError("!!!! You need to set up the credentials of your application in the CloudBuilder settings pane under the Window menu !!!!");
				return;
			}

			if (string.IsNullOrEmpty(s.FacebookAppId)) {
				CloudBuilder.Log(LogLevel.Info, "No facebook credential. Facebook will be unavailable.");
			}
			else {
				// TODO make the setup to wait on this operation (a chain of initialization callbacks for example)
				FB.Init(() => CloudBuilder.Log("FB inited"), s.FacebookAppId);
			}

			CloudBuilder.Setup((Result<Clan> result) => {
				clan = result.Value;
				CloudBuilder.Log("CloudBuilder inited");
				// Notify pending handlers
				foreach (var handler in pendingClanHandlers) {
					handler(clan);
				}
			}, s.ApiKey, s.ApiSecret, s.Environment, s.HttpVerbose, s.HttpTimeout, s.EventLoopTimeout);
		}

	}

}
