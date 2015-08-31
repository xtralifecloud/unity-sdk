using System;
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
			if (s == null || string.IsNullOrEmpty(s.Environments[s.SelectedEnvironment].ApiKey) ||
				string.IsNullOrEmpty(s.Environments[s.SelectedEnvironment].ApiSecret))
			{
				throw new ArgumentException("!!!! You need to set up the credentials of your application in the settings of your Cotc object !!!!");
			}

			CotcSettings.Environment env = s.Environments[s.SelectedEnvironment];
			Cotc.Setup(env.ApiKey, env.ApiSecret, env.ServerUrl, env.LbCount, env.HttpVerbose, env.HttpTimeout)
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
