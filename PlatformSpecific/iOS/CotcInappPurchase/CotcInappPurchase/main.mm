#import "main.h"
#import "InappPurchaseWrapper.h"

// MARK: Declarations
extern "C" void UnitySendMessage(const char* obj, const char* method, const char* msg);
/** Returns a non-nil value if the JSON data was decoded successfully (either NSDictionary or NSArray). */
static id jsonFromString(const char *jsonString);
/** Takes a NSDictionary or NSArray. */
static cstring jsonToString(id json);

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
		[g_inappWrapper listProducts:json callback:[=] (NSDictionary *resultJson) {
			// Call back with the result
			cstring str = jsonToString(resultJson);
			UnitySendMessage(g_callbackObjectName, CB_LISTPRODUCTS, str.c_str());
		}];
	}
}

void CotcInappPurchase_launchPurchase(const char *productJson) {
	
}

void CotcInappPurchase_terminatePurchase(const char *paramsJson) {
	
}

// MARK: Private method implementation
id jsonFromString(const char *jsonString) {
	NSError *e = nil;
	NSData *data = [NSData dataWithBytes:jsonString length:strlen(jsonString)];
	id json = [NSJSONSerialization JSONObjectWithData:data options:NSJSONReadingMutableContainers error:&e];
	if (!json) {
		NSLog(@"Error parsing JSON: %@", e);
	}
	return json;
}

cstring jsonToString(id json) {
	NSError *error = nil;
	NSData *data = [NSJSONSerialization dataWithJSONObject:json options:0 error:&error];
	if (error) {
		NSLog(@"Error when serializing JSON: %@", error.localizedDescription);
	}
	// Copy buffer to string
	char *str = (char*) malloc(data.length + 1);
	memcpy(str, data.bytes, data.length);
	str[data.length] = '\0';
	return cstring(str, true);
}
