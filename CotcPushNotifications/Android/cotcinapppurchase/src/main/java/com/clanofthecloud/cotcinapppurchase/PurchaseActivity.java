package com.clanofthecloud.cotcinapppurchase;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;

/**
 * We use this activity in order to simplify the integration.
 */
public class PurchaseActivity extends Activity {

	interface ActivityListener {
		void wasCreated(PurchaseActivity activity);
		void gotActivityResult(int requestCode, int resultCode, Intent data);
		void wasStopped();
	}

	static Activity parent;
	static ActivityListener listener;
	private boolean googleStoreHasBeenShown = false, alreadyStopped = false;

	static void startActivity(Activity parent, ActivityListener listener) {
		Intent intent = new Intent(parent, PurchaseActivity.class);
		PurchaseActivity.listener = listener;
		PurchaseActivity.parent = parent;
		parent.startActivity(intent);
	}

	void stopActivity() {
		if (!alreadyStopped) {
			alreadyStopped = true;
			this.finish();
		}
	}

	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);

		setContentView(R.layout.activity_purchase);
	}

	@Override
	protected void onPostCreate(Bundle savedInstanceState) {
		super.onPostCreate(savedInstanceState);
		if (listener != null) {
			listener.wasCreated(this);
		}
	}

	@Override
	protected void onActivityResult(int requestCode, int resultCode, Intent data) {
		super.onActivityResult(requestCode, resultCode, data);
		if (listener != null) {
			listener.gotActivityResult(requestCode, resultCode, data);
		}
		onDestroy();
	}

	@Override
	protected void onPause() {
		super.onPause();
		// Our activity stops when the Google Play store is shown
		googleStoreHasBeenShown = true;
	}

	@Override
	protected void onResume() {
		super.onResume();
		// This means that the Google dialog has finished
		if (googleStoreHasBeenShown) {
			stopActivity();
		}
	}

	@Override
	protected void onStop() {
		super.onStop();
		// So here we're back to the unity activity, UnitySendMessage can be used again
		if (listener != null) {
			listener.wasStopped();
		}
	}
}
