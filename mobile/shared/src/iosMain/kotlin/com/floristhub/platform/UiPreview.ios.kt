package com.floristhub.platform

import platform.Foundation.NSBundle

actual fun isUiPreviewEnabled(): Boolean {
    val raw = NSBundle.mainBundle.objectForInfoDictionaryKey("FLORIST_HUB_UI_PREVIEW") as? String
    return raw.equals("YES", ignoreCase = true) || raw.equals("true", ignoreCase = true)
}
