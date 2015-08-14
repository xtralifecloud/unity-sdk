using System;
using System.Collections.Generic;
using UnityEngine;

namespace CotcSdk.InappPurchase {

	public class CotcInappPurchaseGameObject : MonoBehaviour {

		private IStore Store;
		private bool PurchaseInProgress;

		void Start () {
#if UNITY_ANDROID
			Store = new GooglePlayStoreImpl(gameObject.name);
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
		 * @param info information about the product to be purchased. Obtained via #FetchProductInfo.
		 */
		public Promise<PurchasedProduct> LaunchPurchase(ProductInfo info) {
			return Store.LaunchPurchaseFlow(info);
		}

#if UNITY_ANDROID
		// Got from the GooglePlayStoreImpl when GetInformationAboutProducts calls back
		void Android_GetInformationAboutProducts_Done(string message) {
			((GooglePlayStoreImpl)Store).GetInformationAboutProducts_Done(message);
		}

		// Got from the GooglePlayStoreImpl when LaunchPurchase calls back
		void Android_LaunchPurchase_Done(string message) {
			((GooglePlayStoreImpl)Store).LaunchPurchase_Done(message);
		}

		void Android_TerminatePurchase_Done(string message) {
			((GooglePlayStoreImpl)Store).TerminatePurchase_Done(message);
		}
#endif
	}

	/**
	 * Purchased product as returned by LaunchPurchase.
	 * TODO move
	 */
	public class PurchasedProduct {
		public StoreType StoreType { get; private set; }
		public string CotcProductId { get; private set; }
		public string InternalProductId { get; private set; }
		public float PaidPrice { get; private set; }
		public string PaidCurrency { get; private set; }
		public string Receipt { get; private set; }
		public string Token { get; private set; }

		internal PurchasedProduct(StoreType storeType, string cotcProductId, string internalProductId, float paidPrice, string paidCurrency, string receipt, string token) {
			StoreType = storeType;
			CotcProductId = cotcProductId;
			InternalProductId = internalProductId;
			PaidPrice = paidPrice;
			PaidCurrency = paidCurrency;
			Receipt = receipt;
			Token = token;
		}
	}

}
