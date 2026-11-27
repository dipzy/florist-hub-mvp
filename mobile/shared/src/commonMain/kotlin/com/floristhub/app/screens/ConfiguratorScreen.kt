package com.floristhub.app.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.floristhub.app.components.FloristScaffold
import com.floristhub.app.components.FloristScreenBox
import com.floristhub.app.configurator.BouquetWebView
import com.floristhub.app.configurator.configuratorEmbedUrl
import com.floristhub.app.i18n.AppStrings
import com.floristhub.app.i18n.l10nString
import com.floristhub.network.ApiClient

@Composable
fun ConfiguratorScreen(
    api: ApiClient,
    webBaseUrl: String,
    preview: Boolean,
    onBack: () -> Unit,
) {
    var url by remember { mutableStateOf<String?>(null) }
    var error by remember { mutableStateOf<String?>(null) }
    val configuratorError = l10nString(AppStrings.configuratorLoadError)

    LaunchedEffect(api.tenantId, webBaseUrl, preview) {
        if (preview) return@LaunchedEffect
        error = null
        val theme = runCatching { api.storeTheme() }.getOrNull()
        val slug = theme?.slug?.takeIf { it.isNotBlank() } ?: api.tenantSlug?.takeIf { it.isNotBlank() }
        if (slug.isNullOrBlank()) {
            error = configuratorError
            url = null
            return@LaunchedEffect
        }
        url = configuratorEmbedUrl(
            webBaseUrl = webBaseUrl,
            tenantSlug = slug,
            primary = theme?.themePrimaryColor,
            accent = theme?.themeAccentColor,
        )
    }

    FloristScaffold(title = l10nString(AppStrings.homeConfigurator)) { padding ->
        FloristScreenBox(padding) {
            Column(
                modifier = Modifier.fillMaxSize(),
                verticalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                TextButton(onClick = onBack) { Text(l10nString(AppStrings.prefsBack)) }
                when {
                    preview -> {
                        Text(
                            l10nString(AppStrings.configuratorPreviewHint),
                            style = MaterialTheme.typography.bodyMedium,
                        )
                    }
                    error != null -> Text(error!!, color = MaterialTheme.colorScheme.error)
                    url == null -> CircularProgressIndicator()
                    else -> BouquetWebView(url = url!!, modifier = Modifier.fillMaxWidth().weight(1f))
                }
            }
        }
    }
}
