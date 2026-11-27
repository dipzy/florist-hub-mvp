package com.floristhub.app.theme

import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.luminance
import kotlin.math.max
import kotlin.math.min
fun contrastOnColor(background: Color, light: Color = Color.White, dark: Color = Ink): Color {
    return if (background.luminance() < 0.4f) light else dark
}

fun accentOnSurface(accent: Color, surface: Color = Cream50): Color {
    val ratio = contrastRatio(accent.luminance(), surface.luminance())
    return if (ratio >= 4.5) accent else Color(0xFF6B4549)
}

private fun contrastRatio(l1: Float, l2: Float): Double {
    val lighter = max(l1, l2).toDouble()
    val darker = min(l1, l2).toDouble()
    return (lighter + 0.05) / (darker + 0.05)
}
