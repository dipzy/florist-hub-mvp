package com.floristhub.push

fun orderIdFromFloristHubUri(uri: String?): String? {
    if (uri.isNullOrBlank()) return null
    val raw = uri.trim()
    val marker = "://encomenda/"
    val scheme = raw.indexOf(marker)
    if (scheme <= 0 || !raw.startsWith("floristhub", ignoreCase = true)) return null
    val id = raw.substring(scheme + marker.length).substringBefore('?').substringBefore('/').trim()
    if (id.length != 36) return null
    return id
}
