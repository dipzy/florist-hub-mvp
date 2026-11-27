package com.floristhub.app.i18n

import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import com.floristhub.preferences.AppLocale

@Composable
fun ProvideAppLocale(appLocale: AppLocale, content: @Composable () -> Unit) {
    CompositionLocalProvider(LocalAppLocale provides appLocale) {
        content()
    }
}
