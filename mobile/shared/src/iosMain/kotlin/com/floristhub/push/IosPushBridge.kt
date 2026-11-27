package com.floristhub.push

object IosPushBridge {
    var token: String? = null
}

fun submitIosPushToken(token: String) {
    val value = token.trim()
    IosPushBridge.token = value.ifEmpty { null }
}
