using System;
using System.Collections.Generic;
using UnityEngine;

namespace CotcSdk
{
	public class CotcGameObject : MonoBehaviour {

		private Promise<Cloud> whenStarted = new Promise<Cloud>();

		public Promise<Cloud> GetCloud() {
			return whenStarted;
		}

		void Start() {
			CotcSettings s = CotcSettings.Instance;

			// No need to initialize it once more
			if (string.IsNullOrEmpty(s.ApiKey) || string.IsNullOrEmpty(s.ApiSecret)) {
				throw new ArgumentException("!!!! You need to set up the credentials of your application in the settings of your Cotc object !!!!");
			}

			Cotc.Setup(s.ApiKey, s.ApiSecret, s.Environment, s.LbCount, s.HttpVerbose, s.HttpTimeout)
			.Then(result => {
				Common.Log("CotC inited");
				whenStarted.Resolve(result);
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
