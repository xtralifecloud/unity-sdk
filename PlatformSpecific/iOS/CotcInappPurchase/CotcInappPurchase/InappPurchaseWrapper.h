//
//  InappPurchaseWrapper.h
//  CotcInappPurchase
//
//  Created by Florian on 19/08/15.
//  Copyright (c) 2015 Clan of the Cloud. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <StoreKit/StoreKit.h>
#include <functional>
#include <map>
#include <vector>
#include "config.h"
#include "safe.h"

using std::function;
using std::map;
using std::vector;
using safe::cstring;

#import "InteropTypes.h"

@interface InappPurchaseWrapper : NSObject <SKProductsRequestDelegate, SKRequestDelegate, SKPaymentTransactionObserver> {
@private
	map<SKRequest*, function<void(SKProductsResponse*, NSError*)>> productsResponseHandlers;
	map<cstring, function<void(SKPaymentTransaction*)>> productPurchasedHandlers;
	// Transactions that have been notified but not yet acknowledged (probably because there hasn't
	// been a productPurchasedHandlers registered for the corresponding product).
	map<cstring, SKPaymentTransaction*> pendingTransactions;
}

- (id)init;

- (void)listProducts:(const vector<ConfiguredProduct> &)products
			 success:(function<void(const vector<ProductInfo> &)>)onSuccess
			   error:(function<void(ErrorCode code, const char *desc)>)onError;

- (void)launchPurchaseFlow:(const ProductInfo &)product
				   forUser:(const cstring &)userName
				   success:(function<void(const PurchasedProduct &)>)onSuccess
			   error:(function<void(ErrorCode code, const char *desc)>)onError;

- (void)terminatePurchase:(const cstring &)transactionId
				  success:(function<void()>)onSuccess
					error:(function<void(ErrorCode code, const char *desc)>)onError;

// MARK: Private
- (void)deliverTransaction:(SKPaymentTransaction*)tx;
- (void)fetchReceipt:(function<void(NSData*)>)onSuccess
			   error:(function<void(ErrorCode code, const char *desc)>)onError;
- (NSString *)hashedValueForAccountName:(const char*)accountName;
- (void)markTransactionAsFinished:(SKPaymentTransaction*)tx;
- (void)setCallback:(function<void(SKProductsResponse *response, NSError *error)>)callback
	 forNextRequest:(SKRequest*)request;
// Transactions that are in a finished state only will be processed (pending and so on are discarded)
- (BOOL)shouldDeliverTransaction:(SKPaymentTransaction*)tx;

@end
