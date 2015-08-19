//
//  safe.h
//  CotcInappPurchase
//
//  Created by Florian on 19/08/15.
//  Copyright (c) 2015 Clan of the Cloud. All rights reserved.
//

#ifndef __CotcInappPurchase__safe__
#define __CotcInappPurchase__safe__

#include <stdlib.h>
#include <string.h>
#include <stdarg.h>

#define numberof(x) (sizeof(x)/sizeof(x[0]))
#define local_str(x) x, numberof(x)

namespace safe {
	extern void strcpy(char *dest, size_t max_size, const char *source);
	template <size_t S> void strcpy(char (&dest)[S], const char *source) { strcpy(dest, S, source); }
	
	extern void strcat(char *dest, size_t max_size, const char *source);
	template <size_t S> void strcat(char (&dest)[S], const char *source) { strcat(dest, S, source); }
	
	extern void sprintf(char *dest, size_t max_size, const char *format, ...);
	template <size_t S> void sprintf(char (&dest)[S], const char *format, ...) {
		va_list args;
		va_start (args, format);
		vsnprintf(dest, S, format, args);
		dest[S - 1] = '\0';
		va_end (args);
	}
	
	template <size_t S> size_t charsIn(char (&dest)[S]) { return S; }

	/**
	 * Constant string holder.
	 * - Automatically initializes to NULL
	 * - Duplicates the assigned string
	 * - Frees the assigned string upon destruction or assignment of a different one
	 * Miniature version of std::string. Equivalents (char* / cstring):
	 * {                                   {
	 *     char *str = NULL;
	 *     str = strdup("hello");              cstring str = "hello";
	 *     printf("%s world", str);            printf("%s world", str);
	 *     if (str)
	 *         delete str;                 }
	 * }
	 */
	struct cstring {
		cstring() : buffer(NULL) {}
		cstring(const cstring &other);
		cstring(cstring &&other);
		cstring(const char *other);
		cstring(char *other, bool takeOwnership = false);
		~cstring();
		
		cstring& operator = (const char *other);
		cstring& operator = (cstring &other);
		cstring& operator = (cstring &&other);
		operator const char *() const { return buffer; }
		/**
		 * Transfers ownership of the string to this cstring object. The 'other' object will be freed when this cstring dies and no copy is made.
		 * @param other string to use and be released at the end
		 */
		cstring& operator <<= (char *other);
		
		char *c_str() const { return buffer; }
		bool operator == (const cstring& other) const { return buffer && other.buffer && !strcmp(buffer, other.buffer); }
		bool operator < (const cstring& other) const { return buffer && other.buffer && strcmp(buffer, other.buffer) < 0; }
		bool operator > (const cstring& other) const { return buffer && other.buffer && strcmp(buffer, other.buffer) > 0; }
		bool operator <= (const cstring& other) const { return buffer && other.buffer && strcmp(buffer, other.buffer) <= 0; }
		bool operator >= (const cstring& other) const { return buffer && other.buffer && strcmp(buffer, other.buffer) >= 0; }
		bool IsEqual(const char *other) const;
		/**
		 * Takes out the string held by this cstring and returns it. You'll be free to delete it whenever needed.
		 * @return the detached string
		 */
		char *DetachOwnership();
		
	private:
		char *buffer;
	};
}

#endif /* defined(__CotcInappPurchase__safe__) */
