using System;
using System.Runtime.InteropServices;

namespace CotcSdk.PushNotifications {

	public static class NativeFunctions {
#if UNITY_IPHONE
		private const string DllName = "__Internal";
#else
		private const string DllName = "CotcPushNotificationsNative";
#endif

		public delegate void CotcDelegate(string json);

		/**
		 * Should register the device for push notifications and return as a result a JSON containing the token encoded in base64.
		 * @param whenDone delegate to be called asynchronously when done. Please run it from the same thread that the function was called on.
		 */
		[DllImport(DllName)]
		public static extern void Cotc_PushNotifications_RegisterDevice(CotcDelegate whenDone);
	}
}
