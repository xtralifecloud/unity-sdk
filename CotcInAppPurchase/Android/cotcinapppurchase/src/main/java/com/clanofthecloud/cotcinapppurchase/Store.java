package com.clanofthecloud.cotcinapppurchase;

import android.app.Activity;
import android.content.Intent;
import android.util.Log;

import com.clanofthecloud.cotcinapppurchase.iab.ErrorCode;
import com.clanofthecloud.cotcinapppurchase.iab.IabResult;
import com.unity3d.player.UnityPlayer;
import com.clanofthecloud.cotcinapppurchase.iab.IabHelper;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.ArrayList;

/**
 * Main class interfacing the plugin.
 */
public class Store {
	private static final String TAG = "CotcStore";
	public static int STORE_REQUEST_CODE = 0xC07C;
	private static String gameObjectName;
	// Activity on the behalf of which any purchase flow will be launched. Note that you NEED to forward the onActivityResult to this class!
	private static Activity activity;
	// Callbacks (messages sent to Unity) after operations completed
	private static String CB_LISTPRODUCTS = "Android_GetInformationAboutProducts_Done";
	private static String CB_LAUNCHPURCHASE = "Android_LaunchPurchase_Done";

	/**
	 * Call this at startup in order to be able to make in-app payments through CloudBuilder.
	 * Does NOT work if any attempt to use the CloudBuilder has been made prior to calling this.
	 * @param gameObjectName Name of the game object to send a message to when a result is to be
	 *                       posted.
	 */
	public static void startup(String gameObjectName) {
		Store.gameObjectName = gameObjectName;
		Store.activity = UnityPlayer.currentActivity;
	}

	public static Activity getActivity() {
		return activity;
	}

	/**
	 * Handles an activity result that's part of the purchase flow in in-app billing.
	 * You must call this method from your Activity's {@link android.app.Activity@onActivityResult}
	 * method. This method MUST be called from the UI thread of the Activity.
	 * @param requestCode The requestCode as you received it. A request code matching
	 *                    STORE_REQUEST_CODE indicates a request that needs to be handled by this
	 *                    class. If the STORE_REQUEST_CODE collides with another of your codes, you
	 *                    may overwrite it before you start using the CStoreManager.
	 * @param resultCode The resultCode as you received it.
	 * @param data The data (Intent) as you received it.
	 * @return Returns true if the result was related to a purchase flow and was handled;
	 *     false if the result was not related to a purchase, in which case you should
	 *     handle it normally.
	 */
/*	public static void handleActivityResult(int requestCode, int resultCode, Intent data) {
		IabHelper.sHandleActivityResult(requestCode, resultCode, data);
	}*/

	// These are only used internally, do not call them yourself. Protected is just used as a marker
	// since JNI doesn't enforce access control restrictions.
	/**
	 * Lists the products on sale in the Google Play Store.
	 * @param paramsJson Currently unused.
	 */
	public static void listProducts(String paramsJson) {
		try {
			Log.d(TAG, "ICI");
			// Convert the JSON array to a standard ArrayList SKU list
			final JSONArray products = new JSONArray(paramsJson);
			final ArrayList<String> skus = new ArrayList<String>();
			for (int i = 0; i < products.length(); i++) {
				JSONObject product = products.getJSONObject(i);
				skus.add(product.getString("googlePlayId"));
			}

			// Everything went well; connect to the IAB service
			IabHelper.getHandler(activity, new IabHelper.SetupListener() {
				@Override
				public void onDone(IabHelper handler, IabResult result) {
					if (handler == null) {
						callbackToUnity(CB_LISTPRODUCTS, ErrorCode.ErrorWithExternalStore, result.toString());
						return;
					}
					// Now we can query the products
					handler.getProductDetails(skus, new IabHelper.CloudResultListener() {
						@Override
						public void onDone(JSONObject result) {
							// We now need to enrich the skus with the product ID
							enrichProductDetails(result, products);
							callbackToUnity(CB_LISTPRODUCTS, result);
						}

						@Override
						public void onError(ErrorCode code, String description) {
							callbackToUnity(CB_LISTPRODUCTS, code, description);
						}
					});
				}
			});

		} catch (JSONException e) {
			Log.e("GooglePlayStoreHandler", "Decoding param JSON", e);
			callbackToUnity(CB_LISTPRODUCTS, ErrorCode.InternalError, "Decoding param JSON: " + e.getMessage());
		}
	}

	private static void callbackToUnity(String methodName, String arg) {
		UnityPlayer.UnitySendMessage(gameObjectName, methodName, arg);
	}

	private static void callbackToUnity(String methodName, JSONObject arg) {
		callbackToUnity(methodName, arg.toString());
	}

	private static void callbackToUnity(String methodName, ErrorCode code, String description) {
		try {
			JSONObject result = new JSONObject();
			result.put("error", code.code);
			result.put("description", description);
			callbackToUnity(methodName, result.toString());
		} catch (JSONException ex) {
			Log.e(TAG, "Exception calling back to Unity", ex);
		}
	}

	/**
	 * Launch the purchase of a product.
	 * @param paramsJson Should contain productId = the product ID to purchase.
	 * @param onCompletedHandler Called upon completion.
	 */
/*	protected void purchaseProduct(String paramsJson, final long onCompletedHandler) {
		if (!checkInitCalled(onCompletedHandler)) { return; }
		try {
			JSONObject params = new JSONObject(paramsJson);
			final String cotcProductId = params.getString("productId");
			final String gpSku = params.getString("googlePlayId");

			// Everything went well; connect to the IAB service
			IabHelper.getHandler(activity, new IabHelper.SetupListener() {
				@Override
				public void onDone(final IabHelper handler, IabResult result) {
					if (handler == null) {
						CloudBuilder.InvokeHandler(onCompletedHandler, EErrorCode.enErrorWithExternalStore, result.toString());
						return;
					}

					// Fetch additional info about the product, we'll include this into the receipt
					ArrayList<String> skus = new ArrayList<String>();
					skus.add(gpSku);
					handler.getProductDetails(skus, new IabHelper.CloudResultListener() {
						@Override
						public void onDone(EErrorCode code, JSONObject productDetails, String message) {
							if (code != EErrorCode.enNoErr) {
								CloudBuilder.InvokeHandler(onCompletedHandler, code, productDetails, message);
								return;
							}
							launchPurchase(productDetails, handler, gpSku, onCompletedHandler, cotcProductId);
						}
					});
				}
			});

		} catch (JSONException e) {
			Log.e("GooglePlayStoreHandler", "Decoding param JSON", e);
			CloudBuilder.InvokeHandler(onCompletedHandler, EErrorCode.enInternalError, "Decoding param JSON: " + e.getMessage());
		}
	}*/

	/**
	 * When coming back from google, the product list has only the google SKUs. We want to put back
	 * the names of the products as they appear on the BO.
	 * @param result Result got from getProductDetails.
	 * @param products List of products returned by CotC.
	 */
	private static void enrichProductDetails(JSONObject result, JSONArray products) {
		try {
			JSONArray values = result.getJSONArray("products");
			for (int i = 0; i < values.length(); i++) {
				JSONObject enrichedObject = values.getJSONObject(i);
				String googleSku = enrichedObject.getString("internalProductId");
				for (int j = 0; j < products.length(); j++) {
					JSONObject product = products.getJSONObject(j);
					if (googleSku.equals(product.getString("googlePlayId"))) {
						enrichedObject.put("productId", product.getString("productId"));
					}
				}
			}
		} catch (JSONException e) {
			Log.e("GooglePlayStoreHandler", "Enriching products JSON", e);
		}
	}

	// 3rd step of purchaseProduct (methods sorted by alphabetical order + accesbility).
/*	private void handleEndOfPurchase(Activity activity, final EErrorCode previousCode, final JSONObject previousResult, final long onCompletedHandler, final String gpSku, final String consumptionToken) {
		IabHelper.getHandler(activity, new IabHelper.SetupListener() {
			@Override
			public void onDone(IabHelper handler, IabResult result) {
				if (previousCode != EErrorCode.enNoErr) {
					CloudBuilder.InvokeHandler(onCompletedHandler, previousCode, previousResult, null);
					return;
				}

				// Purchase verified -> consume it
				handler.terminatePurchase(gpSku, consumptionToken, new IabHelper.CloudResultListener() {
					@Override
					public void onDone(EErrorCode code, JSONObject result, String message) {
						if (code != EErrorCode.enNoErr) {
							CloudBuilder.InvokeHandler(onCompletedHandler, code, result, message);
							return;
						}
						// The result that we return to the user is the one from our servers
						CloudBuilder.InvokeHandler(onCompletedHandler, previousCode, previousResult, null);
					}
				});
			}
		});
	}

	// 2nd step of purchaseProduct.
	private void launchPurchase(JSONObject productDetails, final IabHelper handler, final String gpSku, final long onCompletedHandler, final String cotcProductId) {
		try {
			// This object will be used to report additional information with the receipt
			final JSONObject boughtProductInfo = productDetails.getJSONArray("products").getJSONObject(0);

			// Now we can launch the purchase
			handler.launchPurchase(activity, STORE_REQUEST_CODE, gpSku, null, new IabHelper.CloudResultListener() {
				@Override
				public void onDone(EErrorCode code, JSONObject result, String message) {
					if (code != EErrorCode.enNoErr) {
						CloudBuilder.InvokeHandler(onCompletedHandler, code, result, message);
						return;
					}

					// Put additional information with the receipt for the server
					try {
						result.put("productId", cotcProductId);
						result.put("price", boughtProductInfo.getDouble("price"));
						result.put("currency", boughtProductInfo.getString("currency"));

						// Verify the purchase data (use our server)
						final String consumptionToken = message;
						postPurchase(JavaHandler.makeHandler(new JavaHandler() {
							@Override
							void onInvoked(final EErrorCode previousCode, final JSONObject previousResult) {
								handleEndOfPurchase(activity, previousCode, previousResult, onCompletedHandler, gpSku, consumptionToken);
							}
						}), result.toString());
					} catch (JSONException e) {
						Log.e("GooglePlayStoreHandler", "Adding info to receipt JSON", e);
						CloudBuilder.InvokeHandler(onCompletedHandler, EErrorCode.enInternalError, "Adding info to receipt JSON: " + e.getMessage());
					}
				}
			});
		} catch (JSONException e) {
			Log.e("GooglePlayStoreHandler", "Decoding product JSON", e);
			CloudBuilder.InvokeHandler(onCompletedHandler, EErrorCode.enInternalError, "Decoding product JSON: " + e.getMessage());
		}
	}*/
}
