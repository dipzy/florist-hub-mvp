package com.floristhub.app.configurator

fun themeQueryColor(raw: String?): String? {
    val value = raw?.trim()?.takeIf { it.isNotEmpty() } ?: return null
    val withHash = if (value.startsWith("#")) value else "#$value"
    return if (QUERY_COLOR.matches(withHash)) withHash else null
}

fun configuratorEmbedUrl(
    webBaseUrl: String,
    tenantSlug: String,
    primary: String? = null,
    accent: String? = null,
): String {
    val base = webBaseUrl.trim().trimEnd('/')
    val query = buildList {
        add("tenant=${encodeQuery(tenantSlug)}")
        themeQueryColor(primary)?.let { add("primary=${encodeQuery(it)}") }
        themeQueryColor(accent)?.let { add("accent=${encodeQuery(it)}") }
    }.joinToString("&")
    return "$base/embed/configurador?$query"
}

private val QUERY_COLOR = Regex("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")

private fun encodeQuery(value: String): String = buildString {
    for (byte in value.encodeToByteArray()) {
        val code = byte.toInt() and 0xff
        val ch = code.toChar()
        if (ch.isLetterOrDigit() || ch in "-._~") append(ch) else append("%${code.toString(16).uppercase().padStart(2, '0')}")
    }
}
