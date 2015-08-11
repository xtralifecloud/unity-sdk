#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CotcSdk.InappPurchase {

	/**
	 * Android implementation of the Store. Uses Java code for interfacing with the machine.
	 */
	class GooglePlayStoreImpl: IStore {

		private AndroidJavaClass JavaClass;
		private Promise<List<ProductInfo>> LastGetInformationAboutProductsPromise;
		private Promise<PurchasedProduct> LastLaunchProductPromise;

		// GameObjectName is used for callbacks from Java
		public GooglePlayStoreImpl(string gameObjectName) {
			JavaClass = new AndroidJavaClass("com.clanofthecloud.cotcinapppurchase.Store");
			if (JavaClass == null) {
				throw new InvalidOperationException("com.clanofthecloud.cotcinapppurchase.Store java class failed to load; check that the AAR is included properly in Assets/Plugins/Android");
			}
			JavaClass.CallStatic("startup", gameObjectName);
		}

		Promise<List<ProductInfo>> IStore.GetInformationAboutProducts(List<ConfiguredProduct> products) {
			// Already in progress? Refuse immediately.
			lock (this) {
				if (LastGetInformationAboutProductsPromise != null) {
					return Promise<List<ProductInfo>>.Rejected(new CotcException(ErrorCode.AlreadyInProgress, "Listing products"));
				}
				LastGetInformationAboutProductsPromise = new Promise<List<ProductInfo>>();
			}

			// Serialize for Android (passed as a JSON string)
			Bundle interop = Bundle.CreateArray();
			foreach (ConfiguredProduct pi in products) {
				interop.Add(pi.AsBundle());
			}

			// Will call back the CotcInappPurchaseGameObject
			JavaClass.CallStatic("listProducts", interop);
			return LastGetInformationAboutProductsPromise;
		}

		Promise<PurchasedProduct> IStore.LaunchPurchaseFlow(ProductInfo product) {
			// Already in progress? Refuse immediately.
			lock (this) {
				if (LastLaunchProductPromise != null) {
					return Promise<PurchasedProduct>.Rejected(new CotcException(ErrorCode.AlreadyInProgress, "Launching purchase"));
				}
				LastLaunchProductPromise = new Promise<PurchasedProduct>();
			}

			// Will call back the CotcInappPurchaseGameObject
			JavaClass.CallStatic("launchPurchase", product.AsBundle());
			return LastLaunchProductPromise;
		}

		// Callback from Java
		internal void GetInformationAboutProducts_Done(string message) {
			// Extract promise and allow again
			Promise<List<ProductInfo>> promise;
			lock (this) {
				promise = LastGetInformationAboutProductsPromise;
				LastGetInformationAboutProductsPromise = null;
			}

			if (promise == null) {
				Debug.LogWarning("Responding to GetInformationAboutProducts without having promise set");
			}

			Bundle json = Bundle.FromJson(message);
			List<ProductInfo> result = new List<ProductInfo>();
			foreach (Bundle obj in json.AsArray()) {
				result.Add(new ProductInfo(obj));
			}
			promise.Resolve(result);
		}

		// Callback from Java
		internal void LaunchPurchase_Done(string message) {
			// Extract promise and allow again
			Promise<PurchasedProduct> promise;
			lock (this) {
				promise = LastLaunchProductPromise;
				LastLaunchProductPromise = null;
			}

			if (promise == null) {
				Debug.LogWarning("Responding to LaunchPurchase without having promise set");
			}
			
			Bundle bundle = Bundle.FromJson(message);
			PurchasedProduct product = new PurchasedProduct(
				Common.ParseEnum<StoreType>(bundle["storeType"], StoreType.Googleplay),
				bundle["productId"], bundle["price"], bundle["currency"], bundle["receipt"]);
			promise.Resolve(product);
		}
	}
}
#endif
