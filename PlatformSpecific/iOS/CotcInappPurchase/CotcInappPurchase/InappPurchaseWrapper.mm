#import "InappPurchaseWrapper.h"
#import "safe.h"

@implementation InappPurchaseWrapper

- (id)init {
	if ((self = [super init])) {
	}
	return self;
}

- (void)listProducts:(NSArray *)productInfo callback:(function<void(NSDictionary*)>)callback {
	NSMutableSet *productIdentifiers = [NSMutableSet set];
	for (NSDictionary *node in productInfo) {
		// Do not show those which are not configured for AppStore
		NSString *pid = [node objectForKey:STORE_PRODUCT_ID];
		if (pid) {
			[productIdentifiers addObject:pid];
		}
	}

	SKProductsRequest *productsRequest = [[SKProductsRequest alloc] initWithProductIdentifiers:productIdentifiers];

	[self setCallback:[=] (SKProductsResponse *response, NSError *error) {
		if (error) {
			return callback([self makeErrorJson:ErrorWithExternalStore desc:error.description.UTF8String]);
		}
		
		NSMutableDictionary *json = [NSMutableDictionary dictionary];
		// Report invalid products in the console
		if (response.invalidProductIdentifiers.count > 0) {
			char result[1024], temp[1024];
			int index = 0;
			safe::strcpy(result, "");
			for (NSString *productId in response.invalidProductIdentifiers) {
				safe::sprintf(temp, "%s%s", index++ > 0 ? ", " : "", productId.UTF8String);
				safe::strcat(result, temp);
			}
			NSLog(@"Queried invalid product identifiers: [%s]", result);
		}
		
		// Fetch product info and return it to the caller
		NSMutableArray *products = [NSMutableArray array];
		for (SKProduct *product in response.products) {
			NSMutableDictionary *node = [NSMutableDictionary dictionary];
			NSNumberFormatter *formatter = [[NSNumberFormatter alloc] init];
			[formatter setLocale:product.priceLocale];
			
			// We could add much more info, but let's limit to that for now
			[node setValue:product.productIdentifier forKey:@"internalProductId"];
			[node setValue:product.price forKey:@"price"];
			[node setValue:formatter.currencyCode forKey:@"currency"];
			
			// Find the CotC product ID corresponding to the AppStore product ID
			for (NSDictionary *p in productInfo) {
				if ([[p objectForKey:STORE_PRODUCT_ID] isEqualToString:product.productIdentifier]) {
					[node setValue:[p objectForKey:@"productId"] forKey:@"productId"];
					break;
				}
			}

			[products addObject:node];
		}
		
		[json setValue:products forKey:@"products"];
		return callback(json);
	} forNextRequest:productsRequest];

	productsRequest.delegate = self;
	[productsRequest start];
}

// MARK: SKRequestDelegate protocol
- (void)requestDidFinish:(SKRequest *)request {
	[self invokeHandlerForRequest:request withResponse:nil andError:nil];
}

- (void)request:(SKRequest *)request didFailWithError:(NSError *)error {
	// We do not actually care about the error
	[self invokeHandlerForRequest:request withResponse:nil andError:error];
}

// MARK: SKProductsRequest delegate
- (void)productsRequest:(SKProductsRequest *)request didReceiveResponse:(SKProductsResponse *)response {
	[self invokeHandlerForRequest:request withResponse:response andError:nil];
}

// MARK: SKPaymentTransactionObserver protocol
- (void)paymentQueue:(SKPaymentQueue *)queue updatedTransactions:(NSArray *)transactions {
/*	for (SKPaymentTransaction *transaction in transactions) {
		[self processTransaction:transaction];
	}*/
}

// MARK: Private
- (void)invokeHandlerForRequest:(SKRequest *)request withResponse:(SKProductsResponse *)response andError:(NSError *)error {
	// Unexpected request
	if (productsResponseHandlers.find(request) == productsResponseHandlers.end()) { return; }
	// Move ownership to a temp object as we'll remove it from the list from this thread
	auto handler = productsResponseHandlers[request];
	productsResponseHandlers.erase(request);
	
	dispatch_async(dispatch_get_main_queue(), ^{
		handler(response, error);
	});
}

- (NSMutableDictionary *)makeErrorJson:(ErrorCode)code desc:(const char *)description {
	NSMutableDictionary *json = [NSMutableDictionary dictionary];
	[json setValue:[NSString stringWithUTF8String:description] forKey:@"description"];
	[json setValue:[NSNumber numberWithInt:code] forKey:@"error"];
	return json;
}

- (void)setCallback:(function<void (SKProductsResponse *, NSError *)>)callback forNextRequest:(SKProductsRequest *)request {
	productsResponseHandlers[request] = callback;
}

@end
