#include "safe.h"
#include <stdio.h>

safe::cstring::cstring(const char *other) : buffer(NULL) {
	*this = other;
}

safe::cstring::cstring(char *other, bool takeOwnership) : buffer(NULL) {
	if (takeOwnership) { *this <<= other; }
	else { *this = other; }
}

safe::cstring::cstring(const cstring &other) : buffer(NULL) {
	*this = other;
}

safe::cstring::cstring(cstring &&other) {
	buffer = other.buffer;
	other.buffer = NULL;
}

safe::cstring::cstring(NSString *other) : buffer(NULL) {
	*this = other.UTF8String;
}

safe::cstring& safe::cstring::operator=(const char *other) {
	if (buffer) { free(buffer); buffer = NULL; }
	if (other) { buffer = strdup(other); }
	return *this;
}

safe::cstring& safe::cstring::operator=(cstring &other) {
	*this = other.buffer; return *this;
}

safe::cstring& safe::cstring::operator=(cstring &&other) {
	*this = NULL; buffer = other.buffer; other.buffer = NULL; return *this;
}

safe::cstring::~cstring() {
	if (buffer) { free(buffer); buffer = NULL; }
}

safe::cstring& safe::cstring::operator<<=(char *other) {
	*this = NULL; buffer = other;
	return *this;
}

bool safe::cstring::IsEqual(const char *other) const {
	return other != NULL && strcmp(buffer, other) == 0;
}

char *safe::cstring::DetachOwnership() {
	char *result = buffer;
	buffer = NULL;
	return result;
}

void safe::strcpy(char *dest, size_t max_size, const char *source) {
	while (*source && max_size-- > 1) {
		*dest++ = *source++;
	}
	*dest = '\0';
}

void safe::strcat(char *dest, size_t max_size, const char *source) {
	while (*dest && max_size > 1) {
		dest++, max_size--;
	}
	while (*source && max_size-- > 1) {
		*dest++ = *source++;
	}
	*dest = '\0';
}

void safe::sprintf(char *dest, size_t max_size, const char *format, ...) {
	va_list args;
	va_start (args, format);
	vsnprintf(dest, max_size, format, args);
	dest[max_size - 1] = '\0';
	va_end (args);
}
