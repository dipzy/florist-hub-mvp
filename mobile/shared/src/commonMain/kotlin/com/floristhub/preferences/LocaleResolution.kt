package com.floristhub.preferences

fun apiAcceptLanguage(locale: AppLocale): String = locale.apiTag

fun resolveDarkTheme(preference: AppThemePreference, systemDark: Boolean): Boolean = when (preference) {
    AppThemePreference.Light -> false
    AppThemePreference.Dark -> true
    AppThemePreference.System -> systemDark
}
