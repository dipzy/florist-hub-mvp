package com.floristhub.preferences

import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertFalse
import kotlin.test.assertTrue

class LocaleResolutionTest {
    @Test
    fun apiAcceptLanguage_usesPtPtForPortuguese() {
        assertEquals("pt-PT", apiAcceptLanguage(AppLocale.Pt))
    }

    @Test
    fun apiAcceptLanguage_usesEnForEnglish() {
        assertEquals("en", apiAcceptLanguage(AppLocale.En))
    }

    @Test
    fun fromStored_parsesEnglishVariants() {
        assertEquals(AppLocale.En, AppLocale.fromStored("en"))
        assertEquals(AppLocale.En, AppLocale.fromStored("en-GB"))
    }

    @Test
    fun fromStored_defaultsToPortuguese() {
        assertEquals(AppLocale.Pt, AppLocale.fromStored(null))
        assertEquals(AppLocale.Pt, AppLocale.fromStored("pt-PT"))
    }

    @Test
    fun resolveDarkTheme_respectsPreference() {
        assertFalse(resolveDarkTheme(AppThemePreference.Light, systemDark = true))
        assertTrue(resolveDarkTheme(AppThemePreference.Dark, systemDark = false))
        assertTrue(resolveDarkTheme(AppThemePreference.System, systemDark = true))
        assertFalse(resolveDarkTheme(AppThemePreference.System, systemDark = false))
    }
}
