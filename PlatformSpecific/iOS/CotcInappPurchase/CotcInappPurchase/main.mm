#import "main.h"
#import "InappPurchaseWrapper.h"

// MARK: Declarations
extern "C" void UnitySendMessage(const char* obj, const char* method, const char* msg);
/** Returns a non-nil value if the JSON data was decoded successfully (either NSDictionary or NSArray). */
static id jsonFromString(const char *jsonString);
/** Takes a NSDictionary or NSArray. */
static cstring jsonToString(id json);
static cstring makeErrorJson(ErrorCode code, const char *desc);

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
	g_inappWrapper = [[InappPurchaseWrapper alloc] init];
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

// MARK: Private method implementation
id jsonFromString(const char *jsonString) {
	NSError *e = nil;
	NSData *data = [NSData dataWithBytes:jsonString length:strlen(jsonString)];
	id json = [NSJSONSerialization JSONObjectWithData:data options:NSJSONReadingMutableContainers error:&e];
	if (!json) {
		Log(@"Error parsing JSON: %@", e);
	}
	return json;
}

cstring jsonToString(id json) {
	NSError *error = nil;
	NSData *data = [NSJSONSerialization dataWithJSONObject:json options:0 error:&error];
	if (error) {
		Log(@"Error when serializing JSON: %@", error.localizedDescription);
	}
	// Copy buffer to string
	char *str = (char*) malloc(data.length + 1);
	memcpy(str, data.bytes, data.length);
	str[data.length] = '\0';
	return cstring(str, true);
}

cstring makeErrorJson(ErrorCode code, const char *desc) {
	NSMutableDictionary *json = [NSMutableDictionary dictionary];
	[json setValue:[NSString stringWithUTF8String:desc] forKey:@"description"];
	[json setValue:[NSNumber numberWithInteger:code] forKey:@"error"];
	return jsonToString(json);
}
