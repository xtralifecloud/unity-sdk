In-app purchases {#store}
===========

In-app purchase functionality is provided with the in-app purchase package, which may be downloaded separately. The API reference provided by the package @ref CotcSdk.InappPurchase.CotcInappPurchaseGameObject "is available here".

CotC offer services that make it easier to provide in-app purchases within your game, while maintaining a familiar and reassuring user experience on each platform. That means that the purchases are made through the AppStore on iOS or Google Play on Android for instance.

This also means that you need to configure the products to be sold within your application using the corresponding backend for each platform. Typically, if you are selling your application to iOS and Android devices, you will need to have an AppStore as well as a Google Play Developer membership. This configuration needs to be partially replicated on Clan of the Cloud servers in order for it to present a catalog to the player and perform security checks on purchases. The backoffice is used for this job as described below.

As a very generic overview, an in-app purchase may involve the following processes:

1. The list of products sold as well as other information required to verify purchases is configured on the Clan of the Cloud servers through the backoffice.
2. Your app must be configured on the app store backend corresponding to the operating system for which the app will be distributed (information about the app, prices, etc. just like any app).
3. Once everything is set-up correctly on the app store, CotC can be used to present a list of products along with their price. CotC only stores the name of the product on the app store, this information is enriched on the client side by performing a query to the store.
4. A purchase can be launched for any listed product through the CotC SDK.
5. The purchase receipt from the app store is sent to Clan of the Cloud servers where the transaction is checked for genuineness.
6. If the transaction is deemed valid, a reward is optionally given to the player by running an user-defined transaction.
7. The result is returned to the client which closes the transaction (or consumes the product) so that it can be purchased again later.
8. The purchase history can be used to determine whether the player has acquired a given product or not. This should be made prior to launching a purchase, as products can be purchased multiple times, and depending on the actual product you might want to prevent that to happen.

One important thing to note is that purchases are consumed on the device in order to allow buying them again later. Meaning that track about them may be difficult to recover. And while a purchase is related to an iTunes account, the recorded purchase is associated with a Clan of the Cloud account. It means that if an user creates a new account on Clan of the Cloud, even though he is still using the same iTunes account, his purchase history will start out blank.

Configuring products
-----------

### For iOS ###

Products must be configured on iTunes. First, connect with your user name on [iTunes Connect](https://itunesconnect.apple.com/WebObjects/iTunesConnect.woa).

If you have not configured your app yet, click on the plus sign in the top-left corner and choose New iOS app. Follow the instructions; you may need to register an additional bundle identifier in the Developer Center. What you choose here is not relevant in regard to your integration with CotC. It will be used when distributing your app on the AppStore.

**Advice:** even if you are eager to test your integration, we recommend you to fill this information thoroughly. Some of it might be difficult to change later on.

Click on the create button. You will be redirected to the interface allowing to configure your app. There, you need to fill general information about your application. In general, we recommend you to fill as much information as possible. But in any case, you need to fill enough information so that the circular sign besides the application logo becomes yellow, indicating "Ready for submission".

Then open the "In-app Purchases" tab. Click on create new, select "consumable" to create a new product. Add enough information (including a screenshot and a default translation) so that the indicator becomes yellow (ready to submit). What is relevant here is the **Product identifier** (the reference name is just for yourself). This is the information that you will report to the Clan of the Cloud backoffice in order for the product to be purchasable with the SDK.

Then, open the Clan of the Cloud backoffice and under Games -> Store, create a new product. Fill out at least the *Product ID on the AppStore* field with the product identifier as noted before.

Now you should be ready to test. For that you need to create test users (sandbox users) as described [here (external link)](https://developer.apple.com/library/ios/documentation/LanguagesUtilities/Conceptual/iTunesConnectInAppPurchase_Guide/Chapters/TestingInAppPurchases.html). On the target test device, you need to sign out from the AppStore (from the system settings). Do not sign in with your test account from this screen, as you will be prompted to input your credit card information, which you do not want. Launch your app and when prompted to sign in, do so with your test account. The purchase should work. If not, please verify all settings (yellow lights for both the product and app state in iTunes Connect, valid `appStoreId` on the Clan of the Cloud backoffice, properly signed app, make sure that the account appears under **Users and roles**, **Sandbox testers** and that the app and product are marked as available in the country of your test user).

**Note:** while fetching the list of products does work on the simulator, actually purchasing any product will fail due to internal encryption processes. You need to use a real device to test in-app purchases, even in sandbox.

**Note on environments:** Apple offer two separated environments to perform in-app purchases, the sandbox and production environment. Clan of the Cloud sandbox environment is configured to work with the Apple sandbox environment, while the Clan of the Cloud production environment is configured to work with the Apple production environment. Therefore, in order to perform test purchases, you need to use our sandbox environment as well.

### For Android ###

If you want to test quickly on Android, you do not need to register your application as instructed below. This is only necessary to perform real life tests, and we highly recommend you register your application properly before you start doing extensive tests on your application. Therefore, to get started quickly you may simply use the product name `android.test.purchased` when creating your product in the Clan of the Cloud backoffice. This is a reserved name for a product whose purchase always succeeds regardless of the application state or the credit card used. You do not need to have an account on the Google Play Developer Console or pay a membership either.

You must input information about the products to be sold in the Google Play Developer Console. Follow the URL below and connect using your Google account. Note that you need to pay a membership in order to access the [Developer Console](https://play.google.com/apps/publish/).

You need to prepare your application for purchase. First you need to input the list of products, which is described in [Add Your Application to the Developer Console (external link)](http://developer.android.com/training/in-app-billing/preparing-iab-app.html#AddToDevConsole). You do not need follow the later steps in this document (adding the AIDL and such).

Then comes the difficult part. You need to prepare your application so that the Google Play Store will list your products, and this can sometimes be a pain to get working unless you choose to publish your application. Also, we recommend starting to prepare your app for distribution long ahead of your plans to start testing, as some simple configuration may take hours until they propagate through Google servers. The steps that should be necessary are:

1. Make sure that the [Developer Console](https://play.google.com/apps/publish/) tells you that your application is ready for publishing. Perform any steps as stated on the UI.
2. Your products should be marked as active (this does not actually publish anything until you publish the application).
3. Sign your APK using a production key. You should create a new keystore as described [here](http://developer.android.com/tools/publishing/preparing.html#publishing-build).
4. Upload an alpha version APK through the APK tab. Be careful: the APK must be signed and the key used to sign your application with will be the reference one for any subsequent upload, be it alpha, beta or production, so keep it safe.
5. Create a test user as described on [this page](http://developer.android.com/training/in-app-billing/test-iab-app.html). For more success, try to make this user the only Google account set-up on the device. At the very least, make sure that the Google Play Store app display this account by default when you unroll the left menu. This user must then be entered in the Google Play Developer Console under settings (gear icon), Account details, License testing. This change might take up to 24 hours to effectively take effect. Make sure that you spelled the account e-mail address correctly and try testing regularly once your integration is complete.
6. Open the Clan of the Cloud backoffice. Under the Status page, go to the *Push notification & Store certificates* section. Check the box next to Android and fill out the Package ID (it corresponds to the package as declared in your manifest). Next, you need to generate a service account in order to allow CotC servers to check purchase receipts for validity. You may do so directly from the Google Play Developer Console under Settings (gear icon), API access and follow the instructions under the Linked project section. Or, from the Google Developers Console, create a project if you don't already have one. Go to APIs & Auth, Credentials. Click Add credentials and select Service Account. Upon creating your account, you will be prompted to download a JSON file, which represents the certificate. Open the JSON file and paste its contents in the Google Service Account text field of the CotC backoffice. This account needs to be linked to your Google Play Account. Go back to the Google Play Developer Console and under Settings (gear icon), under API access, link it with the account you created earlier. Ensure that the permissions allow for checking in-app purchases. To do so, click on the View permissions link next to the displayed user.
7. Again from the CotC backoffice, under the Games -> Store, create a new product and operate as usual. The *SKU on Google Play* field must at least be filled. Alternatively, if you have already created a product for iOS and you simply want to provide the same functionality to Android users, you may as well simply edit the existing product and fill out the *SKU on Google Play* field.
8. The user of the service account should have at least the rights to view financial reports, else the purchase cannot be verified on the CotC servers. If you get an error such as follows after a successful purchase, check that twice.
```{ name: 'PurchaseNotConfirmed',
  message: 'The purchase has not been verified, code: 2. Error checking the purchase: {}.' }```

![Creating a Service Account in the Google Developer Console](./img/google-inapp-config01.png)
![Linking the Service Account to the project in the Google Play Developer Console](./img/google-inapp-config02.png)
![Giving the administrative rights to the account](./img/google-inapp-config03.png)

Configuring your application
-----------

There are a few basic steps that you need to take in your applications in order to take advantage of in-app purchases. These are described below.

### For iOS ###

On iOS and Mac OS X, you only need to verify that your binary is linked against the StoreKit framework. This can be done from the project options, selecting your target, and under **Build phases**, **Link binary with libraries**, click the plus button and add `StoreKit.framework`.

### For Android ###

On Android, you do not need anything special. The supplied plugin in AAR form will automatically modify your manifest as required.

API basics
-----------

The InappPurchaseSampleScene scene and associated script, bundled with the plugin, are a very good start. The integration requires multiple steps, but they ensure a maximum of flexibility and security. The #CotcSdk.GamerStore object is accessed through a logged in #CotcSdk.Gamer and provide CotC backend related functions. The other is the #CotcSdk.InappPurchase.CotcInappPurchaseGameObject which exposes functions that interact with the platform dependent store. You will need to mix the two in order to perform a purchase. Typically, we have:

1. List configured products on CotC (#CotcSdk.GamerStore.ListConfiguredProducts)
2. Enrich products with information from the native store (#CotcSdk.InappPurchase.CotcInappPurchaseGameObject.FetchProductInfo)
3. Launch purchase for one of these enriched products (#CotcSdk.InappPurchase.CotcInappPurchaseGameObject.LaunchPurchase)
4. Validate the financial transaction against CotC servers (#CotcSdk.GamerStore.ValidateReceipt)
5. In case of successful purchase, close the device-dependent store transaction (#CotcSdk.InappPurchase.CotcInappPurchaseGameObject.CloseTransaction).

Querying the list of available products
-----------

Prior to starting any purchase, the first thing that a game should do is to query the products available for sale. This can be used to show an UI to the player indicating what he is able to purchase and possibly the price. Querying product information can be done as in the snippet below.

~~~~{.cs}
var inApp = FindObjectOfType<CotcInappPurchaseGameObject>();

Gamer.Store.ListConfiguredProducts()
.Then((List<ConfiguredProduct> backofficeProducts) => {
	// Now enrich this basic information
	return inApp.FetchProductInfo(backofficeProducts);
})
.Then((List<ProductInfo> enrichedProducts) => {
	foreach (var pi in enrichedProducts) {
		Debug.Log(pi.ToJson());
	}
})
.Done();
~~~~

The #CotcSdk.InappPurchase.ProductInfo object contains a lot of information about the product to be purchased, regardless of the current platform. This is what should be used to present a store front to the user.

The first 30 products are returned by default, and pagination may be used as described under #CotcSdk.PagedList<DataType>.

With `FetchProductInfo`, the product information for each product configured on the Clan of the Cloud backoffice for this game is consolidated with the product information returned by the app store. If product information for a given product can not be fetched, the product is removed from the list. This means that even though products are configured on the backoffice, if something is wrong on the servers of the app store, in the above snippet `backofficeProducts` will be non-empty but `enrichedProducts` may be.

Launching a purchase
-----------

Launching a purchase brings a store front end to the customer, prompting him to purchase the product. The UI is the native one as expected by your players. The result handler is called whether the purchase is successful or failed.

**Notes:** While launching a purchase is straightforward using CotC, the process that does run on the device is not. Therefore, CotC **can not guarantee** that you will receive a response for a given call to `LaunchPurchase` (that is, the handler is ever called). The application might have a shorter livecycle. CotC does however offer a guarantee that no product will have to be purchased twice by the client without the purchase being registered properly on the Clan of the Cloud servers, unless the application is uninstalled after a purchase that failed to be validated to Clan of the Cloud servers.

After having received a positive result from the purchase process, you should validate it through CotC. Purchases usually trigger a transaction as configured in the backoffice, that might give the player additional abilities or otherwise affect his inventory. It means that you should reload the _balance_ after any purchase reported as successful. Also, it is very important that you close the transaction once CotC reports your purchase as taken in account successfully. Unless a completed transaction is closed, the product will be reported as purchased for any additional attempts to purchase it. This protection prevents that any crash happening between the purchase on the store and the validation on CotC servers makes the product "lost" (purchased, but not delivered).

Here is how you may drive a purchase flow once the #CotcSdk.ConfiguredProduct is chosen:

~~~~{.cs}
var inApp = FindObjectOfType<CotcInappPurchaseGameObject>();

inApp.LaunchPurchase(Gamer, configuredProduct)
.Then(purchasedProduct => {
	Debug.Log("Purchase ok: " + purchasedProduct.ToString());

	return Gamer.Store.ValidateReceipt(
		purchasedProduct.StoreType,
		purchasedProduct.CotcProductId,
		purchasedProduct.PaidPrice,
		purchasedProduct.PaidCurrency,
		purchasedProduct.Receipt)
	.Then(validationResult => {
		Debug.Log("Validated receipt");

		return inApp.CloseTransaction(purchasedProduct)
		.Then(done => {
			Debug.Log("Purchase transaction completed successfully: " + validationResult.ToString());
		});
	});
})
.Done();
~~~~

Retrieving the purchase history
-----------

You can query the history of purchased products for the logged in user. Note that this won't actually query the history of purchases made on the current device, but by the current Clan of the Cloud user. This distinction is important, and means that a player having purchased something must avoid losing his Clan of the Cloud account.

Querying the product list may be done as shown in the following snippet. Take a look at the #CotcSdk.GamerStore.GetPurchaseHistory method for information about what is precisely returned.

~~~~{.cpp}
Gamer.Store.GetPurchaseHistory()
.Then((List<PurchaseTransaction> transactions) => {
	foreach (var t in transactions) {
		Debug.Log(t.ToJson());
	}
})
.Done();
~~~~
