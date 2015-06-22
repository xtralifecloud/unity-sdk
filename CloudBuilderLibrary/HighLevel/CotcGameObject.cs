using System;
using System.Collections.Generic;
using UnityEngine;

namespace CotcSdk
{
	public class CotcGameObject : MonoBehaviour {

		private static Cloud cloud = null;
		private List<Action<Cloud>> pendingCloudHandlers = new List<Action<Cloud>>();

		public void GetCloud(Action<Cloud> done) {
			if (cloud == null) {
				pendingCloudHandlers.Add(done);
			}
			else {
				done(cloud);
			}
		}

		void Start() {
			CotcSettings s = CotcSettings.Instance;

			// No need to initialize it once more
			if (cloud != null) {
				return;
			}
			if (string.IsNullOrEmpty(s.ApiKey) || string.IsNullOrEmpty(s.ApiSecret)) {
				throw new ArgumentException("!!!! You need to set up the credentials of your application in the settings of your Cotc object !!!!");
			}

			Cotc.Setup(s.ApiKey, s.ApiSecret, s.Environment, s.LbCount, s.HttpVerbose, s.HttpTimeout)
			.Then(result => {
				cloud = result.Value;
				Common.Log("CotC inited");
				// Notify pending handlers
				foreach (var handler in pendingCloudHandlers) {
					handler(cloud);
				}
			});
		}

		void Update() {
			Cotc.Update();
		}

		void OnApplicationFocus(bool focused) {
			Common.Log(focused ? "CotC resumed" : "CotC suspended");
			Cotc.OnApplicationFocus(focused);
		}

		void OnApplicationQuit() {
			Cotc.OnApplicationQuit();
		}
	}

}
