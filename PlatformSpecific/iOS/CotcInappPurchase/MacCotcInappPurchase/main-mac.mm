#import "main-mac.h"
#import "InappPurchaseWrapper.h"
#include "utils.h"

// MARK: Globals
static InappPurchaseWrapper *g_inappWrapper;

// MARK: Interface implementation
void CotcInappPurchase_startup() {
	g_inappWrapper = [[InappPurchaseWrapper alloc] initWithAppStore:MAC_STORE];
}

void CotcInappPurchase_listProducts(const char *productsJson, Delegate onFinished) {
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
			onFinished(jsonToString(json));

		} error:[=] (ErrorCode code, const char *desc) {
			// Forward error
			onFinished(makeErrorJson(code, desc));
		}];
	}
}

void CotcInappPurchase_launchPurchase(const char *productJson, Delegate onFinished) {
	@autoreleasepool {
		NSDictionary *json = jsonFromString(productJson);
		NSString *userName = [json objectForKey:@"userName"];
		ProductInfo pi(json);
		
		[g_inappWrapper launchPurchaseFlow:pi forUser:userName.UTF8String success:[=] (const PurchasedProduct &p) {
			// Call back with the result
			NSDictionary *json = p.toJson();
			onFinished(jsonToString(json));
			
		} error:[=] (ErrorCode code, const char *desc) {
			// Forward error
			onFinished(makeErrorJson(code, desc));
		}];
	}
}

void CotcInappPurchase_terminatePurchase(const char *paramsJson, Delegate onFinished) {
	@autoreleasepool {
		NSDictionary *json = jsonFromString(paramsJson);
		NSString *transactionId = [json objectForKey:@"token"];
		
		[g_inappWrapper terminatePurchase:transactionId success:[=] {
			onFinished("{}");

		} error:[=] (ErrorCode code, const char *desc) {
			// Forward error
			onFinished(makeErrorJson(code, desc));
		}];
	}
}
