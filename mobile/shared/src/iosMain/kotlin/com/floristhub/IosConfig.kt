package com.floristhub

import com.floristhub.config.DEFAULT_APP_TENANT_SLUG
import platform.Foundation.NSBundle

fun iosApiBaseUrl(): String = plistUrl("FLORIST_HUB_API_BASE_URL") ?: "http://127.0.0.1:5183"

fun iosWebBaseUrl(): String = plistUrl("FLORIST_HUB_WEB_BASE_URL") ?: "http://127.0.0.1:5173"

fun iosTenantSlug(): String = plistUrl("FLORIST_HUB_TENANT_SLUG") ?: DEFAULT_APP_TENANT_SLUG

private fun plistUrl(key: String): String? {
    val fromPlist = NSBundle.mainBundle.objectForInfoDictionaryKey(key) as? String
    return fromPlist?.trim()?.trimEnd('/')?.takeIf { it.isNotEmpty() }
}
