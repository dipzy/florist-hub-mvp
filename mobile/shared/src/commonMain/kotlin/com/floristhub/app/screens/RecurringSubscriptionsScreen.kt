package com.floristhub.app.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Card
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.floristhub.app.components.FloristScaffold
import com.floristhub.app.i18n.AppStrings
import com.floristhub.app.i18n.l10nString
import com.floristhub.app.i18n.recurringFrequencyLabel
import com.floristhub.app.i18n.recurringStatusLabel
import com.floristhub.network.ApiClient
import com.floristhub.network.dto.RecurringSubscriptionDto
import kotlinx.coroutines.launch

@Composable
fun RecurringSubscriptionsScreen(api: ApiClient, onBack: () -> Unit) {
    val items = remember { mutableStateListOf<RecurringSubscriptionDto>() }
    val scope = rememberCoroutineScope()
    LaunchedEffect(api) {
        runCatching { api.myRecurringSubscriptions() }.onSuccess {
            items.clear()
            items.addAll(it)
        }
    }

    FloristScaffold(title = l10nString(AppStrings.recurringTitle)) { padding ->
        Column(Modifier.fillMaxSize().padding(padding)) {
            TextButton(onClick = onBack) { Text(l10nString(AppStrings.prefsBack)) }
            LazyColumn(
                modifier = Modifier.fillMaxSize().padding(horizontal = 16.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                items(items) { sub ->
                    Card {
                        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                            Text(
                                recurringFrequencyLabel(sub.frequency),
                                style = MaterialTheme.typography.titleMedium,
                            )
                            Text(l10nString(AppStrings.recurringStatusLine, recurringStatusLabel(sub.status)))
                            sub.nextDeliveryAt?.let {
                                Text(l10nString(AppStrings.recurringNextDelivery, it), style = MaterialTheme.typography.bodySmall)
                            }
                            if (sub.status == 1) {
                                OutlinedButton(onClick = {
                                    scope.launch {
                                        runCatching { api.pauseRecurring(sub.id) }.onSuccess { refreshed ->
                                            val idx = items.indexOfFirst { it.id == sub.id }
                                            if (idx >= 0) items[idx] = refreshed
                                        }
                                    }
                                }) {
                                    Text(l10nString(AppStrings.recurringPause))
                                }
                            }
                        }
                    }
                }
                if (items.isEmpty()) {
                    item { Text(l10nString(AppStrings.recurringEmpty)) }
                }
            }
        }
    }
}
