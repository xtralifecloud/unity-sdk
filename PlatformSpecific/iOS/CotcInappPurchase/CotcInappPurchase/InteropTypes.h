//
//  InteropTypes.h
//  CotcInappPurchase
//
//  Created by Florian on 19/08/15.
//  Copyright (c) 2015 Clan of the Cloud. All rights reserved.
//
//  Defines types that have an equivalent in C#.

#define STORE_TYPE @"appstore"

/** Converts a vector of entities to a JSON-useable NSArray. */
template<class T>
NSArray *makeJsonArray(const vector<T> list) {
	NSMutableArray *result = [NSMutableArray array];
	for (const T& item: list) {
		[result addObject:item.toJson()];
	}
	return result;
}

struct ConfiguredProduct {
	cstring appStoreId;
	cstring googlePlayId;
	cstring productId;
	
	ConfiguredProduct(NSDictionary *sourceJson) {
		appStoreId = [[sourceJson objectForKey:@"appStoreId"] UTF8String];
		googlePlayId = [[sourceJson objectForKey:@"googlePlayId"] UTF8String];
		productId = [[sourceJson objectForKey:@"productId"] UTF8String];
	}

	NSDictionary *toJson() const {
		NSMutableDictionary *result = [NSMutableDictionary dictionary];
		[result setObject:[NSString stringWithUTF8String:appStoreId] forKey:@"appStoreId"];
		[result setObject:[NSString stringWithUTF8String:googlePlayId] forKey:@"googlePlayId"];
		[result setObject:[NSString stringWithUTF8String:productId] forKey:@"productId"];
		return result;
	}
};

enum ErrorCode {
	/// No error.
	Ok = 0,
	
	NetworkError = 2000,
	ServerError = 2001,
	NotImplemented = 2002,
	LogicError = 2003,
	InternalError = 2004,
	Canceled = 2005,
	AlreadyInProgress = 2006,
	
	NotSetup = 2100,
	BadAppCredentials = 2101,
	NotLoggedIn = 2102,
	BadParameters = 2104,
	EventListenerAlreadyRegistered = 2105,
	AlreadySetup = 2106,
	SocialNetworkError = 2107,
	LoginCanceled = 2108,
	ErrorWithExternalStore = 2109,
	
	/// You shouldn't receive this error, it's just a convenient value
	LastError
};

struct ProductInfo {
	cstring productId;
	float price;
	cstring currency;
	cstring internalProductId;
	
	ProductInfo() : price(0.0f) {}
	
	ProductInfo(NSDictionary *sourceJson) {
		productId = [[sourceJson objectForKey:@"productId"] UTF8String];
		price = [[sourceJson objectForKey:@"price"] floatValue];
		currency = [[sourceJson objectForKey:@"currency"] UTF8String];
		internalProductId = [[sourceJson objectForKey:@"internalProductId"] UTF8String];
	}
	
	NSDictionary *toJson() const {
		NSMutableDictionary *result = [NSMutableDictionary dictionary];
		[result setObject:[NSString stringWithUTF8String:productId] forKey:@"productId"];
		[result setObject:[NSDecimalNumber numberWithFloat:price] forKey:@"price"];
		[result setObject:[NSString stringWithUTF8String:currency] forKey:@"currency"];
		[result setObject:[NSString stringWithUTF8String:internalProductId] forKey:@"internalProductId"];
		return result;
	}
};

struct PurchasedProduct {
	cstring storeType;
	cstring cotcProductId;
	cstring internalProductId;
	float paidPrice;
	cstring paidCurrency;
	cstring receipt;
	cstring token;
	
	PurchasedProduct() : paidPrice(0.0f) {}
	
	PurchasedProduct(NSDictionary *sourceJson) {
		storeType = [[sourceJson objectForKey:@"store"] UTF8String];
		cotcProductId = [[sourceJson objectForKey:@"productId"] UTF8String];
		internalProductId = [[sourceJson objectForKey:@"internalProductId"] UTF8String];
		paidPrice = [[sourceJson objectForKey:@"price"] floatValue];
		paidCurrency = [[sourceJson objectForKey:@"currency"] UTF8String];
		receipt = [[sourceJson objectForKey:@"receipt"] UTF8String];
		token = [[sourceJson objectForKey:@"token"] UTF8String];
	}
	
	NSDictionary *toJson() const {
		NSMutableDictionary *result = [NSMutableDictionary dictionary];
		[result setObject:[NSString stringWithUTF8String:storeType] forKey:@"store"];
		[result setObject:[NSString stringWithUTF8String:cotcProductId] forKey:@"productId"];
		[result setObject:[NSString stringWithUTF8String:internalProductId] forKey:@"internalProductId"];
		[result setObject:[NSDecimalNumber numberWithFloat:paidPrice] forKey:@"price"];
		[result setObject:[NSString stringWithUTF8String:paidCurrency] forKey:@"currency"];
		[result setObject:[NSString stringWithUTF8String:receipt] forKey:@"receipt"];
		[result setObject:[NSString stringWithUTF8String:token] forKey:@"token"];
		return result;
	}
};
