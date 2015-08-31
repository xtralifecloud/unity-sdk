using System;
using UnityEngine;
using CotcSdk;
using System.Reflection;
using IntegrationTests;
using System.Threading;
using CotcSdk.InappPurchase;

/**
 * These tests can not test device specific methods unfortunately.
 * So it tests it but does not assess any result.
 */
public class StoreTests : TestBase {

	[InstanceMethod(typeof(StoreTests))]
	public string TestMethodName;
	
	private const string BoConfiguration = "Needs a product like that in the BO: {\"reward\":{\"domain\":\"private\",\"description\":\"Test\",\"tx\":{\"coins\":100}},\"productId\":\"cotc_product1\",\"googlePlayId\":\"android.test.purchased\"}";

	void Start() {
		RunTestMethod(TestMethodName);
	}

	[Test("This test uses store methods (nothing related to the device-specific in-app plugin).", BoConfiguration)]
	public void ShouldPerformFakePurchase(Cloud cloud) {
		LoginNewUser(cloud, gamer => {
			string transactionId = "transaction." + Guid.NewGuid();
			string receiptJson = "{\"packageName\":\"com.clanofthecloud.cli\",\"orderId\":\"" + transactionId + "\",\"productId\":\"android.test.purchased\",\"developerPayload\":\"\",\"purchaseTime\":0,\"purchaseState\":0,\"purchaseToken\":\"inapp:com.clanofthecloud.cli:android.test.purchased\"}";
			// Fetch the catalog
			gamer.Store.ListConfiguredProducts()
			.ExpectSuccess(productList => {
				// Look for a result in all pages
				return ProcessAllPages(productList, page => {
					foreach (ConfiguredProduct p in page) {
						if (p.GooglePlayId == "android.test.purchased" && p.ProductId == "testproduct") {
							return true;
						}
					}
					return false;
				});
			})
			.ExpectSuccess(productFound => {
				Assert(productFound, "Did not find testproduct / android.test.purchased in the list");
				return gamer.Store.ValidateReceipt(StoreType.Googleplay, "testproduct", 1, "CHF", receiptJson);
			})
			.ExpectSuccess(result => {
				// Receipt validated -> Check that it has been taken in the history
				return gamer.Store.GetPurchaseHistory();
			})
			.ExpectSuccess(history => {
				Assert(history.Count == 1, "Should only contain one item");
				Assert(history[0].Currency == "CHF", "Invalid history currency");
				Assert(history[0].Price == 1, "Invalid history price amount");
				Assert(history[0].ProductId == "testproduct", "Invalid history product ID");
				Assert(history[0].Store == StoreType.Googleplay, "Invalid history store type");
				// The server may slightly decorate the transaction ID, especially for a constant tx ID such as android.test.purchased
				Assert(history[0].StoreTransactionId.StartsWith(transactionId), "Invalid history store transaction ID");
				// Then check the balance, should have gotten 100 coins
				return gamer.Transactions.Balance();
			})
			.ExpectSuccess(balance => {
				Assert(balance["coins"] == 100, "Balance not adjusted properly. Also check configuration on backoffice.");
				CompleteTest();
			});
		});
	}

#if false
	[Test("Tests the native plugin as well.", BoConfiguration)]
	public void ShouldUseNativePurchasePlugin(Cloud cloud) {
		LoginNewUser(cloud, gamer => {
			var productToBeBought = new ConfiguredProduct[1];
			var inappObject = FindObjectOfType<CotcInappPurchaseGameObject>();

			// Fetch the catalog
			gamer.Store.ListConfiguredProducts()
			.ExpectSuccess(productList => {
				// Fetch products natively
				return inappObject.FetchProductInfo(productList);
			})
			.ExpectSuccess(enrichedProducts => {
				// Sadly, depending on the device it is run on, we can't expect anything to be present or not in this list
				// So just expect a success
				CompleteTest();
			});
		});
	}
#endif
}
