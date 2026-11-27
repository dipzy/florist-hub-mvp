package com.floristhub.config

import kotlin.test.Test
import kotlin.test.assertEquals

class AppTenantDefaultsTest {
    @Test
    fun normalizeTenantSlug_trims_and_lowercases() {
        assertEquals("sao-flores", normalizeTenantSlug("  Sao-Flores  "))
    }
}
