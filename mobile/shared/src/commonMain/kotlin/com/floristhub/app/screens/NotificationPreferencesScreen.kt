package com.floristhub.app.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.floristhub.app.components.FloristScaffold
import com.floristhub.app.components.FloristScreenBox
import com.floristhub.app.i18n.AppStrings
import com.floristhub.app.i18n.l10nString
import com.floristhub.network.ApiClient
import com.floristhub.network.dto.NotificationPreferencesDto
import kotlinx.coroutines.launch

@Composable
fun NotificationPreferencesScreen(api: ApiClient, onBack: () -> Unit) {
    var prefs by remember { mutableStateOf(NotificationPreferencesDto()) }
    var error by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    val florist = api.role == "Florista"
    val customer = api.role == "Cliente"
    val showFlorist = florist || api.role.isNullOrBlank()
    val showCustomer = customer || api.role.isNullOrBlank()
    val saveError = l10nString(AppStrings.notificationsSaveError)
    val loadError = l10nString(AppStrings.notificationsLoadError)

    fun save(next: NotificationPreferencesDto) {
        prefs = next
        scope.launch {
            runCatching { api.updateNotificationPreferences(next) }
                .onSuccess { prefs = it }
                .onFailure { error = it.message ?: saveError }
        }
    }

    LaunchedEffect(Unit) {
        runCatching { api.notificationPreferences() }
            .onSuccess { prefs = it }
            .onFailure { error = it.message ?: loadError }
    }

    FloristScaffold(title = l10nString(AppStrings.homeNotifications)) { padding ->
        FloristScreenBox(padding) {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                TextButton(onClick = onBack) { Text(l10nString(AppStrings.prefsBack)) }
                Text(
                    l10nString(AppStrings.notificationsIntro),
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
                if (showFlorist) {
                    PreferenceRow(l10nString(AppStrings.notificationsNewOrder), prefs.newOrder) { save(prefs.copy(newOrder = it)) }
                    PreferenceRow(l10nString(AppStrings.notificationsOrderPaid), prefs.orderPaid) { save(prefs.copy(orderPaid = it)) }
                    PreferenceRow(l10nString(AppStrings.notificationsLowStock), prefs.lowStock) { save(prefs.copy(lowStock = it)) }
                    PreferenceRow(l10nString(AppStrings.notificationsRecurring), prefs.recurringSubscriptions) {
                        save(prefs.copy(recurringSubscriptions = it))
                    }
                }
                if (showCustomer) {
                    PreferenceRow(l10nString(AppStrings.notificationsOrderStatus), prefs.orderStatus) { save(prefs.copy(orderStatus = it)) }
                    PreferenceRow(l10nString(AppStrings.notificationsRecurring), prefs.recurringSubscriptions) {
                        save(prefs.copy(recurringSubscriptions = it))
                    }
                }
            }
        }
    }
}

@Composable
private fun PreferenceRow(label: String, checked: Boolean, onChecked: (Boolean) -> Unit) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Text(label, style = MaterialTheme.typography.bodyLarge)
        Switch(checked = checked, onCheckedChange = onChecked)
    }
}
