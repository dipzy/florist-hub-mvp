package com.floristhub.app.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.selection.selectable
import androidx.compose.foundation.selection.selectableGroup
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.RadioButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.semantics.Role
import androidx.compose.ui.unit.dp
import com.floristhub.app.components.FloristScaffold
import com.floristhub.app.components.FloristScreenBox
import com.floristhub.app.i18n.AppStrings
import com.floristhub.app.i18n.LocalUiPreferences
import com.floristhub.app.i18n.l10nString
import com.floristhub.preferences.AppLocale
import com.floristhub.preferences.AppThemePreference

@Composable
fun AppPreferencesScreen(onBack: () -> Unit) {
    val ui = LocalUiPreferences.current
        ?: error("AppPreferencesScreen requires LocalUiPreferences")
    val scroll = rememberScrollState()

    FloristScaffold(title = l10nString(AppStrings.prefsTitle)) { padding ->
        FloristScreenBox(padding) {
            Column(
                modifier = Modifier
                    .verticalScroll(scroll)
                    .selectableGroup(),
                verticalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                Text(
                    l10nString(AppStrings.prefsThemeSection),
                    style = MaterialTheme.typography.titleMedium,
                )
                ThemeRow(
                    label = l10nString(AppStrings.prefsThemeLight),
                    selected = ui.theme == AppThemePreference.Light,
                    onSelect = { ui.onThemeChange(AppThemePreference.Light) },
                )
                ThemeRow(
                    label = l10nString(AppStrings.prefsThemeDark),
                    selected = ui.theme == AppThemePreference.Dark,
                    onSelect = { ui.onThemeChange(AppThemePreference.Dark) },
                )
                ThemeRow(
                    label = l10nString(AppStrings.prefsThemeSystem),
                    selected = ui.theme == AppThemePreference.System,
                    onSelect = { ui.onThemeChange(AppThemePreference.System) },
                )

                Text(
                    l10nString(AppStrings.prefsLanguageSection),
                    style = MaterialTheme.typography.titleMedium,
                    modifier = Modifier.padding(top = 16.dp),
                )
                ThemeRow(
                    label = l10nString(AppStrings.prefsLanguagePt),
                    selected = ui.locale == AppLocale.Pt,
                    onSelect = { ui.onLocaleChange(AppLocale.Pt) },
                )
                ThemeRow(
                    label = l10nString(AppStrings.prefsLanguageEn),
                    selected = ui.locale == AppLocale.En,
                    onSelect = { ui.onLocaleChange(AppLocale.En) },
                )

                OutlinedButton(
                    onClick = onBack,
                    modifier = Modifier.fillMaxWidth().padding(top = 24.dp),
                ) {
                    Text(l10nString(AppStrings.prefsBack))
                }
            }
        }
    }
}

@Composable
private fun ThemeRow(
    label: String,
    selected: Boolean,
    onSelect: () -> Unit,
) {
    androidx.compose.foundation.layout.Row(
        modifier = Modifier
            .fillMaxWidth()
            .selectable(
                selected = selected,
                onClick = onSelect,
                role = Role.RadioButton,
            )
            .padding(vertical = 4.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(8.dp),
    ) {
        RadioButton(selected = selected, onClick = null)
        Text(label)
    }
}
