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

/** Call this before everything else. The object name is used as target for subsequent callbacks. */
extern "C" void CotcInappPurchase_startup(const char *callbackGameObjectName);
/** Expects an array of `CotcSdk.ConfiguredProduct` C# entities. Sends back GetInformationAboutProducts_Done message. **/
extern "C" void CotcInappPurchase_listProducts(const char *productsJson);
/** Expects `CotcSdk.InappPurchase.ProductInfo` C# entity. Sends back LaunchPurchase_Done message. **/
extern "C" void CotcInappPurchase_launchPurchase(const char *productJson);
/** Expects `token` & `internalProductId`. Sends back TerminatePurchase_Done message. **/
extern "C" void CotcInappPurchase_terminatePurchase(const char *paramsJson);
