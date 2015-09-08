using System;
using System.Collections.Generic;

namespace CotcSdk.InappPurchase {

	/// <summary>Interface about the platform-specific store.</summary>
	interface IStore {

		/// <summary>
		/// Implements the actual listing of products. Note that this is not necessarily called when
		/// the customer is actually querying the list of products.
		/// </summary>
		/// <param name="products">list of products to query information for on the store. Should come from the product
		/// info as returned by Cotc.</param>
		/// <returns>promise resolved when the operation has completed, with consolidated product information.</returns>
		Promise<List<ProductInfo>> GetInformationAboutProducts(List<ConfiguredProduct> products);

		/// <summary>
		/// Implements the actual purchase flow. This does not represent the whole purchase process
		/// though, only the first part where the store front end is presented to the user and allows
		/// him to pay. The purchase process will also incur an additional step consisting in a
		/// verification of the payment before actually delivering the goods. That is why you need to
		/// call #CotcSdk.GamerStore.ValidateReceipt after your purchase has been done and forward the
		/// result.
		/// </summary>
		/// <param name="gamer">gamer to associate with the purchase.</param>
		/// <param name="product">product to be purchased, as returned by #GetInformationAboutProducts.</param>
		/// <returns>promise resolved when the purchase has completed successfully.</returns>
		Promise<PurchasedProduct> LaunchPurchaseFlow(Gamer gamer, ProductInfo product);

		/// <summary>
		/// Completes the purchase process. This step is mandatory in order to close the purchase transaction.
		/// You can not start another purchase until you haven't validated the previous one. In order to
		/// validate it, you need to pass it to CotC servers via the #CotcSdk.GamerStore.ValidateReceipt
		/// method. In case of success, do not forget to call TerminatePurchase. This operation is local and
		/// should not fail.
		/// </summary>
		/// <param name="product">product as returned by LaunchPurchaseFlow.</param>
		/// <returns>a promise that is resolved when the native operation has completed.</returns>
		Promise<Done> TerminatePurchase(PurchasedProduct product);

		// ------------- Callback messages used by some native implementations -------------
		void GetInformationAboutProducts_Done(string message);
		void LaunchPurchase_Done(string message);
		void TerminatePurchase_Done(string message);
	}
}
