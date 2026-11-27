package com.floristhub

import androidx.compose.ui.window.ComposeUIViewController
import com.floristhub.app.FloristHubApp
import com.floristhub.push.IosPushBridge

@Suppress("FunctionName", "unused")
fun MainViewController() = ComposeUIViewController {
    FloristHubApp(
        apiBaseUrl = iosApiBaseUrl(),
        webBaseUrl = iosWebBaseUrl(),
        configuredTenantSlug = iosTenantSlug(),
        pushPlatform = "Ios",
        pushToken = { IosPushBridge.token },
    )
}
