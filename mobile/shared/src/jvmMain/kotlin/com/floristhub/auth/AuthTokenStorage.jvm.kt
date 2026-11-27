package com.floristhub.auth

private class InMemoryAuthSessionStore : AuthSessionStore {
    private var token: String? = null
    private var tenantId: String? = null

    override fun getToken(): String? = token

    override fun setToken(token: String?) {
        this.token = token
    }

    override fun getTenantId(): String? = tenantId

    override fun setTenantId(tenantId: String?) {
        this.tenantId = tenantId
    }
}

actual fun createAuthSessionStore(): AuthSessionStore = InMemoryAuthSessionStore()
