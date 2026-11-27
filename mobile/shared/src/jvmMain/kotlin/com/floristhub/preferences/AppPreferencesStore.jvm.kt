package com.floristhub.preferences

private class InMemoryAppPreferencesStore : AppPreferencesStore {
    private var theme: AppThemePreference = AppThemePreference.System
    private var locale: AppLocale = AppLocale.Pt

    override fun getThemePreference(): AppThemePreference = theme

    override fun setThemePreference(preference: AppThemePreference) {
        theme = preference
    }

    override fun getAppLocale(): AppLocale = locale

    override fun setAppLocale(locale: AppLocale) {
        this.locale = locale
    }
}

actual fun createAppPreferencesStore(): AppPreferencesStore = InMemoryAppPreferencesStore()
