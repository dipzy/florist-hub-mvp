package com.floristhub.app.i18n

import androidx.compose.runtime.staticCompositionLocalOf
import com.floristhub.preferences.AppLocale
import com.floristhub.preferences.AppThemePreference

data class UiPreferencesController(
    val theme: AppThemePreference,
    val locale: AppLocale,
    val onThemeChange: (AppThemePreference) -> Unit,
    val onLocaleChange: (AppLocale) -> Unit,
)

val LocalUiPreferences = staticCompositionLocalOf<UiPreferencesController?> { null }
