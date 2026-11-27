package com.floristhub.platform

import android.content.Intent
import android.net.Uri
import androidx.browser.customtabs.CustomTabsIntent
import com.floristhub.auth.AuthTokenStorageHolder

actual fun openSystemCheckout(url: String) {
    val context = AuthTokenStorageHolder.applicationContext ?: return
    val tabs = CustomTabsIntent.Builder().setShowTitle(true).build()
    tabs.intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
    tabs.launchUrl(context, Uri.parse(url))
}
