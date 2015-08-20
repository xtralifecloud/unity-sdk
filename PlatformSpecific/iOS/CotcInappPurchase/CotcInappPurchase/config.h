//
//  Config.h
//  CotcInappPurchase
//
//  Created by Florian on 20/08/15.
//  Copyright (c) 2015 Clan of the Cloud. All rights reserved.
//

#ifndef CotcInappPurchase_Config_h
#define CotcInappPurchase_Config_h

#if DEBUG
#	define Log(...)
#else
#	define Log(...)	NSLog(__VA_ARGS__);
#endif

#endif
