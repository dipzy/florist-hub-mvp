package com.floristhub.app.configurator

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier

@Composable
expect fun BouquetWebView(url: String, modifier: Modifier = Modifier)
