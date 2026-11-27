package com.floristhub.app.configurator

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.viewinterop.UIKitView
import kotlinx.cinterop.ExperimentalForeignApi
import platform.CoreGraphics.CGRectMake
import platform.Foundation.NSURL
import platform.Foundation.NSURLRequest
import platform.WebKit.WKWebView
import platform.WebKit.WKWebViewConfiguration

@OptIn(ExperimentalForeignApi::class)
@Composable
actual fun BouquetWebView(url: String, modifier: Modifier) {
    UIKitView(
        modifier = modifier,
        factory = {
            WKWebView(
                frame = CGRectMake(0.0, 0.0, 0.0, 0.0),
                configuration = WKWebViewConfiguration(),
            )
        },
        update = { webView ->
            val current = webView.URL?.absoluteString
            if (current == url) return@UIKitView
            val target = NSURL.URLWithString(url) ?: return@UIKitView
            webView.loadRequest(NSURLRequest.requestWithURL(target))
        },
    )
}
