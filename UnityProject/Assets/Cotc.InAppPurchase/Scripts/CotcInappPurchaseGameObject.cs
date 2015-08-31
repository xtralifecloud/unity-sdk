using System;
using System.Collections.Generic;
using UnityEngine;

namespace CotcSdk.InappPurchase {

	public class CotcInappPurchaseGameObject : MonoBehaviour {

		private IStore Store;

		void Start() {
#if UNITY_ANDROID
			Store = new GooglePlayStoreImpl(gameObject.name);
#elif UNITY_IPHONE
			Store = new AppStoreImpl(gameObject.name);
#else
			Debug.LogError("In-app purchase not available on this platform");
			Store = null;
#endif
		}

		/**
		 * Extremely important call to be made in case success is returned from #LaunchPurchase and
		 * then validated to #CotcSdk.GamerStore.ValidateReceipt. Failure to do that may leave
		 * transactions in an open state and disrupt the purchase process (delivering several times
		 * the product, etc.).
		 */
		public Promise<Done> CloseTransaction(PurchasedProduct product) {
			return Store.TerminatePurchase(product);
		}

		/**
		 * Enriches a catalog fetched by #CotcSdk.GamerStore.ListConfiguredProducts with price
		 * information and such.
		 * @return promise resolved when the operation has completed. Contains a non-paginated list
		 *     of products configured on the backend.
		 */
		public Promise<List<ProductInfo>> FetchProductInfo(List<ConfiguredProduct> products) {
			return Store.GetInformationAboutProducts(products);
		}

		/**
		 * Launches a purchase process for a given product. The purchase process is asynchronous,
		 * may take a lot of time and may not necessarily ever finish (i.e. the application may be
		 * killed before it actually finishes). That said, only one purchase process is running at
		 * at a time.
		 * @return promise resolved when the purchase completes with information about the purchased
		 *     item. In order to validate the transaction, you need to call
		 *     #CotcSdk.GamerStore.ValidateReceipt. More important, you MUST then call
		 *     #CloseTransaction in case of success!
		 * @param gamer gamer to associate with the purchase
		 * @param info information about the product to be purchased. Obtained via #FetchProductInfo.
		 */
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
