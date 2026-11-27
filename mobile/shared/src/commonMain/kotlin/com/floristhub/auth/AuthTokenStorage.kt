package com.floristhub.auth

interface AuthSessionStore {
    fun getToken(): String?
    fun setToken(token: String?)
    fun getTenantId(): String?
    fun setTenantId(tenantId: String?)
}

expect fun createAuthSessionStore(): AuthSessionStore
