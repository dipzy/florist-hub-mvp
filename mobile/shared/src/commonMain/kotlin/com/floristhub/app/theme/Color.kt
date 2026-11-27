package com.floristhub.app.theme

import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.ui.graphics.Color

val Sage900 = Color(0xFF1E3D32)
val Sage800 = Color(0xFF2A5244)
val Sage700 = Color(0xFF356B58)
val Sage600 = Color(0xFF4A8770)
val Sage100 = Color(0xFFE8F0EC)
val Sage50 = Color(0xFFF4F8F6)

val Rose500 = Color(0xFFD4A0A8)
val Rose100 = Color(0xFFF9ECEF)
val Cream50 = Color(0xFFFFFCF7)
val Ink = Color(0xFF1C2420)
val InkMuted = Color(0xFF5C6B64)

val FloristLightColors = lightColorScheme(
    primary = Sage700,
    onPrimary = Color.White,
    primaryContainer = Sage100,
    onPrimaryContainer = Sage900,
    secondary = Rose500,
    onSecondary = Sage900,
    secondaryContainer = Rose100,
    onSecondaryContainer = Sage800,
    background = Cream50,
    onBackground = Ink,
    surface = Color.White,
    onSurface = Ink,
    surfaceVariant = Sage50,
    onSurfaceVariant = InkMuted,
    outline = Color(0xFFC5D0C9),
    error = Color(0xFFB42318),
)

val FloristDarkColors = darkColorScheme(
    primary = Color(0xFF8FBFAD),
    onPrimary = Sage900,
    primaryContainer = Color(0xFF243029),
    onPrimaryContainer = Color(0xFFE8F0EC),
    secondary = Color(0xFFE8B4BC),
    onSecondary = Sage900,
    secondaryContainer = Color(0xFF2E2225),
    onSecondaryContainer = Color(0xFFF9ECEF),
    background = Color(0xFF141614),
    onBackground = Color(0xFFEEF2EF),
    surface = Color(0xFF1E2421),
    onSurface = Color(0xFFEEF2EF),
    surfaceVariant = Color(0xFF252B28),
    onSurfaceVariant = Color(0xFFA8B5AE),
    outline = Color(0xFF3D4842),
    error = Color(0xFFFDA4A4),
)
