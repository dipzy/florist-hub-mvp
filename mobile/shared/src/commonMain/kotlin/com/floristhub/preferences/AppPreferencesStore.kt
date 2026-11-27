package com.floristhub.preferences

interface AppPreferencesStore {
    fun getThemePreference(): AppThemePreference
    fun setThemePreference(preference: AppThemePreference)
    fun getAppLocale(): AppLocale
    fun setAppLocale(locale: AppLocale)
}

expect fun createAppPreferencesStore(): AppPreferencesStore
