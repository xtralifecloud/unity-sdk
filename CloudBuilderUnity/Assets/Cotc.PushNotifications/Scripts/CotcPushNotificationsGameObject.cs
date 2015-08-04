﻿using System;
using System.Text;
using UnityEngine;

namespace CotcSdk.PushNotifications {

	public class CotcPushNotificationsGameObject : MonoBehaviour {

#if UNITY_ANDROID
		private AndroidJavaClass JavaClass;
#endif

		void Start() {
#if UNITY_ANDROID
			JavaClass = new AndroidJavaClass("com.clanofthecloud.cotcpushnotifications.Controller");
			if (JavaClass == null) {
				Common.LogError("Java class failed to load; check that the JAR is included properly in Assets/Plugins/Android");
				return;
			}
			JavaClass.CallStatic("startup");
#endif
			Cotc.LoggedIn += Cotc_DidLogin;
			Cotc.ApplicationFocusChanged += Cotc_ApplicationFocusChanged;
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

		void Cotc_ApplicationFocusChanged(object sender, Cotc.ApplicationFocusChangedEventArgs e) {
			if (e.NewFocusState) {
#if UNITY_IPHONE
				// Discard the notification count icon on the app
				UnityEngine.iOS.NotificationServices.ClearRemoteNotifications();
#endif
			}
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
	}
}
