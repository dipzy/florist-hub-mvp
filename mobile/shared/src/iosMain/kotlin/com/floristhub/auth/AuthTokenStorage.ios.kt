package com.floristhub.auth

import platform.Foundation.NSUserDefaults

private class UserDefaultsAuthSessionStore : AuthSessionStore {
    private val defaults = NSUserDefaults.standardUserDefaults

    override fun getToken(): String? = defaults.stringForKey(KEY_TOKEN)?.takeIf { it.isNotEmpty() }

    override fun setToken(token: String?) {
        if (token == null) {
            defaults.removeObjectForKey(KEY_TOKEN)
        } else {
            defaults.setObject(token, forKey = KEY_TOKEN)
        }
    }

    override fun getTenantId(): String? = defaults.stringForKey(KEY_TENANT)?.takeIf { it.isNotEmpty() }

    override fun setTenantId(tenantId: String?) {
        if (tenantId == null) {
            defaults.removeObjectForKey(KEY_TENANT)
        } else {
            defaults.setObject(tenantId, forKey = KEY_TENANT)
        }
    }

    companion object {
        private const val KEY_TOKEN = "floristhub_token"
        private const val KEY_TENANT = "floristhub_tenant_id"
    }
}

actual fun createAuthSessionStore(): AuthSessionStore = UserDefaultsAuthSessionStore()
