package com.clanofthecloud.cotcpushnotifications;

import android.app.Activity;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.support.v4.content.LocalBroadcastManager;
import android.util.Log;

import com.google.android.gms.common.ConnectionResult;
import com.google.android.gms.common.GooglePlayServicesUtil;
import com.unity3d.player.UnityPlayer;

/**
 * Main class allowing to start/stop/etc. the plugin
 *
 * Call startup() when you want to enable receiving notifications (on the onCreate of your main activity).
 */
public class Controller {
	static final String REGISTRATION_COMPLETE = "registrationComplete";
	private static final int PLAY_SERVICES_RESOLUTION_REQUEST = 9000;
	private static final String TAG = "CotcPushController";
	private BroadcastReceiver mRegistrationBroadcastReceiver;
	private Activity activity;
	private static Controller instance;

	public static void startup() {
		if (instance == null) {
			instance = new Controller();
		}
	}

	public static void registerForNotifications() {
		instance.startRegistration();
	}

	public static void onPause() {
		instance.pause();
	}

	public static void onResume() {
		instance.resume();
	}

	public static String getToken() {
		return instance.registrationToken();
	}

	public Controller() {
		activity = UnityPlayer.currentActivity;
	}

	private void pause() {
		LocalBroadcastManager.getInstance(activity).unregisterReceiver(mRegistrationBroadcastReceiver);
	}

	private String registrationToken() {
		return RegistrationIntentService.getRegistrationToken();
	}

	private void resume() {
		LocalBroadcastManager.getInstance(activity).registerReceiver(mRegistrationBroadcastReceiver,
			new IntentFilter(REGISTRATION_COMPLETE));
	}

	public void startRegistration() {
		Log.d(TAG, "Registering for notifications (Android)");
		mRegistrationBroadcastReceiver = new BroadcastReceiver() {
			@Override
			public void onReceive(Context context, Intent intent) {
				Log.d(TAG, "Registration broadcast intent received");
			}
		};
		if (checkPlayServices()) {
			// Start IntentService to register this application with GCM.
			Intent intent = new Intent(activity, RegistrationIntentService.class);
			activity.startService(intent);
		}
	}

	/**
	 * Check the device to make sure it has the Google Play Services APK. If
	 * it doesn't, display a dialog that allows users to download the APK from
	 * the Google Play Store or enable it in the device's system settings.
	 */
	private boolean checkPlayServices() {
		int resultCode = GooglePlayServicesUtil.isGooglePlayServicesAvailable(activity);
		if (resultCode != ConnectionResult.SUCCESS) {
			if (GooglePlayServicesUtil.isUserRecoverableError(resultCode)) {
				GooglePlayServicesUtil.getErrorDialog(resultCode, activity,
					PLAY_SERVICES_RESOLUTION_REQUEST).show();
			} else {
				Log.w(TAG, "This device is not supported.");
			}
			return false;
		}
		return true;
	}
}
