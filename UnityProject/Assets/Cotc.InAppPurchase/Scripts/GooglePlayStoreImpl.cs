#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CotcSdk.InappPurchase {

	/// <summary>Android implementation of the Store. Uses Java code for interfacing with the machine.</summary>
	class GooglePlayStoreImpl: IStore {

		private AndroidJavaClass JavaClass;
		private Promise<List<ProductInfo>> LastGetInformationAboutProductsPromise;
		private Promise<PurchasedProduct> LastLaunchProductPromise;
		private Promise<Done> LastTerminatePurchasePromise;

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
			JavaClass.CallStatic("listProducts", interop.ToJson());
			return LastGetInformationAboutProductsPromise;
		}

		// Callback from native code
		void IStore.GetInformationAboutProducts_Done(string message) {
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
			// Error
			if (json.Has("error")) {
				promise.Reject(ParseError(json));
				return;
			}
			
			List<ProductInfo> result = new List<ProductInfo>();
			foreach (Bundle obj in json["products"].AsArray()) {
				result.Add(new ProductInfo(obj));
			}
			promise.Resolve(result);
		}

		Promise<PurchasedProduct> IStore.LaunchPurchaseFlow(Gamer gamer, ProductInfo product) {
			// Already in progress? Refuse immediately.
			lock (this) {
				if (LastLaunchProductPromise != null) {
					return Promise<PurchasedProduct>.Rejected(new CotcException(ErrorCode.AlreadyInProgress, "Launching purchase"));
				}
				LastLaunchProductPromise = new Promise<PurchasedProduct>();
			}

			// Will call back the CotcInappPurchaseGameObject
			JavaClass.CallStatic("launchPurchase", product.ToJson());
			return LastLaunchProductPromise;
		}

		// Callback from native code
		void IStore.LaunchPurchase_Done(string message) {
			// Extract promise and allow again
			Promise<PurchasedProduct> promise;
			lock (this) {
				promise = LastLaunchProductPromise;
				LastLaunchProductPromise = null;
			}
			
			if (promise == null) {
				Debug.LogWarning("Responding to LaunchPurchase without having promise set");
			}
			
			Bundle json = Bundle.FromJson(message);
			if (json.Has("error")) {
				promise.Reject(ParseError(json));
				return;
			}
			
			PurchasedProduct product = new PurchasedProduct(
				Common.ParseEnum<StoreType>(json["store"], StoreType.Googleplay),
				json["productId"], json["internalProductId"], json["price"],
				json["currency"], json["receipt"], json["token"]);
			promise.Resolve(product);
		}

		Promise<Done> IStore.TerminatePurchase(PurchasedProduct product) {
			// Already in progress? Refuse immediately.
			lock (this) {
				if (LastTerminatePurchasePromise != null) {
					return Promise<Done>.Rejected(new CotcException(ErrorCode.AlreadyInProgress, "Terminating purchase"));
				}
				LastTerminatePurchasePromise = new Promise<Done>();
			}

			Bundle arg = Bundle.CreateObject();
			arg["token"] = product.Token;
			arg["internalProductId"] = product.InternalProductId;

			// Will call back the CotcInappPurchaseGameObject
			JavaClass.CallStatic("terminatePurchase", arg.ToJson());
			return LastTerminatePurchasePromise;
		}

		// Callback from native code
		void IStore.TerminatePurchase_Done(string message) {
			// Extract promise and allow again
			Promise<Done> promise;
			lock (this) {
				promise = LastTerminatePurchasePromise;
				LastTerminatePurchasePromise = null;
			}

			promise.Resolve(new Done(true, Bundle.Empty));
		}

		/// <summary>Parses an error coming from an unity message sent from Android.</summary>
		/// <param name="bundle">error as received from Android, parsed to JSON.</param>
		/// <returns>an exception</returns>
		private CotcException ParseError(Bundle bundle) {
			return new CotcException((ErrorCode) bundle["error"].AsInt(), bundle["description"]);
		}
	}
}
#endif
