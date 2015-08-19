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
#include "ErrorCode.h"
#include "safe.h"

using std::map;
using std::function;
using safe::cstring;

#define STORE_PRODUCT_ID @"appStoreId"

@interface InappPurchaseWrapper : NSObject <SKProductsRequestDelegate, SKRequestDelegate, SKPaymentTransactionObserver> {
@private
	// Contains ProductRequestCallbackPair
	map<SKRequest*, function<void(SKProductsResponse*, NSError*)>> productsResponseHandlers;
}

- (id)init;
- (void)listProducts:(NSArray*)productIdentifiers
			callback:(function<void(NSDictionary *outJson)>)callback;

// MARK: Private
- (NSMutableDictionary*)makeErrorJson:(ErrorCode)code desc:(const char*)description;
- (void)setCallback:(function<void(SKProductsResponse *response, NSError *error)>)callback
	 forNextRequest:(SKProductsRequest*)request;

@end
