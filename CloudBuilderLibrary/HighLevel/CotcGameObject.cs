using System;
using System.Collections.Generic;
using UnityEngine;

namespace CotcSdk
{
	public class CotcGameObject : MonoBehaviour {

		private static Cloud clan = null;
		private List<Action<Cloud>> pendingClanHandlers = new List<Action<Cloud>>();

		public void GetCloud(Action<Cloud> done) {
			if (clan == null) {
				pendingClanHandlers.Add(done);
			}
			else {
				done(clan);
			}
		}

		void Start() {
			CotcSettings s = CotcSettings.Instance;

			// No need to initialize it once more
			if (clan != null) {
				return;
			}
			if (string.IsNullOrEmpty(s.ApiKey) || string.IsNullOrEmpty(s.ApiSecret)) {
				throw new ArgumentException("!!!! You need to set up the credentials of your application in the settings of your Cotc object !!!!");
			}

			Cotc.Setup((Result<Cloud> result) => {
				clan = result.Value;
				Cotc.Log("CotC inited");
				// Notify pending handlers
				foreach (var handler in pendingClanHandlers) {
					handler(clan);
				}
			}, s.ApiKey, s.ApiSecret, s.Environment, s.LbCount, s.HttpVerbose, s.HttpTimeout);
		}

		void Update() {
			Cotc.Update();
		}

		void OnApplicationFocus(bool focused) {
			Cotc.Log(focused ? "CotC resumed" : "CotC suspended");
			Cotc.OnApplicationFocus(focused);
		}

		void OnApplicationQuit() {
			Cotc.OnApplicationQuit();
		}
	}

}
