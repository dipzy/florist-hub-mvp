package com.floristhub.app.configurator

import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNull

class ConfiguratorUrlTest {
    @Test
    fun incluiTenantECoresDaLoja() {
        val url = configuratorEmbedUrl(
            webBaseUrl = "http://10.0.2.2:5173/",
            tenantSlug = "flor-do-dia",
            primary = "2d6a4f",
            accent = "#d4a373",
        )
        assertEquals(
            "http://10.0.2.2:5173/embed/configurador?tenant=flor-do-dia&primary=%232d6a4f&accent=%23d4a373",
            url,
        )
    }

    @Test
    fun ignoraCorInvalida() {
        val url = configuratorEmbedUrl(
            webBaseUrl = "https://loja.example",
            tenantSlug = "a b",
            primary = "verde",
            accent = null,
        )
        assertEquals("https://loja.example/embed/configurador?tenant=a%20b", url)
        assertNull(themeQueryColor("nao-e-cor"))
    }
}
