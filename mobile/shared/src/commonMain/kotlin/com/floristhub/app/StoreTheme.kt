package com.floristhub.app

import androidx.compose.ui.graphics.Color

fun parseThemeColor(hex: String, fallback: Color = Color(0xFF2D6A4F)): Color {
    val normalized = hex.trim().removePrefix("#")
    if (normalized.length != 6) return fallback
    return try {
        Color(0xFF000000 or normalized.toLong(16))
    } catch (_: Exception) {
        fallback
    }
}
