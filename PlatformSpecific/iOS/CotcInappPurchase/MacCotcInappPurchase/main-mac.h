//
//  main-ios.h
//  CotcInappPurchase
//
//  Created by Florian on 19/08/15.
//  Copyright (c) 2015 Clan of the Cloud. All rights reserved.
//
//  Main class. Defines the interface between the C# code and this native plugin.

#import <Foundation/Foundation.h>
#include "config.h"

typedef void (STDCALL *Delegate)(const char* jsonResult);

/** Call this before everything else. The object name is used as target for subsequent callbacks. */
extern "C" void CotcInappPurchase_startup();
/** Expects an array of `CotcSdk.ConfiguredProduct` C# entities. **/
extern "C" void CotcInappPurchase_listProducts(const char *productsJson, Delegate onFinished);
/** Expects `CotcSdk.InappPurchase.ProductInfo` C# entity. **/
extern "C" void CotcInappPurchase_launchPurchase(const char *productJson, Delegate onFinished);
/** Expects `token` & `internalProductId`. **/
extern "C" void CotcInappPurchase_terminatePurchase(const char *paramsJson, Delegate onFinished);
