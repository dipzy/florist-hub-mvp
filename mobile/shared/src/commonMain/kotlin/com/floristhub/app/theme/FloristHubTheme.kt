package com.floristhub.app.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

@Composable
fun FloristHubTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    storePrimary: Color? = null,
    storeAccent: Color? = null,
    content: @Composable () -> Unit,
) {
    val base = if (darkTheme) FloristDarkColors else FloristLightColors
    var colors = if (storePrimary != null) {
        base.copy(
            primary = storePrimary,
            onPrimary = contrastOnColor(storePrimary),
        )
    } else {
        base
    }
    if (storeAccent != null) {
        val accent = accentOnSurface(storeAccent)
        colors = colors.copy(
            tertiary = accent,
            secondary = accent,
            onSecondary = contrastOnColor(accent, dark = Sage900),
            onTertiary = contrastOnColor(accent, dark = Sage900),
        )
    }
    MaterialTheme(
        colorScheme = colors,
        typography = FloristTypography,
        shapes = FloristShapes,
        content = content,
    )
}
