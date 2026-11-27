package com.floristhub.app.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.floristhub.app.components.EmptyState
import com.floristhub.app.components.FloristScaffold
import com.floristhub.app.components.FloristScreenBox
import com.floristhub.app.components.RemoteImage
import com.floristhub.app.i18n.AppStrings
import com.floristhub.app.i18n.l10nString
import com.floristhub.network.ApiClient
import com.floristhub.network.dto.PortfolioDto

@Composable
fun PortfolioScreen(api: ApiClient, onUseAsBase: (String) -> Unit, isCustomer: Boolean) {
    var items by remember { mutableStateOf<List<PortfolioDto>?>(null) }
    var failed by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) {
        runCatching { items = api.portfolio() }
            .onFailure { failed = true; items = emptyList() }
    }

    FloristScaffold(title = l10nString(AppStrings.portfolioScreenTitle)) { padding ->
        FloristScreenBox(padding) {
            when {
                items == null -> Column(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalAlignment = Alignment.CenterHorizontally,
                ) {
                    CircularProgressIndicator()
                }
                items!!.isEmpty() -> EmptyState(
                    title = if (failed) {
                        l10nString(AppStrings.portfolioEmptyFailTitle)
                    } else {
                        l10nString(AppStrings.portfolioEmptyTitle)
                    },
                    description = if (failed) {
                        l10nString(AppStrings.portfolioEmptyFailDesc)
                    } else {
                        l10nString(AppStrings.portfolioEmptyDesc)
                    },
                )
                else -> LazyColumn(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                    items(items!!) { p ->
                        Card(
                            elevation = CardDefaults.cardElevation(defaultElevation = 2.dp),
                            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
                        ) {
                            Column(
                                modifier = Modifier.padding(12.dp),
                                verticalArrangement = Arrangement.spacedBy(8.dp),
                            ) {
                                p.photos.firstOrNull()?.let { photo ->
                                    RemoteImage(
                                        url = api.mediaUrl(photo.url),
                                        contentDescription = p.title,
                                    )
                                }
                                Text(p.title, style = MaterialTheme.typography.titleLarge)
                                p.price?.let {
                                    Text(
                                        l10nString(AppStrings.portfolioPrice, it),
                                        style = MaterialTheme.typography.labelLarge,
                                        color = MaterialTheme.colorScheme.primary,
                                    )
                                }
                                p.description?.let {
                                    Text(it, color = MaterialTheme.colorScheme.onSurfaceVariant)
                                }
                                if (isCustomer) {
                                    Button(onClick = { onUseAsBase(p.id) }, modifier = Modifier.fillMaxWidth()) {
                                        Text(l10nString(AppStrings.portfolioUseAsBase))
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
