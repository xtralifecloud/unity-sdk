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

		void Update() {
#if UNITY_IPHONE
			// Achieved the registration
			var token = UnityEngine.iOS.NotificationServices.deviceToken;
			if (token != null) {
				Debug.Log("Token to be sent");
				FinishedRegistering(token);
				AlreadySentToken = true;
			}
#endif
		}

		private void Cotc_DidLogin(object sender, Cotc.LoggedInEventArgs e) {
#if UNITY_IPHONE
			UnityEngine.iOS.NotificationServices.RegisterForNotifications(
				UnityEngine.iOS.NotificationType.Alert | 
			    UnityEngine.iOS.NotificationType.Badge | 
			    UnityEngine.iOS.NotificationType.Sound);
#else
			NativeFunctions.Cotc_PushNotifications_RegisterDevice(doneJson => {
				Debug.LogWarning("Done: " + doneJson);
			});
#endif
		}

		private void FinishedRegistering(byte[] token) {

		}

		private bool AlreadySentToken = false;
	}
}
