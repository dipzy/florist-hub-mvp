package com.floristhub.preferences

import platform.Foundation.NSUserDefaults

private class UserDefaultsAppPreferencesStore : AppPreferencesStore {
    private val defaults = NSUserDefaults.standardUserDefaults

    override fun getThemePreference(): AppThemePreference =
        AppThemePreference.fromStored(defaults.stringForKey(KEY_THEME))

    override fun setThemePreference(preference: AppThemePreference) {
        defaults.setObject(AppThemePreference.toStored(preference), forKey = KEY_THEME)
    }

    override fun getAppLocale(): AppLocale =
        AppLocale.fromStored(defaults.stringForKey(KEY_LOCALE))

    override fun setAppLocale(locale: AppLocale) {
        defaults.setObject(AppLocale.toStored(locale), forKey = KEY_LOCALE)
    }

    companion object {
        private const val KEY_THEME = "floristhub_ui_theme"
        private const val KEY_LOCALE = "floristhub_app_locale"
    }
}

actual fun createAppPreferencesStore(): AppPreferencesStore = UserDefaultsAppPreferencesStore()
