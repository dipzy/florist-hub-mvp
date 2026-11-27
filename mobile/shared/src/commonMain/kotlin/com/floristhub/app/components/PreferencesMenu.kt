package com.floristhub.app.components

import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.StrokeCap
import androidx.compose.ui.unit.dp
import com.floristhub.app.i18n.AppStrings
import com.floristhub.app.i18n.LocalUiPreferences
import com.floristhub.app.i18n.l10nString
import com.floristhub.preferences.AppLocale
import com.floristhub.preferences.AppThemePreference

@Composable
fun PreferencesMenu(modifier: Modifier = Modifier) {
    val ui = LocalUiPreferences.current ?: return
    var open by remember { mutableStateOf(false) }
    val iconTint = MaterialTheme.colorScheme.onPrimary

    Box(modifier) {
        IconButton(onClick = { open = true }) {
            SlidersIcon(iconTint)
        }
        DropdownMenu(expanded = open, onDismissRequest = { open = false }) {
            MenuHeading(l10nString(AppStrings.prefsThemeSection))
            ThemeChoice(l10nString(AppStrings.prefsThemeLight), ui.theme == AppThemePreference.Light) {
                ui.onThemeChange(AppThemePreference.Light)
            }
            ThemeChoice(l10nString(AppStrings.prefsThemeDark), ui.theme == AppThemePreference.Dark) {
                ui.onThemeChange(AppThemePreference.Dark)
            }
            ThemeChoice(l10nString(AppStrings.prefsThemeSystem), ui.theme == AppThemePreference.System) {
                ui.onThemeChange(AppThemePreference.System)
            }
            MenuHeading(l10nString(AppStrings.prefsLanguageSection))
            ThemeChoice(l10nString(AppStrings.prefsLanguagePt), ui.locale == AppLocale.Pt) {
                ui.onLocaleChange(AppLocale.Pt)
            }
            ThemeChoice(l10nString(AppStrings.prefsLanguageEn), ui.locale == AppLocale.En) {
                ui.onLocaleChange(AppLocale.En)
            }
        }
    }
}

@Composable
private fun MenuHeading(text: String) {
    Text(
        text,
        style = MaterialTheme.typography.labelSmall,
        color = MaterialTheme.colorScheme.onSurfaceVariant,
        modifier = Modifier.width(220.dp),
    )
}

@Composable
private fun ThemeChoice(label: String, selected: Boolean, onClick: () -> Unit) {
    DropdownMenuItem(
        text = {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(label, modifier = Modifier.weight(1f))
                if (selected) Text("✓")
            }
        },
        onClick = onClick,
    )
}

@Composable
private fun SlidersIcon(color: Color) {
    Canvas(Modifier.size(20.dp)) {
        val stroke = 1.8.dp.toPx()
        fun line(y: Float) {
            drawLine(color, Offset(2f, y), Offset(size.width - 2f, y), stroke, StrokeCap.Round)
        }
        line(size.height * 0.28f)
        line(size.height * 0.5f)
        line(size.height * 0.72f)
        drawCircle(color, 3.2.dp.toPx(), Offset(size.width * 0.35f, size.height * 0.28f))
        drawCircle(color, 3.2.dp.toPx(), Offset(size.width * 0.62f, size.height * 0.5f))
        drawCircle(color, 3.2.dp.toPx(), Offset(size.width * 0.45f, size.height * 0.72f))
    }
}
