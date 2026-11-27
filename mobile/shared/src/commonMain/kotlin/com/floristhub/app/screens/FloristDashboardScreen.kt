package com.floristhub.app.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.FilterChip
import androidx.compose.material3.FilterChipDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Button
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.floristhub.app.components.MiniTrendChart
import com.floristhub.app.format.formatEuroAmount
import com.floristhub.app.i18n.AppStrings
import com.floristhub.app.i18n.l10nString
import com.floristhub.app.i18n.orderStatusShortLabel
import com.floristhub.app.util.countLabel
import com.floristhub.network.ApiClient
import com.floristhub.network.dto.CreateOrderItemDto
import com.floristhub.network.dto.FloristDashboardDto
import com.floristhub.network.dto.FloristDashboardMetricsDto
import com.floristhub.network.dto.OrderDto
import com.floristhub.network.dto.PublicCapacityDayDto
import com.floristhub.time.nowInstant
import kotlinx.datetime.TimeZone
import kotlinx.datetime.toLocalDateTime
import kotlinx.coroutines.launch
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json

private val nextStatus = mapOf(1 to 2, 2 to 3, 3 to 4)

@Composable
fun FloristDashboardScreen(
    api: ApiClient,
    onUploadPhoto: ((onResult: (ByteArray, String, String) -> Unit) -> Unit)? = null,
    onOnboarding: (() -> Unit)? = null,
) {
    var filterStatus by remember { mutableIntStateOf(0) }
    var periodDays by remember { mutableIntStateOf(30) }
    var dashboard by remember { mutableStateOf<FloristDashboardDto?>(null) }
    var metrics by remember { mutableStateOf<FloristDashboardMetricsDto?>(null) }
    var todayCapacity by remember { mutableStateOf<PublicCapacityDayDto?>(null) }
    var orders by remember { mutableStateOf<List<OrderDto>>(emptyList()) }
    var title by remember { mutableStateOf("") }
    var description by remember { mutableStateOf("") }
    val photoUrls = remember { mutableStateOf(listOf<String>()) }
    val proposalMessages = remember { mutableStateMapOf<String, String>() }
    val rejectReasons = remember { mutableStateMapOf<String, String>() }
    val scope = rememberCoroutineScope()
    val orderSingular = l10nString(AppStrings.wordOrderSingular)
    val orderPlural = l10nString(AppStrings.wordOrderPlural)
    val wordToday = l10nString(AppStrings.wordToday)
    val wordTomorrow = l10nString(AppStrings.wordTomorrow)
    val statusUpdateNote = l10nString(AppStrings.dashboardStatusUpdateNote)

    fun refresh() {
        scope.launch { runCatching { orders = api.floristOrders(filterStatus) } }
    }

    fun refreshMetrics() {
        scope.launch {
            runCatching {
                dashboard = api.floristDashboard()
                metrics = api.floristDashboardMetrics(periodDays)
                val today = nowInstant().toLocalDateTime(TimeZone.currentSystemDefault()).date.toString()
                todayCapacity = api.floristCapacity(today, today).firstOrNull()
            }
        }
    }

    LaunchedEffect(filterStatus) {
        refresh()
    }

    LaunchedEffect(periodDays) {
        refreshMetrics()
    }

    val colors = MaterialTheme.colorScheme
    val trend = metrics?.ordersByDay?.map { it.count }.orEmpty().takeLast(14)

    LazyColumn(Modifier.fillMaxSize().padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
        item {
            Text(l10nString(AppStrings.homeFloristDashboard), style = MaterialTheme.typography.headlineSmall)
            onOnboarding?.let { open ->
                Button(onClick = open) { Text(l10nString(AppStrings.dashboardStorePrep)) }
            }
            dashboard?.let { d ->
                val todayPart = "${countLabel(d.ordersToday, orderSingular, orderPlural)} $wordToday"
                val tomorrowPart = "${countLabel(d.ordersTomorrow, orderSingular, orderPlural)} $wordTomorrow"
                Text(l10nString(AppStrings.dashboardOrdersSummary, todayPart, tomorrowPart))
            }
            todayCapacity?.let { day ->
                val occupancy = when {
                    day.closed -> l10nString(AppStrings.dashboardCapacityClosed)
                    day.dailyLimit == null -> l10nString(AppStrings.dashboardCapacityUnlimited, day.booked)
                    else -> l10nString(AppStrings.dashboardCapacityOccupancy, day.booked, day.dailyLimit)
                }
                Text(occupancy)
            }
            metrics?.let { m ->
                Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    listOf(7, 30, 90).forEach { days ->
                        FilterChip(
                            selected = periodDays == days,
                            onClick = { periodDays = days },
                            label = { Text(l10nString(AppStrings.dashboardPeriodDays, days)) },
                            colors = FilterChipDefaults.filterChipColors(
                                selectedContainerColor = colors.primary.copy(alpha = 0.2f),
                            ),
                        )
                    }
                }
                Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    MetricCard(
                        label = l10nString(AppStrings.dashboardMetricPending),
                        value = m.pendingOrders.toString(),
                        modifier = Modifier.weight(1f),
                    )
                    MetricCard(
                        label = l10nString(AppStrings.dashboardMetricRevenue, m.days),
                        value = "${formatEuroAmount(m.periodRevenue)} €",
                        modifier = Modifier.weight(1f),
                    )
                }
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(containerColor = colors.surfaceVariant.copy(alpha = 0.45f)),
                ) {
                    Column(Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        Text(l10nString(AppStrings.dashboardTrendTitle), style = MaterialTheme.typography.titleSmall)
                        MiniTrendChart(
                            values = trend,
                            lineColor = colors.primary,
                            fillColor = colors.tertiary.copy(alpha = 0.25f),
                        )
                    }
                }
            }
            RowFilters(filterStatus) { filterStatus = it; refresh() }
        }
        items(orders) { order ->
            Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
                Text(
                    l10nString(
                        AppStrings.dashboardOrderLine,
                        order.id.take(8),
                        orderStatusShortLabel(order.status),
                        order.estimatedTotal,
                    ),
                )
                order.deliveryAddress?.let { Text(l10nString(AppStrings.commonAddressColon, it)) }
                order.items.forEach { i -> Text(l10nString(AppStrings.buildBasketLine, i.name, i.quantity)) }

                if (order.status == 0) {
                    OutlinedTextField(
                        value = proposalMessages[order.id] ?: "",
                        onValueChange = { proposalMessages[order.id] = it },
                        label = { Text(l10nString(AppStrings.dashboardChangeProposal)) },
                    )
                    Button(onClick = {
                        scope.launch {
                            val message = proposalMessages[order.id]?.trim().orEmpty()
                            if (message.isEmpty()) return@launch
                            val itemsJson = Json.encodeToString(
                                order.items.map {
                                    CreateOrderItemDto(it.flowerId, it.name, it.quantity, it.unitPrice)
                                },
                            )
                            api.proposeChanges(order.id, message, itemsJson, order.estimatedTotal)
                            refresh()
                            refreshMetrics()
                        }
                    }) { Text(l10nString(AppStrings.dashboardSendProposal)) }
                    OutlinedTextField(
                        value = rejectReasons[order.id] ?: "",
                        onValueChange = { rejectReasons[order.id] = it },
                        label = { Text(l10nString(AppStrings.dashboardRejectReason)) },
                    )
                    Button(onClick = {
                        scope.launch {
                            val reason = rejectReasons[order.id]?.trim().orEmpty()
                            if (reason.isEmpty()) return@launch
                            api.rejectOrder(order.id, reason)
                            refresh()
                            refreshMetrics()
                        }
                    }) { Text(l10nString(AppStrings.commonReject)) }
                    Button(onClick = {
                        scope.launch {
                            api.confirmOrder(order.id)
                            refresh()
                            refreshMetrics()
                        }
                    }) { Text(l10nString(AppStrings.dashboardConfirm)) }
                }

                nextStatus[order.status]?.let { next ->
                    Button(onClick = {
                        scope.launch {
                            api.updateOrderStatus(order.id, next, statusUpdateNote)
                            refresh()
                            refreshMetrics()
                        }
                    }) {
                        Text(
                            when (next) {
                                2 -> l10nString(AppStrings.dashboardMarkPreparing)
                                3 -> l10nString(AppStrings.dashboardMarkReady)
                                4 -> l10nString(AppStrings.dashboardMarkDelivered)
                                else -> l10nString(AppStrings.dashboardAdvance)
                            },
                        )
                    }
                }
            }
        }
        item {
            Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                Text(l10nString(AppStrings.dashboardNewPortfolio))
                OutlinedTextField(
                    value = title,
                    onValueChange = { title = it },
                    label = { Text(l10nString(AppStrings.commonTitle)) },
                )
                OutlinedTextField(
                    value = description,
                    onValueChange = { description = it },
                    label = { Text(l10nString(AppStrings.commonDescription)) },
                )
                if (onUploadPhoto != null) {
                    Button(onClick = {
                        onUploadPhoto { bytes, name, type ->
                            scope.launch {
                                val uploaded = api.uploadPortfolioPhoto(bytes, name, type)
                                photoUrls.value = photoUrls.value + uploaded.url
                            }
                        }
                    }) { Text(l10nString(AppStrings.dashboardUploadPhoto)) }
                }
                Button(onClick = {
                    scope.launch {
                        api.createPortfolio(title, description.ifBlank { null }, photoUrls.value)
                        title = ""
                        description = ""
                        photoUrls.value = emptyList()
                    }
                }) { Text(l10nString(AppStrings.commonPublish)) }
            }
        }
    }
}

@Composable
private fun MetricCard(label: String, value: String, modifier: Modifier = Modifier) {
    Card(modifier = modifier) {
        Column(Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(4.dp)) {
            Text(label, style = MaterialTheme.typography.labelMedium)
            Text(value, style = MaterialTheme.typography.titleLarge)
        }
    }
}

@Composable
private fun RowFilters(selected: Int, onSelect: (Int) -> Unit) {
    Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
        val filters = listOf(
            0 to l10nString(AppStrings.dashboardFilterPending),
            1 to l10nString(AppStrings.dashboardFilterConfirmed),
            2 to l10nString(AppStrings.dashboardFilterPreparing),
            3 to l10nString(AppStrings.dashboardFilterReady),
            4 to l10nString(AppStrings.dashboardFilterDelivered),
        )
        filters.forEach { (s, label) ->
            Button(onClick = { onSelect(s) }) {
                Text(if (s == selected) l10nString(AppStrings.dashboardFilterSelected, label) else label)
            }
        }
    }
}
