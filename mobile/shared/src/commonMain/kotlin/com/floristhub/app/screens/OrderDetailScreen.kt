package com.floristhub.app.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Card
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Button
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.rememberCoroutineScope
import kotlinx.coroutines.launch
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
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
import com.floristhub.network.ApiClient
import com.floristhub.network.dto.OrderDto

@Composable
fun OrderDetailScreen(api: ApiClient, orderId: String, onBack: () -> Unit) {
    var order by remember { mutableStateOf<OrderDto?>(null) }
    var error by remember { mutableStateOf<String?>(null) }
    var coupon by remember { mutableStateOf("") }
    var stars by remember { mutableStateOf("5") }
    var comment by remember { mutableStateOf("") }
    var info by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    val openError = l10nString(AppStrings.orderDetailOpenError)
    val couponApplied = l10nString(AppStrings.orderDetailCouponApplied)
    val couponRejected = l10nString(AppStrings.orderDetailCouponRejected)
    val reviewSent = l10nString(AppStrings.orderDetailReviewSent)
    val reviewError = l10nString(AppStrings.orderDetailReviewError)

    LaunchedEffect(orderId) {
        runCatching { api.getOrder(orderId) }
            .onSuccess { order = it }
            .onFailure { error = it.message ?: openError }
    }

    FloristScaffold(title = l10nString(AppStrings.orderDetailTitle)) { padding ->
        FloristScreenBox(padding) {
            Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                TextButton(onClick = onBack) { Text(l10nString(AppStrings.prefsBack)) }
                when {
                    error != null && order == null -> EmptyState(
                        title = l10nString(AppStrings.orderDetailUnavailable),
                        description = error ?: "",
                    )
                    order == null -> Text(l10nString(AppStrings.commonLoading))
                    else -> {
                        val current = order!!
                        Column(
                            modifier = Modifier.fillMaxSize(),
                            verticalArrangement = Arrangement.spacedBy(12.dp),
                        ) {
                            Card(modifier = Modifier.fillMaxWidth()) {
                                Column(
                                    modifier = Modifier.padding(16.dp),
                                    verticalArrangement = Arrangement.spacedBy(8.dp),
                                ) {
                                    Text("#${current.id.take(8)}", style = MaterialTheme.typography.titleLarge)
                                    OrderStatusChip(
                                        status = current.status,
                                        label = orderStatusFullLabel(current.status),
                                    )
                                    Text(l10nString(AppStrings.orderDetailTotal, current.estimatedTotal))
                                    if (current.discountAmount > 0) {
                                        Text(
                                            l10nString(
                                                AppStrings.orderDetailDiscount,
                                                current.couponCode.orEmpty(),
                                                current.discountAmount,
                                            ),
                                        )
                                    }
                                    if (current.status in 1..3 && !current.paymentCaptured) {
                                        OutlinedTextField(
                                            value = coupon,
                                            onValueChange = { coupon = it },
                                            label = { Text(l10nString(AppStrings.commonCoupon)) },
                                            modifier = Modifier.fillMaxWidth(),
                                        )
                                        Button(onClick = {
                                            scope.launch {
                                                runCatching { api.applyCoupon(current.id, coupon) }
                                                    .onSuccess {
                                                        order = it
                                                        info = couponApplied
                                                    }
                                                    .onFailure { info = it.message ?: couponRejected }
                                            }
                                        }) { Text(l10nString(AppStrings.orderDetailApplyCoupon)) }
                                    }
                                    if (current.status == 4) {
                                        OutlinedTextField(
                                            value = stars,
                                            onValueChange = { stars = it },
                                            label = { Text(l10nString(AppStrings.orderDetailStars)) },
                                        )
                                        OutlinedTextField(
                                            value = comment,
                                            onValueChange = { comment = it },
                                            label = { Text(l10nString(AppStrings.orderDetailComment)) },
                                        )
                                        Button(onClick = {
                                            scope.launch {
                                                runCatching { api.createReview(current.id, stars.toIntOrNull() ?: 0, comment) }
                                                    .onSuccess { info = reviewSent }
                                                    .onFailure { info = it.message ?: reviewError }
                                            }
                                        }) { Text(l10nString(AppStrings.orderDetailReviewSubmit)) }
                                    }
                                    info?.let { Text(it) }
                                    current.deliveryAddress?.let {
                                        Text(l10nString(AppStrings.commonAddressColon, it))
                                    }
                                    current.items.forEach { item ->
                                        Text(l10nString(AppStrings.orderDetailItemQty, item.name, item.quantity))
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
