using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloudBuilderLibrary
{
	public class CloudBuilderGameObject : MonoBehaviour {

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

			CloudBuilder.Setup((Result<Clan> result) => {
				clan = result.Value;
				CloudBuilder.Log("CloudBuilder inited");
				// Notify pending handlers
				foreach (var handler in pendingClanHandlers) {
					handler(clan);
				}
			}, s.ApiKey, s.ApiSecret, s.Environment, s.HttpVerbose, s.HttpTimeout, s.EventLoopTimeout);
		}

		void OnApplicationFocus(bool focused) {
			CloudBuilder.Log(focused ? "CloudBuilder resumed" : "CloudBuilder suspended");
			CloudBuilder.OnApplicationFocus(focused);
		}

		void OnApplicationQuit() {
			CloudBuilder.OnApplicationQuit();
		}
	}

}
