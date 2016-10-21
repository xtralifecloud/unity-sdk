using System;
using System.Collections.Generic;
using UnityEngine;

namespace CotcSdk.InappPurchase {
	
	public class CotcInappPurchaseGameObject : MonoBehaviour {

		private IStore Store;

		void Start() {
#if UNITY_EDITOR
			Store = null;
			Debug.LogError("In-app purchase not available on this platform");
#elif UNITY_ANDROID
			Store = new GooglePlayStoreImpl(gameObject.name);
#elif UNITY_IPHONE
			Store = new AppStoreImpl(gameObject.name);
#elif UNITY_EDITOR_OSX
			Store = null;
			Debug.LogError("In-app purchase not available on this platform: you need to build it standalone and configure it on iTunes Connect");
#elif UNITY_STANDALONE_OSX
			Store = new MacAppStoreImpl();
#else
			Store = null;
			Debug.LogError("In-app purchase not available on this platform");
#endif
		}

		/// <summary>
		/// Extremely important call to be made in case success is returned from #LaunchPurchase and
		/// then validated to #CotcSdk.GamerStore.ValidateReceipt. Failure to do that may leave
		/// transactions in an open state and disrupt the purchase process (delivering several times
		/// the product, etc.).
		/// </summary>
		public Promise<Done> CloseTransaction(PurchasedProduct product) {
			return Store.TerminatePurchase(product);
		}

		/// <summary>
		/// Enriches a catalog fetched by #CotcSdk.GamerStore.ListConfiguredProducts with price
		/// information and such.
		/// </summary>
		/// <returns>promise resolved when the operation has completed. Contains a non-paginated list
		/// of products configured on the backend.</returns>
		public Promise<List<ProductInfo>> FetchProductInfo(List<ConfiguredProduct> products) {
			return Store.GetInformationAboutProducts(products);
		}

		/// <summary>
		/// Launches a purchase process for a given product. The purchase process is asynchronous,
		/// may take a lot of time and may not necessarily ever finish (i.e. the application may be
		/// killed before it actually finishes). That said, only one purchase process is running at
		/// at a time.
		/// </summary>
		/// <returns>promise resolved when the purchase completes with information about the purchased
		/// item. In order to validate the transaction, you need to call
		/// #CotcSdk.GamerStore.ValidateReceipt. More important, you MUST then call
		/// #CloseTransaction in case of success!</returns>
		/// <param name="gamer">gamer to associate with the purchase</param>
		/// <param name="info">information about the product to be purchased. Obtained via #FetchProductInfo.</param>
		public Promise<PurchasedProduct> LaunchPurchase(Gamer gamer, ProductInfo info) {
			return Store.LaunchPurchaseFlow(gamer, info);
		}

#if UNITY_ANDROID || UNITY_IPHONE
		// Got from the GooglePlayStoreImpl when GetInformationAboutProducts calls back
		void GetInformationAboutProducts_Done(string message) {
			Store.GetInformationAboutProducts_Done(message);
		}

		// Got from the GooglePlayStoreImpl when LaunchPurchase calls back
		void LaunchPurchase_Done(string message) {
			Store.LaunchPurchase_Done(message);
		}

		void TerminatePurchase_Done(string message) {
			Store.TerminatePurchase_Done(message);
		}
#endif
	}
}
