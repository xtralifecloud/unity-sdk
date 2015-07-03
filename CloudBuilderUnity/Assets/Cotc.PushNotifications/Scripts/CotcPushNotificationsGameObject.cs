using System;
using System.Text;
using UnityEngine;

namespace CotcSdk.PushNotifications {

	public class CotcPushNotificationsGameObject : MonoBehaviour {

		void Start() {
			Cotc.LoggedIn += Cotc_DidLogin;
		}

		void OnDestroy() {
			Cotc.LoggedIn -= Cotc_DidLogin;
		}

		private void Cotc_DidLogin(object sender, Cotc.LoggedInEventArgs e) {
#if UNITY_IPHONE
#else
			NativeFunctions.Cotc_PushNotifications_RegisterDevice(doneJson => {
				Debug.LogWarning("Done: " + doneJson);
			});
#endif
		}
	}
}
