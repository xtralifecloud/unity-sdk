using System;
using System.Collections.Generic;

namespace CotcSdk.InappPurchase {

	/**
	 * Interface about the platform-specific store.
	 */
	interface IStore {

		/**
		 * Implements the actual listing of products. Note that this is not necessarily called when
		 * the customer is actually querying the list of products.
		 * @param products list of products to query information for on the store. Should come from the product
		 *     info as returned by Cotc.
		 * @return promise resolved when the operation has completed, with consolidated product information.
		 */
		Promise<List<ProductInfo>> GetInformationAboutProducts(List<ConfiguredProduct> products);

		/**
		 * Implements the actual purchase flow. This does not represent the whole purchase process
		 * though, only the first part where the store front end is presented to the user and allows
		 * him to pay. The purchase process will also incur an additional step consisting in a
		 * verification of the payment before actually delivering the goods. That is why you need to
		 * call #CotcSdk.GamerStore.SendStorePurchaseToServer after your purchase has been done
		 * and forward the result.
		 * @param product product to be purchased, as returned by #GetInformationAboutProducts.
		 * @return promise resolved when the purchase has completed successfully.
		 */
		Promise<PurchasedProduct> LaunchPurchaseFlow(ProductInfo product);
	}
}
