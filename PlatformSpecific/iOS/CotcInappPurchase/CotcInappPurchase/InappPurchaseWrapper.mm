#import <CommonCrypto/CommonCrypto.h>
#import "InappPurchaseWrapper.h"
#import "safe.h"

@implementation InappPurchaseWrapper

- (id)init {
	if ((self = [super init])) {
		[[SKPaymentQueue defaultQueue] addTransactionObserver:self];
	}
	return self;
}

- (void)dealloc {
	[[SKPaymentQueue defaultQueue] removeTransactionObserver:self];
}

- (void)listProducts:(const vector<ConfiguredProduct> &)products
			 success:(function<void (const vector<ProductInfo> &)>)onSuccess
			   error:(function<void (ErrorCode, const char *)>)onError {

	NSMutableSet *productIdentifiers = [NSMutableSet set];
	for (const ConfiguredProduct &p: products) {
		// Do not show those which are not configured for AppStore
		if (p.appStoreId) {
			[productIdentifiers addObject:p.appStoreId];
		}
	}

	SKProductsRequest *productsRequest = [[SKProductsRequest alloc] initWithProductIdentifiers:productIdentifiers];

	// '='-capturing a vector<T>& will copy it for the block, thus lifecycle is okay
	[self setCallback:[=] (SKProductsResponse *response, NSError *error) {
		if (error) {
			return onError(ErrorCode::ErrorWithExternalStore, error.description.UTF8String);
		}
		
		// Report invalid products in the console
		if (response.invalidProductIdentifiers.count > 0) {
			char result[1024], temp[1024];
			int index = 0;
			safe::strcpy(result, "");
			for (NSString *productId in response.invalidProductIdentifiers) {
				safe::sprintf(temp, "%s%s", index++ > 0 ? ", " : "", productId.UTF8String);
				safe::strcat(result, temp);
			}
			Log(@"Queried invalid product identifiers: [%s]", result);
		}
		
		// Fetch product info and return it to the caller
		vector<ProductInfo> outProducts;
		for (SKProduct *product in response.products) {
			ProductInfo pi;
			NSNumberFormatter *formatter = [[NSNumberFormatter alloc] init];
			[formatter setLocale:product.priceLocale];
			
			// We could add much more info, but let's limit to that for now
			pi.internalProductId = product.productIdentifier;
			pi.price = product.price.floatValue;
			pi.currency = formatter.currencyCode;
			
			// Find the CotC product ID corresponding to the AppStore product ID
			for (const ConfiguredProduct &p: products) {
				if ([product.productIdentifier isEqualToString:p.appStoreId]) {
					pi.productId = p.productId;
					break;
				}
			}
			outProducts.push_back(pi);
		}
		
		return onSuccess(outProducts);
	} forNextRequest:productsRequest];

	productsRequest.delegate = self;
	[productsRequest start];
}

- (void)launchPurchaseFlow:(const ProductInfo &)product
				   forUser:(const cstring &)userName
				   success:(function<void (const PurchasedProduct &)>)onSuccess
					 error:(function<void (ErrorCode, const char *)>)onError {

	if (!userName) {
		return onError(ErrorCode::LogicError, "Missing userName parameter");
	}
	
	// We need to fetch the product list first…
	NSSet *productIds = [NSSet setWithObject:product.internalProductId];
	SKProductsRequest *productsRequest = [[SKProductsRequest alloc] initWithProductIdentifiers:productIds];
	[self setCallback:[=] (SKProductsResponse *response, NSError *error) {
		if (error) {
			return onError(ErrorCode::ErrorWithExternalStore, error.description.UTF8String);
		}
		if (response.products.count < 1) {
			return onError(ErrorCode::ErrorWithExternalStore, "The product is not listed in the store");
		}
		
		// Cancel previously set handler
		if (productPurchasedHandlers[product.internalProductId]) {
			productPurchasedHandlers[product.internalProductId](nil);
		}
		
		// To be executed upon successful purchase of this product
		productPurchasedHandlers[product.internalProductId] = [=] (SKPaymentTransaction *tx) {
			// Okay so a transaction was processed, either asynchronously as a result of SKPaymentQueue addPayment: or
			// directly since it has been notified before this call.
			productPurchasedHandlers[product.internalProductId] = nullptr;

			if (!tx) {
				// Happens just above (in case a handler had already been set)
				return onError(ErrorCode::Canceled, "No payment transaction");
			}
			
			if (tx.transactionState == SKPaymentTransactionStateFailed) {
				[self markTransactionAsFinished:tx];
				return onError(ErrorCode::ErrorWithExternalStore, "Payment failed or canceled");
			}
			else if (tx.transactionState == SKPaymentTransactionStatePurchased || tx.transactionState == SKPaymentTransactionStateRestored) {
				// Consolidate with receipt
				[self fetchReceipt:[=] (NSData *receiptData) {
					NSNumberFormatter *formatter = [[NSNumberFormatter alloc] init];
					SKProduct *storeProduct = response.products.firstObject;
					[formatter setLocale:storeProduct.priceLocale];
					
					PurchasedProduct result;
					result.storeType = STORE_TYPE;
					result.cotcProductId = product.productId;
					result.internalProductId = storeProduct.productIdentifier;
					result.paidPrice = storeProduct.price.floatValue;
					result.paidCurrency = formatter.currencyCode;
					result.receipt = [receiptData base64EncodedStringWithOptions:0];
					result.token = tx.transactionIdentifier;
					return onSuccess(result);

				} error:[=] (ErrorCode code, const char *desc) {
					return onError(code, desc);
				}];
			}
			else {
				Log(@"Unknown transaction to be handled: %@", @(tx.transactionState));
				return onError(ErrorCode::LogicError, "Unsupported transaction state");
			}
		};
		
		// Check if there is already a pending transaction
		if (pendingTransactions[product.internalProductId]) {
			Log(@"Handling pending tx for product %s", product.internalProductId.c_str());
			[self deliverTransaction:pendingTransactions[product.internalProductId]];
			return;
		}
		
		// Check system pending transactions (not yet accounted by us, so not in pendingTransactions)
		if (SKPaymentQueue.defaultQueue.transactions.count > 0) {
			Log(@"There are %d pending transactions, will perform them.", (int) SKPaymentQueue.defaultQueue.transactions.count);
			for (SKPaymentTransaction *tx in SKPaymentQueue.defaultQueue.transactions) {
				if ([tx.payment.productIdentifier isEqualToString:product.internalProductId] && [self shouldDeliverTransaction:tx]) {
					Log(@"Product %s already processing, discarding additional purchase attempt.", product.internalProductId.c_str());
					[self deliverTransaction:tx];
					return;
				}
			}
			Log(@"Product %s not in pending transactions, will purchase anyway.", product.internalProductId.c_str());
		}
		
		// Product found, prepare & launch payment
		SKMutablePayment *payment = [SKMutablePayment paymentWithProduct:response.products.firstObject];
		payment.quantity = 1;
		payment.applicationUsername = [self hashedValueForAccountName:userName];
		// Will continue at paymentQueue:updatedTransactions:
		[[SKPaymentQueue defaultQueue] addPayment:payment];
		
	} forNextRequest:productsRequest];

	productsRequest.delegate = self;
	[productsRequest start];
}

- (void)terminatePurchase:(const safe::cstring &)transactionId
				  success:(function<void ()>)onSuccess
					error:(function<void (ErrorCode, const char *)>)onError {
	
	for (SKPaymentTransaction *tx in SKPaymentQueue.defaultQueue.transactions) {
		if ([tx.transactionIdentifier isEqualToString:transactionId]) {
			[self markTransactionAsFinished:tx];
			return onSuccess();
		}
	}
	Log(@"Could not complete transaction %s: not found locally", transactionId.c_str());
	return onError(ErrorCode::ErrorWithExternalStore, "Transaction not found locally");
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
	int deliveredCount = 0;
	// Called at any point since this class is instantiated (which happens at startup)
	for (SKPaymentTransaction *transaction in transactions) {
		if ([self shouldDeliverTransaction:transaction]) {
			deliveredCount++;
			[self deliverTransaction:transaction];
		}
	}
	Log(@"Updated %d transactions (of which %d were ignored)", (int) transactions.count, (int) transactions.count - deliveredCount);
}

// MARK: Private
- (void)deliverTransaction:(SKPaymentTransaction *)tx {
	const char *productId = tx.payment.productIdentifier.UTF8String;
	if (![self shouldDeliverTransaction:tx]) {
		Log(@"Incorrectly processed transaction in state %@", @(tx.transactionState));
		return;
	}
	
	auto handler = productPurchasedHandlers[productId];
	if (!handler) {
		// No handler, keep for later
		pendingTransactions[productId] = tx;
	}
	else {
		// Won't be delivered again
		pendingTransactions[productId] = nullptr;
		// Execute success handler
		handler(tx);
	}
}

- (void)fetchReceipt:(function<void (NSData *)>)onSuccess
			   error:(function<void (ErrorCode, const char *)>)onError {

	NSURL *receiptURL = [[NSBundle mainBundle] appStoreReceiptURL];
	if ([[NSFileManager defaultManager] fileExistsAtPath:[receiptURL path]]) {
		// We can send the receipt directly
		onSuccess([NSData dataWithContentsOfURL:receiptURL]);
	}
	else {
		Log(@"AppStore receipt not found, refreshing");
		SKReceiptRefreshRequest *refreshReceiptRequest = [[SKReceiptRefreshRequest alloc] initWithReceiptProperties:@{}];
		[self setCallback:[=] (SKProductsResponse *response, NSError *err) {
			// After refresh we can try again
			NSURL *receiptURL = [[NSBundle mainBundle] appStoreReceiptURL];
			if (![[NSFileManager defaultManager] fileExistsAtPath:[receiptURL path]]) {
				return onError(ErrorCode::ErrorWithExternalStore, "Unable to fetch product license, the user is not logged in to the AppStore");
			}
			
			Log(@"AppStore receipt refreshed successfully");
			onSuccess([NSData dataWithContentsOfURL:receiptURL]);
		} forNextRequest:refreshReceiptRequest];
		
		refreshReceiptRequest.delegate = self;
		[refreshReceiptRequest start];
	}
}

- (NSString *)hashedValueForAccountName:(const char*)accountName {
	const int HASH_SIZE = 32;
	unsigned char hashedChars[HASH_SIZE];
	size_t accountNameLen = strlen(accountName);
 
	// Confirm that the length of the user name is small enough
	// to be recast when calling the hash function.
	if (accountNameLen > UINT32_MAX) {
		Log(@"Account name too long to hash: %s", accountName);
		return nil;
	}
	CC_SHA256(accountName, (CC_LONG)accountNameLen, hashedChars);
 
	// Convert the array of bytes into a string showing its hex representation.
	NSMutableString *userAccountHash = [[NSMutableString alloc] init];
	for (int i = 0; i < HASH_SIZE; i++) {
		// Add a dash every four bytes, for readability.
		if (i != 0 && i%4 == 0) {
			[userAccountHash appendString:@"-"];
		}
		[userAccountHash appendFormat:@"%02x", hashedChars[i]];
	}
	return userAccountHash;
}

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

- (void)markTransactionAsFinished:(SKPaymentTransaction *)tx {
	Log(@"Closing transaction %@ (product %@)", tx.transactionIdentifier, tx.payment.productIdentifier);
	[[SKPaymentQueue defaultQueue] finishTransaction:tx];
}

- (void)setCallback:(function<void (SKProductsResponse *, NSError *)>)callback forNextRequest:(SKProductsRequest *)request {
	productsResponseHandlers[request] = callback;
}

- (BOOL)shouldDeliverTransaction:(SKPaymentTransaction *)tx {
	switch (tx.transactionState) {
		// Call the appropriate custom method for the transaction state.
		case SKPaymentTransactionStatePurchasing:
		case SKPaymentTransactionStateDeferred:
			return NO;
		case SKPaymentTransactionStateFailed:
		case SKPaymentTransactionStatePurchased:
		case SKPaymentTransactionStateRestored:
			return YES;
		default:
			// For debugging
			Log(@"Unexpected AppStore transaction state %@", @(tx.transactionState).description);
			return NO;
	}
}

@end
