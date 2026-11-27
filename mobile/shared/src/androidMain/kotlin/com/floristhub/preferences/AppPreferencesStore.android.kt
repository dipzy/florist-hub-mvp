package com.floristhub.preferences

import android.content.Context
import com.floristhub.auth.AuthTokenStorageHolder

private class SharedPrefsAppPreferencesStore(context: Context) : AppPreferencesStore {
    private val prefs =
        context.applicationContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

    override fun getThemePreference(): AppThemePreference =
        AppThemePreference.fromStored(prefs.getString(KEY_THEME, null))

    override fun setThemePreference(preference: AppThemePreference) {
        prefs.edit().putString(KEY_THEME, AppThemePreference.toStored(preference)).apply()
    }

    override fun getAppLocale(): AppLocale =
        AppLocale.fromStored(prefs.getString(KEY_LOCALE, null))

    override fun setAppLocale(locale: AppLocale) {
        prefs.edit().putString(KEY_LOCALE, AppLocale.toStored(locale)).apply()
    }

    companion object {
        private const val PREFS_NAME = "floristhub_app_prefs"
        private const val KEY_THEME = "ui_theme"
        private const val KEY_LOCALE = "app_locale"
    }
}

actual fun createAppPreferencesStore(): AppPreferencesStore {
    val context = AuthTokenStorageHolder.applicationContext
        ?: error("AuthTokenStorageHolder.applicationContext must be set in MainActivity")
    return SharedPrefsAppPreferencesStore(context)
}
