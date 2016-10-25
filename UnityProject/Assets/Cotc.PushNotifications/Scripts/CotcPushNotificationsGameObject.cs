using System;
using System.Text;
using UnityEngine;

namespace CotcSdk.PushNotifications {

	/// <summary>
	/// This class allows to interact with the underlying implementation of push notifications for your platform.
	/// You should not need to do anything with it.
	/// </summary>
	public class CotcPushNotificationsGameObject : MonoBehaviour {
#if !UNITY_EDITOR

#if UNITY_ANDROID
		private AndroidJavaClass JavaClass;
#endif

		void Start() {
#if UNITY_ANDROID
			JavaClass = new AndroidJavaClass("com.clanofthecloud.cotcpushnotifications.Controller");
			if (JavaClass == null) {
				Common.LogError("com.clanofthecloud.cotcpushnotifications.Controller java class failed to load; check that the AAR is included properly in Assets/Plugins/Android");
				return;
			}
			JavaClass.CallStatic("startup");
#endif
			Cotc.LoggedIn += Cotc_DidLogin;
			Cotc.GotDomainLoopEvent += Cotc_GotDomainLoopEvent;
		}

		void OnDestroy() {
			Cotc.LoggedIn -= Cotc_DidLogin;
		}

		void Update() {
			var token = GetToken();
			// Achieved the registration
			if (token != null && ShouldSendToken) {
				FinishedRegistering(token);
				ShouldSendToken = false;
			}
		}

		void Cotc_GotDomainLoopEvent(DomainEventLoop sender, EventLoopArgs args) {
			// When we receive a message, it means that the pending notification has been approved, so reset the application badge
#if UNITY_IPHONE
			if (args.Message.Has("osn")) {
				Debug.LogWarning ("Will clear events");
				UnityEngine.iOS.NotificationServices.ClearRemoteNotifications();
				var setCountNotif = new UnityEngine.iOS.LocalNotification();
				setCountNotif.fireDate = System.DateTime.Now;
				setCountNotif.applicationIconBadgeNumber = -1;
				setCountNotif.hasAction = false;
				UnityEngine.iOS.NotificationServices.ScheduleLocalNotification(setCountNotif);
			}
#endif
		}

		private void Cotc_DidLogin(object sender, Cotc.LoggedInEventArgs e) {
#if UNITY_IPHONE
			UnityEngine.iOS.NotificationServices.RegisterForNotifications(
				UnityEngine.iOS.NotificationType.Alert | 
			    UnityEngine.iOS.NotificationType.Badge | 
			    UnityEngine.iOS.NotificationType.Sound);
#elif UNITY_ANDROID
			JavaClass.CallStatic("registerForNotifications");
#endif
			ShouldSendToken = true;
			RegisteredGamer = e.Gamer;
		}

		private string GetOsName() {
#if UNITY_IPHONE
			return "ios";
#elif UNITY_ANDROID
			return "android";
#else
			return null;
#endif
		}

		private string GetToken() {
#if UNITY_IPHONE
			var token = UnityEngine.iOS.NotificationServices.deviceToken;
			if (token != null) {
				return System.BitConverter.ToString(token).Replace("-", "").ToLower();
			}
			return null;
#elif UNITY_ANDROID
			return JavaClass.CallStatic<string>("getToken");
#else
			return null;
#endif
		}

		private void FinishedRegistering(string token) {
			RegisteredGamer.Account.RegisterDevice(GetOsName(), token)
				.Catch(ex => {
					Common.LogError("Failed to register Android device for push notifications");
				});
		}

		private bool ShouldSendToken = false;
		private Gamer RegisteredGamer;
#endif
	}
}
