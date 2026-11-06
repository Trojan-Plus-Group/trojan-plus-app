// SPDX-License-Identifier: GPL-3.0-or-later
#include <stddef.h>
#include <stdint.h>

typedef void (*log_callback)(const unsigned char *, size_t, int, void *);
static log_callback callback;
static void *callback_context;
static int register_calls, unregister_calls, register_result, unregister_result;

int trojan_set_log_callback(log_callback next, void *context)
{
    if (next) {
        ++register_calls;
        if (register_result) return register_result;
    } else {
        ++unregister_calls;
        if (unregister_result) return unregister_result;
    }
    callback = next;
    callback_context = context;
    return 0;
}

void shim_reset(void)
{
    callback = NULL;
    callback_context = NULL;
    register_calls = unregister_calls = register_result = unregister_result = 0;
}

void shim_results(int registration, int unregistration)
{
    register_result = registration;
    unregister_result = unregistration;
}

int shim_registered(void) { return callback != NULL; }
int shim_register_calls(void) { return register_calls; }
int shim_unregister_calls(void) { return unregister_calls; }

void shim_emit(const unsigned char *bytes, size_t length)
{
    if (callback) callback(bytes, length, 4, callback_context);
}

// Must be rejected before dereferencing: offset exceeds INTPTR_MAX.
void shim_emit_impossible_length(void)
{
    if (callback) callback(NULL, SIZE_MAX, 4, callback_context);
}
