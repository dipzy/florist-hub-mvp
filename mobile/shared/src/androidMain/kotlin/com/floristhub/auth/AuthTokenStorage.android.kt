package com.floristhub.auth

import android.content.Context
import android.content.SharedPreferences

object AuthTokenStorageHolder {
    var applicationContext: Context? = null
}

private class SharedPrefsAuthSessionStore(context: Context) : AuthSessionStore {
    private val prefs: SharedPreferences =
        context.applicationContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

    override fun getToken(): String? = prefs.getString(KEY_TOKEN, null)?.takeIf { it.isNotBlank() }

    override fun setToken(token: String?) {
        prefs.edit().putString(KEY_TOKEN, token).apply()
    }

    override fun getTenantId(): String? = prefs.getString(KEY_TENANT, null)?.takeIf { it.isNotBlank() }

    override fun setTenantId(tenantId: String?) {
        prefs.edit().putString(KEY_TENANT, tenantId).apply()
    }

    companion object {
        private const val PREFS_NAME = "floristhub_auth"
        private const val KEY_TOKEN = "token"
        private const val KEY_TENANT = "tenant_id"
    }
}

actual fun createAuthSessionStore(): AuthSessionStore {
    val context = AuthTokenStorageHolder.applicationContext
        ?: error("AuthTokenStorageHolder.applicationContext must be set in MainActivity")
    return SharedPrefsAuthSessionStore(context)
}
