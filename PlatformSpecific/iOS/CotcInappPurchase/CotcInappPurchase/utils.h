//
//  utils.h
//  CotcInappPurchase
//
//  Created by Florian on 31/08/15.
//  Copyright (c) 2015 Clan of the Cloud. All rights reserved.
//

#ifndef __CotcInappPurchase__utils__
#define __CotcInappPurchase__utils__

#include "InappPurchaseWrapper.h"

/** Returns a non-nil value if the JSON data was decoded successfully (either NSDictionary or NSArray). */
id jsonFromString(const char *jsonString);
/** Takes a NSDictionary or NSArray. */
cstring jsonToString(id json);
cstring makeErrorJson(ErrorCode code, const char *desc);

#endif /* defined(__CotcInappPurchase__utils__) */
