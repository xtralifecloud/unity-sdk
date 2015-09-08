#import <Foundation/Foundation.h>
#include "utils.h"

id jsonFromString(const char *jsonString) {
	NSError *e = nil;
	NSData *data = [NSData dataWithBytes:jsonString length:strlen(jsonString)];
	id json = [NSJSONSerialization JSONObjectWithData:data options:NSJSONReadingMutableContainers error:&e];
	if (!json) {
		Log(@"Error parsing JSON: %@", e);
	}
	return json;
}

cstring jsonToString(id json) {
	NSError *error = nil;
	NSData *data = [NSJSONSerialization dataWithJSONObject:json options:0 error:&error];
	if (error) {
		Log(@"Error when serializing JSON: %@", error.localizedDescription);
	}
	// Copy buffer to string
	char *str = (char*) malloc(data.length + 1);
	memcpy(str, data.bytes, data.length);
	str[data.length] = '\0';
	return cstring(str, true);
}

cstring makeErrorJson(ErrorCode code, const char *desc) {
	NSMutableDictionary *json = [NSMutableDictionary dictionary];
	[json setValue:[NSString stringWithUTF8String:desc] forKey:@"description"];
	[json setValue:[NSNumber numberWithInteger:code] forKey:@"error"];
	return jsonToString(json);
}

