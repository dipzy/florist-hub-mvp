package com.floristhub.app.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Button
import androidx.compose.material3.Checkbox
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.floristhub.network.ApiClient
import com.floristhub.network.dto.CreateOrderItemDto
import com.floristhub.app.components.StoreDateTimePicker
import com.floristhub.network.dto.FlowerDto
import com.floristhub.network.dto.LegalVersionsDto
import com.floristhub.network.dto.PublicCapacityDayDto
import com.floristhub.network.dto.PublicStoreCheckoutDto
import com.floristhub.time.nowInstant
import kotlinx.datetime.DateTimeUnit
import kotlinx.datetime.TimeZone
import kotlinx.datetime.plus
import kotlinx.datetime.toLocalDateTime
import com.floristhub.orders.activeDeliveryOptions
import com.floristhub.orders.needsDeliveryZone
import com.floristhub.orders.requestedDeliveryToIso
import com.floristhub.orders.validateRequestedDeliveryAt
import com.floristhub.app.i18n.AppStrings
import com.floristhub.app.i18n.deliveryTypeLabel
import com.floristhub.app.i18n.l10nString
import kotlinx.coroutines.launch

private data class BasketLine(val flowerId: String, val name: String, val quantity: Int, val unitPrice: Double)

@Composable
fun BuildArrangementScreen(api: ApiClient, inspireId: String? = null) {
    val flowers = remember { mutableStateListOf<FlowerDto>() }
    val basket = remember { mutableStateListOf<BasketLine>() }
    var checkout by remember { mutableStateOf<PublicStoreCheckoutDto?>(null) }
    var capacityDays by remember { mutableStateOf<List<PublicCapacityDayDto>>(emptyList()) }
    var baseType by remember { mutableIntStateOf(0) }
    var deliveryType by remember { mutableIntStateOf(-1) }
    var deliveryZoneId by remember { mutableStateOf("") }
    var address by remember { mutableStateOf("") }
    var requestedAtEpoch by remember { mutableStateOf<Long?>(null) }
    var notes by remember { mutableStateOf("") }
    var couponCode by remember { mutableStateOf("") }
    var message by remember { mutableStateOf<String?>(null) }
    var legalVersions by remember { mutableStateOf<LegalVersionsDto?>(null) }
    var acceptLegal by remember { mutableStateOf(false) }
    var inspiredPortfolioTitle by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    val errCheckoutLoad = l10nString(AppStrings.buildErrCheckoutLoad)
    val errOrdersClosed = l10nString(AppStrings.buildErrOrdersClosed)
    val errCemeteryAddress = l10nString(AppStrings.buildErrCemeteryAddress)
    val errAddress = l10nString(AppStrings.buildErrAddress)
    val errDeliveryZone = l10nString(AppStrings.buildErrDeliveryZone)
    val errLegal = l10nString(AppStrings.buildErrLegal)
    val orderSubmitted = l10nString(AppStrings.buildOrderSubmitted)
    val outOfStockSuffix = l10nString(AppStrings.buildFlowerOutOfStock)
    val addressLabel = if (deliveryType == 2) {
        l10nString(AppStrings.buildAddressCemetery)
    } else {
        l10nString(AppStrings.commonAddress)
    }

    LaunchedEffect(Unit) {
        runCatching {
            legalVersions = api.legalVersions()
        }
        runCatching {
            flowers.addAll(api.flowers())
            val c = api.storeCheckout()
            checkout = c
            val today = nowInstant().toLocalDateTime(TimeZone.currentSystemDefault()).date
            val to = today.plus(60, DateTimeUnit.DAY)
            capacityDays = runCatching { api.publicCapacity(today.toString(), to.toString()) }.getOrDefault(emptyList())
            val opts = activeDeliveryOptions(c)
            if (opts.isNotEmpty()) deliveryType = opts.first().value
        }
    }

    LaunchedEffect(inspireId, flowers.size) {
        val id = inspireId ?: return@LaunchedEffect
        runCatching {
            val portfolio = api.portfolioItem(id)
            portfolio.items.filter { !it.isMaterial }.forEach { item ->
                val flower = item.flowerId?.let { fid -> flowers.find { it.id == fid } }
                basket.add(
                    BasketLine(
                        flowerId = flower?.id ?: item.flowerId.orEmpty(),
                        name = flower?.name ?: item.name,
                        quantity = item.quantity,
                        unitPrice = flower?.unitPrice ?: 0.0,
                    ),
                )
            }
            if (portfolio.description != null) inspiredPortfolioTitle = portfolio.title
        }
    }

    val inspiredNotes = inspiredPortfolioTitle?.let { l10nString(AppStrings.buildInspiredBy, it) }
    LaunchedEffect(inspiredNotes) {
        if (inspiredNotes != null) notes = inspiredNotes
    }

    fun addFlower(f: FlowerDto) {
        val idx = basket.indexOfFirst { it.flowerId == f.id }
        if (idx >= 0) {
            val line = basket[idx]
            basket[idx] = line.copy(quantity = line.quantity + 1)
        } else {
            basket.add(BasketLine(f.id, f.name, 1, f.unitPrice))
        }
    }

    val c = checkout
    val itemsTotal = basket.sumOf { it.quantity * it.unitPrice }
    val selectedZone = c?.deliveryZones?.find { it.id == deliveryZoneId }
    val zoneCount = c?.deliveryZones?.size ?: 0
    val needsZone = needsDeliveryZone(deliveryType, zoneCount)
    val deliveryFee = if (deliveryType != 0 && selectedZone != null) selectedZone.fee else 0.0
    val estimatedTotal = itemsTotal + deliveryFee
    val needsAddress = deliveryType == 1 || deliveryType == 2
    val ordersBlocked = c != null && !c.ordersAcceptingEnabled
    val deliveryOptions = c?.let { activeDeliveryOptions(it) } ?: emptyList()

    LazyColumn(Modifier.fillMaxSize().padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
        item {
            Text(l10nString(AppStrings.buildTitle))
            inspireId?.let { Text(l10nString(AppStrings.buildInspirationId, it.take(8))) }
            if (ordersBlocked) {
                Text(l10nString(AppStrings.buildOrdersBlocked))
            }
        }
        items(flowers) { f ->
            val exhausted = f.isOutOfStock || (f.stockQuantity != null && f.stockQuantity <= 0)
            Column {
                Text(
                    l10nString(
                        AppStrings.buildFlowerLine,
                        f.name,
                        f.unitPrice,
                        if (exhausted) outOfStockSuffix else "",
                    ),
                )
                Button(onClick = { addFlower(f) }, enabled = !ordersBlocked && !exhausted) {
                    Text(l10nString(AppStrings.commonAdd))
                }
            }
        }
        item {
            Text(l10nString(AppStrings.buildSubtotal, itemsTotal))
            if (deliveryFee > 0) {
                Text(
                    l10nString(
                        AppStrings.buildDeliveryFee,
                        deliveryFee,
                        selectedZone?.let { zn -> l10nString(AppStrings.buildDeliveryFeeZone, zn.name) } ?: "",
                    ),
                )
            }
            Text(l10nString(AppStrings.buildEstimatedTotal, estimatedTotal))
            basket.forEach { line -> Text(l10nString(AppStrings.buildBasketLine, line.name, line.quantity)) }

            if (deliveryOptions.isNotEmpty()) {
                Text(l10nString(AppStrings.buildDeliveryTypeHeading))
                deliveryOptions.forEach { opt ->
                    val optLabel = deliveryTypeLabel(opt.value)
                    Button(
                        onClick = {
                            deliveryType = opt.value
                            if (opt.value == 0) deliveryZoneId = ""
                        },
                        enabled = deliveryType != opt.value && !ordersBlocked,
                    ) {
                        Text(
                            if (deliveryType == opt.value) {
                                l10nString(AppStrings.buildDeliverySelected, optLabel)
                            } else {
                                optLabel
                            },
                        )
                    }
                }
            }

            if (needsZone && c != null) {
                Text(l10nString(AppStrings.buildDeliveryZoneHeading))
                c.deliveryZones.forEach { z ->
                    Button(
                        onClick = { deliveryZoneId = z.id },
                        enabled = !ordersBlocked,
                    ) {
                        Text(
                            if (deliveryZoneId == z.id) {
                                l10nString(AppStrings.buildZoneSelected, z.name, z.fee)
                            } else {
                                l10nString(AppStrings.buildZoneOption, z.name, z.fee)
                            },
                        )
                    }
                }
            }

            OutlinedTextField(
                value = couponCode,
                onValueChange = { couponCode = it },
                label = { Text(l10nString(AppStrings.commonCoupon)) },
                modifier = Modifier.fillMaxWidth(),
                singleLine = true,
            )
            OutlinedTextField(
                value = notes,
                onValueChange = { notes = it },
                label = { Text(l10nString(AppStrings.commonNotes)) },
                enabled = !ordersBlocked,
            )
            if (needsAddress) {
                OutlinedTextField(
                    value = address,
                    onValueChange = { address = it },
                    label = { Text(addressLabel) },
                    enabled = !ordersBlocked,
                )
            }
            if (c != null) {
                StoreDateTimePicker(
                    selectedEpochMillis = requestedAtEpoch,
                    onSelected = { requestedAtEpoch = it },
                    openingHours = c.openingHours,
                    minimumAdvanceMinutes = c.minimumOrderAdvanceMinutes,
                    enabled = !ordersBlocked,
                    capacityDays = capacityDays,
                )
                Text(l10nString(AppStrings.buildMinAdvance, c.minimumOrderAdvanceMinutes))
            }

            Button(
                onClick = {
                    scope.launch {
                        message = null
                        if (c == null) {
                            message = errCheckoutLoad
                            return@launch
                        }
                        if (!c.ordersAcceptingEnabled) {
                            message = errOrdersClosed
                            return@launch
                        }
                        if (needsAddress && address.isBlank()) {
                            message = if (deliveryType == 2) errCemeteryAddress else errAddress
                            return@launch
                        }
                        if (needsZone && deliveryZoneId.isBlank()) {
                            message = errDeliveryZone
                            return@launch
                        }
                        val versions = legalVersions
                        if (!acceptLegal || versions == null) {
                            message = errLegal
                            return@launch
                        }
                        var requestedIso: String? = null
                        val epoch = requestedAtEpoch
                        if (epoch != null) {
                            val err = validateRequestedDeliveryAt(epoch, c.openingHours, c.minimumOrderAdvanceMinutes)
                            if (err != null) {
                                message = err
                                return@launch
                            }
                            requestedIso = requestedDeliveryToIso(epoch)
                        }
                        runCatching {
                            api.createOrder(
                                baseType = baseType,
                                deliveryType = deliveryType,
                                deliveryAddress = address.ifBlank { null },
                                deliveryZoneId = if (needsZone) deliveryZoneId.ifBlank { null } else null,
                                requestedDeliveryAt = requestedIso,
                                customerNotes = notes.ifBlank { null },
                                inspiredByPortfolioArrangementId = inspireId,
                                items = basket.map {
                                    CreateOrderItemDto(
                                        flowerId = it.flowerId.ifBlank { null },
                                        name = it.name,
                                        quantity = it.quantity,
                                        unitPrice = it.unitPrice,
                                    )
                                },
                                acceptTermsAndPrivacy = true,
                                termsVersion = versions.termsVersion,
                                privacyVersion = versions.privacyVersion,
                                couponCode = couponCode.ifBlank { null },
                            )
                            message = orderSubmitted
                            basket.clear()
                            deliveryZoneId = ""
                        }.onFailure { message = it.message }
                    }
                },
                enabled = basket.isNotEmpty() && !ordersBlocked && acceptLegal,
            ) { Text(l10nString(AppStrings.buildSubmitOrder)) }
            Row(verticalAlignment = androidx.compose.ui.Alignment.CenterVertically) {
                Checkbox(checked = acceptLegal, onCheckedChange = { acceptLegal = it })
                Text(l10nString(AppStrings.buildAcceptLegal))
            }
            message?.let { Text(it) }
        }
    }
}
