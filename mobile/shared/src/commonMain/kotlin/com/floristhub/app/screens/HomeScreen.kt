package com.floristhub.app.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import com.floristhub.app.components.FloristScaffold
import com.floristhub.app.components.FloristScreenBox
import com.floristhub.app.i18n.AppStrings
import com.floristhub.app.i18n.l10nString
import com.floristhub.network.ApiClient
import com.floristhub.network.dto.PublicStorefrontDto

@Composable
fun HomeScreen(
    onPortfolio: () -> Unit,
    onOrders: () -> Unit,
    onBuild: (() -> Unit)? = null,
    onFloristDashboard: (() -> Unit)? = null,
    onFloristStore: (() -> Unit)? = null,
    onConfigurator: (() -> Unit)? = null,
    onNotifications: (() -> Unit)? = null,
    onRecurring: (() -> Unit)? = null,
    onAppPreferences: () -> Unit,
    onLegalTerms: () -> Unit,
    onLegalPrivacy: () -> Unit,
    onLegalCookies: () -> Unit,
    api: ApiClient? = null,
) {
    var storefront by remember { mutableStateOf<PublicStorefrontDto?>(null) }
    val isFlorist = onFloristDashboard != null
    LaunchedEffect(api) {
        val client = api ?: return@LaunchedEffect
        runCatching { client.storefront() }.onSuccess { storefront = it }
    }
    FloristScaffold(
        title = l10nString(if (isFlorist) AppStrings.homeTitleFlorist else AppStrings.homeTitleCustomer),
    ) { padding ->
        FloristScreenBox(padding) {
            Column(
                modifier = Modifier.fillMaxSize(),
                verticalArrangement = Arrangement.spacedBy(12.dp),
            ) {
                Text(
                    l10nString(if (isFlorist) AppStrings.homeHeadingFlorist else AppStrings.homeHeadingCustomer),
                    style = MaterialTheme.typography.headlineMedium,
                )
                storefront?.campaign?.let { campaign ->
                    Text(campaign.name, style = MaterialTheme.typography.titleLarge)
                    campaign.description?.let { Text(it) }
                    Text("${campaign.startsOn} a ${campaign.endsOn}")
                }
                storefront?.seasonalFlowers?.take(4)?.forEach { flower ->
                    val price = flower.specialPrice ?: flower.unitPrice
                    Text("${flower.name} — $price €")
                }
                Text(
                    l10nString(AppStrings.homeIntro),
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                Button(onClick = onPortfolio, modifier = Modifier.fillMaxWidth()) {
                    Text(l10nString(AppStrings.homePortfolio))
                }
                onBuild?.let { go ->
                    Button(onClick = go, modifier = Modifier.fillMaxWidth()) {
                        Text(l10nString(AppStrings.homeBuild))
                    }
                }
                onConfigurator?.let { go ->
                    Button(onClick = go, modifier = Modifier.fillMaxWidth()) {
                        Text(l10nString(AppStrings.homeConfigurator))
                    }
                }
                OutlinedButton(onClick = onOrders, modifier = Modifier.fillMaxWidth()) {
                    Text(l10nString(AppStrings.homeOrders))
                }
                onNotifications?.let { go ->
                    OutlinedButton(onClick = go, modifier = Modifier.fillMaxWidth()) {
                        Text(l10nString(AppStrings.homeNotifications))
                    }
                }
                onRecurring?.let { go ->
                    OutlinedButton(onClick = go, modifier = Modifier.fillMaxWidth()) {
                        Text(l10nString(AppStrings.homeRecurring))
                    }
                }
                onFloristDashboard?.let { go ->
                    OutlinedButton(onClick = go, modifier = Modifier.fillMaxWidth()) {
                        Text(l10nString(AppStrings.homeFloristDashboard))
                    }
                }
                onFloristStore?.let { go ->
                    OutlinedButton(onClick = go, modifier = Modifier.fillMaxWidth()) {
                        Text(l10nString(AppStrings.homeStoreSettings))
                    }
                }
                OutlinedButton(onClick = onAppPreferences, modifier = Modifier.fillMaxWidth()) {
                    Text(l10nString(AppStrings.homeAppPreferences))
                }
                Text(l10nString(AppStrings.homeLegalSection), style = MaterialTheme.typography.titleSmall)
                OutlinedButton(onClick = onLegalTerms, modifier = Modifier.fillMaxWidth()) {
                    Text(l10nString(AppStrings.homeLegalTerms))
                }
                OutlinedButton(onClick = onLegalPrivacy, modifier = Modifier.fillMaxWidth()) {
                    Text(l10nString(AppStrings.homeLegalPrivacy))
                }
                OutlinedButton(onClick = onLegalCookies, modifier = Modifier.fillMaxWidth()) {
                    Text(l10nString(AppStrings.homeLegalCookies))
                }
            }
        }
    }
}
