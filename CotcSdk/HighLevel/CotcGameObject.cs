using System;
using UnityEngine;

namespace CotcSdk
{
	/// @ingroup main_classes
	/// <summary>
	/// Place this object on all scenes where you would like to use CotC functionality, as described @ref cotcgameobject_ref "in this tutorial".
	/// 
	/// Then call #GetCloud to get a Cloud object, which provides an entry point (through sub objects) to all functionality provided by the SDK.
	/// </summary>
	public class CotcGameObject : MonoBehaviour {

		private Promise<Cloud> whenStarted = new Promise<Cloud>();

		/// <summary>
		/// Use this to get a Cloud object, as shown below:
		/// @code{.cs}
		/// var cotc = FindObjectOfType<CotcGameObject>();
		/// cotc.GetCloud(cloud => {
		///     cloud.Login(...);
		/// } @endcode
		/// </summary>
		/// <returns>A promise that returns a Cloud object to be used for most operations. Although the returned object may be shared among
		/// multiple scenes, you need to place a CotcGameObject and call GetCloud on all your scenes to ensure proper operations.</returns>
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

        void OnDestroy()
        {
            if (Application.isEditor)
            {
                if (GameObject.FindObjectsOfType<CotcGameObject>().Length < 1)
                {
                    Debug.Log("Forcing destroy CotC");
                    Cotc.OnApplicationQuit();
                }
            }
        }

        void OnApplicationQuit() {
			Cotc.OnApplicationQuit();
		}
	}

}
