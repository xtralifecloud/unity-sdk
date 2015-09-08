#import "main-ios.h"
#import "InappPurchaseWrapper.h"
#include "utils.h"

// MARK: Declarations
extern "C" void UnitySendMessage(const char* obj, const char* method, const char* msg);

// MARK: Globals
static cstring g_callbackObjectName;
static InappPurchaseWrapper *g_inappWrapper;
// Callbacks (messages sent to Unity) after operations completed
static const char *CB_LISTPRODUCTS = "GetInformationAboutProducts_Done";
static const char *CB_LAUNCHPURCHASE = "LaunchPurchase_Done";
static const char *CB_TERMINATEPURCHASE = "TerminatePurchase_Done";

// MARK: Interface implementation
void CotcInappPurchase_startup(const char *callbackGameObjectName) {
	g_callbackObjectName = callbackGameObjectName;
	g_inappWrapper = [[InappPurchaseWrapper alloc] initWithAppStore:IOS_STORE];
}

void CotcInappPurchase_listProducts(const char *productsJson) {
	@autoreleasepool {
		NSArray *json = jsonFromString(productsJson);
		vector<ConfiguredProduct> products;
		for (NSDictionary *p in json) {
			products.push_back(ConfiguredProduct(p));
		}

		// Process list of products
		[g_inappWrapper listProducts:products success:[=] (const vector<ProductInfo> &result) {
			// Call back with the result
			NSDictionary *json = [NSDictionary dictionaryWithObjectsAndKeys:makeJsonArray(result), @"products", nil];
			UnitySendMessage(g_callbackObjectName, CB_LISTPRODUCTS, jsonToString(json));

		} error:[=] (ErrorCode code, const char *desc) {
			// Forward error
			UnitySendMessage(g_callbackObjectName, CB_LISTPRODUCTS, makeErrorJson(code, desc));
		}];
	}
}

void CotcInappPurchase_launchPurchase(const char *productJson) {
	@autoreleasepool {
		NSDictionary *json = jsonFromString(productJson);
		NSString *userName = [json objectForKey:@"userName"];
		ProductInfo pi(json);
		
		[g_inappWrapper launchPurchaseFlow:pi forUser:userName.UTF8String success:[=] (const PurchasedProduct &p) {
			// Call back with the result
			NSDictionary *json = p.toJson();
			UnitySendMessage(g_callbackObjectName, CB_LAUNCHPURCHASE, jsonToString(json));
			
		} error:[=] (ErrorCode code, const char *desc) {
			// Forward error
			UnitySendMessage(g_callbackObjectName, CB_LAUNCHPURCHASE, makeErrorJson(code, desc));
		}];
	}
}

void CotcInappPurchase_terminatePurchase(const char *paramsJson) {
	@autoreleasepool {
		NSDictionary *json = jsonFromString(paramsJson);
		NSString *transactionId = [json objectForKey:@"token"];
//		NSString *productId = [json objectForKey:@"internalProductId"];
		
		[g_inappWrapper terminatePurchase:transactionId success:[=] {
			UnitySendMessage(g_callbackObjectName, CB_TERMINATEPURCHASE, "{}");

		} error:[=] (ErrorCode code, const char *desc) {
			// Forward error
			UnitySendMessage(g_callbackObjectName, CB_TERMINATEPURCHASE, makeErrorJson(code, desc));
		}];
	}
}
