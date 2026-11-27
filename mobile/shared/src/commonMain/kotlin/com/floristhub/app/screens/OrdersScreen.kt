package com.floristhub.app.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.layout.Row
import androidx.compose.material3.Button
import androidx.compose.material3.Checkbox
import androidx.compose.material3.Card
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.floristhub.app.components.EmptyState
import com.floristhub.app.components.FloristScaffold
import com.floristhub.app.components.FloristScreenBox
import com.floristhub.app.components.OrderStatusChip
import com.floristhub.app.i18n.AppStrings
import com.floristhub.app.i18n.l10nString
import com.floristhub.app.i18n.orderStatusFullLabel
import com.floristhub.app.i18n.paymentStatusLabel
import com.floristhub.network.ApiClient
import com.floristhub.platform.CheckoutReturn
import com.floristhub.platform.openSystemCheckout
import com.floristhub.network.dto.LegalVersionsDto
import com.floristhub.network.dto.OrderDto
import kotlinx.coroutines.launch

private fun canCancel(status: Int) = status == 0 || status == 1

private fun canPay(order: OrderDto): Boolean {
    val open = order.status == 1 || order.status == 2 || order.status == 3
    val settled = order.paymentCaptured || order.payment?.status == 1 || order.payment?.status == 3
    return open && !settled
}

@Composable
fun OrdersScreen(api: ApiClient) {
    var items by remember { mutableStateOf<List<OrderDto>>(emptyList()) }
    var loaded by remember { mutableStateOf(false) }
    var notice by remember { mutableStateOf<String?>(null) }
    var legalVersions by remember { mutableStateOf<LegalVersionsDto?>(null) }
    var acceptLegal by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()
    val legalErr = l10nString(AppStrings.buildErrLegal)
    val payStartError = l10nString(AppStrings.ordersPayStartError)
    val cancelReason = l10nString(AppStrings.ordersCancelReason)

    fun refresh() {
        scope.launch {
            runCatching { items = api.myOrders() }
            loaded = true
        }
    }

    LaunchedEffect(Unit) {
        refresh()
        runCatching { legalVersions = api.legalVersions() }
    }

    DisposableEffect(Unit) {
        CheckoutReturn.onReturned = { refresh() }
        onDispose { CheckoutReturn.onReturned = null }
    }

    FloristScaffold(title = l10nString(AppStrings.ordersTitle)) { padding ->
        FloristScreenBox(padding) {
            if (loaded && items.isEmpty()) {
                EmptyState(
                    title = l10nString(AppStrings.ordersEmptyTitle),
                    description = l10nString(AppStrings.ordersEmptyDesc),
                )
            } else {
                Column(modifier = Modifier.fillMaxSize(), verticalArrangement = Arrangement.spacedBy(12.dp)) {
                    notice?.let { Text(it, color = MaterialTheme.colorScheme.error) }
                    LazyColumn(
                        modifier = Modifier.weight(1f),
                        verticalArrangement = Arrangement.spacedBy(12.dp),
                    ) {
                        items(items) { o ->
                            Card {
                                Column(
                                    modifier = Modifier.padding(16.dp),
                                    verticalArrangement = Arrangement.spacedBy(8.dp),
                                ) {
                                    Text("#${o.id.take(8)}", style = MaterialTheme.typography.titleLarge)
                                    OrderStatusChip(
                                        status = o.status,
                                        label = orderStatusFullLabel(o.status),
                                    )
                                    Text(
                                        l10nString(AppStrings.orderDetailTotal, o.estimatedTotal),
                                        style = MaterialTheme.typography.labelLarge,
                                        color = MaterialTheme.colorScheme.primary,
                                    )
                                    if (o.deliveryFee > 0) {
                                        Text(
                                            l10nString(
                                                AppStrings.ordersDeliveryLine,
                                                o.deliveryFee,
                                                o.deliveryZoneName?.let { zn ->
                                                    l10nString(AppStrings.ordersDeliveryZoneParen, zn)
                                                } ?: "",
                                            ),
                                            style = MaterialTheme.typography.bodySmall,
                                        )
                                    }
                                    o.deliveryAddress?.let {
                                        Text(l10nString(AppStrings.commonAddressColon, it), style = MaterialTheme.typography.bodySmall)
                                    }
                                    o.items.forEach { i ->
                                        Text(
                                            l10nString(AppStrings.orderDetailItemQty, i.name, i.quantity),
                                            style = MaterialTheme.typography.bodyMedium,
                                        )
                                    }
                                    if (o.statusHistory.isNotEmpty()) {
                                        Text(l10nString(AppStrings.ordersTimeline), style = MaterialTheme.typography.labelLarge)
                                        o.statusHistory.forEach { h ->
                                            Text(
                                                l10nString(
                                                    AppStrings.ordersTimelineEntry,
                                                    orderStatusFullLabel(h.fromStatus),
                                                    orderStatusFullLabel(h.toStatus),
                                                    h.notes ?: "—",
                                                ),
                                                style = MaterialTheme.typography.bodySmall,
                                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                                            )
                                        }
                                    }
                                    o.changeProposals.filter { it.status == 0 }.forEach { proposal ->
                                        Text(l10nString(AppStrings.commonProposal, proposal.message))
                                        Button(onClick = { scope.launch { api.acceptProposal(o.id, proposal.id); refresh() } }) {
                                            Text(l10nString(AppStrings.commonAccept))
                                        }
                                        OutlinedButton(onClick = { scope.launch { api.rejectProposal(o.id, proposal.id); refresh() } }) {
                                            Text(l10nString(AppStrings.commonReject))
                                        }
                                    }
                                    if (canCancel(o.status)) {
                                        OutlinedButton(
                                            onClick = {
                                                scope.launch {
                                                    api.cancelOrder(o.id, cancelReason)
                                                    refresh()
                                                }
                                            },
                                            modifier = Modifier.fillMaxWidth(),
                                        ) {
                                            Text(l10nString(AppStrings.ordersCancel))
                                        }
                                    }
                                    val paymentStatus = o.payment?.status
                                    if (paymentStatus != null) {
                                        Text(
                                            paymentStatusLabel(paymentStatus),
                                            style = MaterialTheme.typography.bodySmall,
                                        )
                                    } else if (o.paymentCaptured) {
                                        Text(l10nString(AppStrings.commonPaid), style = MaterialTheme.typography.bodySmall)
                                    }
                                    if (canPay(o)) {
                                        Row {
                                            Checkbox(checked = acceptLegal, onCheckedChange = { acceptLegal = it })
                                            Text(l10nString(AppStrings.ordersAcceptLegal))
                                        }
                                        Button(
                                            onClick = {
                                                scope.launch {
                                                    notice = null
                                                    val versions = legalVersions
                                                    if (!acceptLegal || versions == null) {
                                                        notice = legalErr
                                                        return@launch
                                                    }
                                                    runCatching {
                                                        api.startCheckout(
                                                            o.id,
                                                            "floristhub://pagamento/sucesso?orderId=${o.id}",
                                                            "floristhub://pagamento/cancelado?orderId=${o.id}",
                                                            true,
                                                            versions.termsVersion,
                                                            versions.privacyVersion,
                                                        )
                                                    }.onSuccess { session ->
                                                        val url = session.checkoutUrl
                                                        if (!url.isNullOrBlank()) openSystemCheckout(url)
                                                        else notice = session.message ?: payStartError
                                                    }.onFailure { notice = it.message ?: payStartError }
                                                }
                                            },
                                            modifier = Modifier.fillMaxWidth(),
                                        ) {
                                            Text(l10nString(AppStrings.commonPay))
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
}
