package com.floristhub.app.configurator

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier

@Composable
actual fun BouquetWebView(url: String, modifier: Modifier) {
    Text(
        "O configurador 3D abre na WebView do Android e na WKWebView do iOS.\n$url",
        modifier = modifier,
        style = MaterialTheme.typography.bodyMedium,
    )
}
