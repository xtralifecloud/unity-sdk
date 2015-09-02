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

#if defined(WIN32) || defined(__WP8__)
#	define STDCALL __stdcall
#else	// WIN32
#	ifndef STDCALL
#		define STDCALL
#	endif
#endif

#endif
