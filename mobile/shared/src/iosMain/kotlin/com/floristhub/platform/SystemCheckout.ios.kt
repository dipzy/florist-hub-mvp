package com.floristhub.platform

import platform.Foundation.NSURL
import platform.SafariServices.SFSafariViewController
import platform.UIKit.UIApplication

actual fun openSystemCheckout(url: String) {
    val nsUrl = NSURL.URLWithString(url) ?: return
    val safari = SFSafariViewController(uRL = nsUrl)
    val root = UIApplication.sharedApplication.keyWindow?.rootViewController ?: return
    root.presentViewController(safari, animated = true, completion = null)
}
