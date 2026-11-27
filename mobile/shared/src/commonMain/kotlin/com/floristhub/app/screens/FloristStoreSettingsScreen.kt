package com.floristhub.app.screens

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.Checkbox
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
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
import com.floristhub.app.i18n.AppStrings
import com.floristhub.app.i18n.l10nString
import com.floristhub.app.i18n.weekdayLabel
import com.floristhub.network.ApiClient
import com.floristhub.network.dto.DeliveryZoneInputDto
import com.floristhub.network.dto.OpeningHourDto
import com.floristhub.network.dto.StoreSettingsDto
import com.floristhub.network.dto.UpdateStoreSettingsDto
import kotlinx.coroutines.launch

private fun defaultHours(): List<OpeningHourDto> =
    (0..6).map { day ->
        OpeningHourDto(
            dayOfWeek = day,
            openTime = if (day == 0) null else "09:00",
            closeTime = if (day == 0) null else "19:00",
            isClosed = day == 0,
        )
    }

@Composable
fun FloristStoreSettingsScreen(
    api: ApiClient,
    onUploadLogo: ((onResult: (ByteArray, String, String) -> Unit) -> Unit)? = null,
    onOpenAppPreferences: (() -> Unit)? = null,
) {
    var settings by remember { mutableStateOf<StoreSettingsDto?>(null) }
    var message by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    val scroll = rememberScrollState()
    val loadError = l10nString(AppStrings.storeLoadError)
    val uploadLogoFail = l10nString(AppStrings.storeUploadLogoFail)
    val nameRequired = l10nString(AppStrings.storeNameRequired)
    val saved = l10nString(AppStrings.storeSaved)
    val saveError = l10nString(AppStrings.storeSaveError)
    val newZoneDefault = l10nString(AppStrings.storeNewZoneDefault)

    LaunchedEffect(Unit) {
        runCatching { settings = api.storeSettings() }
            .onFailure { message = loadError }
    }

    Column(
        Modifier.fillMaxSize().padding(16.dp).verticalScroll(scroll),
        verticalArrangement = Arrangement.spacedBy(8.dp),
    ) {
        Text(l10nString(AppStrings.homeStoreSettings))
        onOpenAppPreferences?.let { open ->
            OutlinedButton(onClick = open, modifier = Modifier.fillMaxWidth()) {
                Text(l10nString(AppStrings.storePrefsLink))
            }
        }
        message?.let { Text(it) }

        val s = settings
        if (s == null) {
            Text(l10nString(AppStrings.commonLoading))
            return@Column
        }

        val hours = if (s.openingHours.isNotEmpty()) s.openingHours else defaultHours()
        var zones by remember(s) { mutableStateOf(s.deliveryZones) }

        fun patch(block: (StoreSettingsDto) -> StoreSettingsDto) {
            settings = settings?.let(block)
        }

        OutlinedTextField(
            value = s.name,
            onValueChange = { v -> patch { it.copy(name = v) } },
            label = { Text(l10nString(AppStrings.commonName)) },
            modifier = Modifier.fillMaxWidth(),
        )
        OutlinedTextField(
            value = s.description.orEmpty(),
            onValueChange = { v -> patch { it.copy(description = v) } },
            label = { Text(l10nString(AppStrings.commonDescription)) },
            modifier = Modifier.fillMaxWidth(),
        )
        OutlinedTextField(
            value = s.contactEmail.orEmpty(),
            onValueChange = { v -> patch { it.copy(contactEmail = v) } },
            label = { Text(l10nString(AppStrings.loginEmail)) },
            modifier = Modifier.fillMaxWidth(),
        )
        OutlinedTextField(
            value = s.contactPhone.orEmpty(),
            onValueChange = { v -> patch { it.copy(contactPhone = v) } },
            label = { Text(l10nString(AppStrings.commonPhone)) },
            modifier = Modifier.fillMaxWidth(),
        )
        OutlinedTextField(
            value = s.addressLine.orEmpty(),
            onValueChange = { v -> patch { it.copy(addressLine = v) } },
            label = { Text(l10nString(AppStrings.commonAddress)) },
            modifier = Modifier.fillMaxWidth(),
        )
        OutlinedTextField(
            value = s.city.orEmpty(),
            onValueChange = { v -> patch { it.copy(city = v) } },
            label = { Text(l10nString(AppStrings.commonCity)) },
            modifier = Modifier.fillMaxWidth(),
        )
        OutlinedTextField(
            value = s.postalCode.orEmpty(),
            onValueChange = { v -> patch { it.copy(postalCode = v) } },
            label = { Text(l10nString(AppStrings.commonPostalCode)) },
            modifier = Modifier.fillMaxWidth(),
        )
        if (onUploadLogo != null) {
            Button(
                onClick = {
                    onUploadLogo { bytes, fileName, contentType ->
                        scope.launch {
                            runCatching {
                                val res = api.uploadPortfolioPhoto(bytes, fileName, contentType)
                                settings = settings?.copy(logoUrl = res.url)
                            }.onFailure { message = uploadLogoFail }
                        }
                    }
                },
            ) { Text(l10nString(AppStrings.storeUploadLogo)) }
            s.logoUrl?.let { Text(l10nString(AppStrings.storeLogoUrl, it)) }
        }

        Row(verticalAlignment = Alignment.CenterVertically) {
            Checkbox(checked = s.ordersAcceptingEnabled, onCheckedChange = { v -> patch { it.copy(ordersAcceptingEnabled = v) } })
            Text(l10nString(AppStrings.storeAcceptOrders))
        }
        OutlinedTextField(
            value = s.minimumOrderAdvanceMinutes.toString(),
            onValueChange = { v -> v.toIntOrNull()?.let { m -> patch { it.copy(minimumOrderAdvanceMinutes = m) } } },
            label = { Text(l10nString(AppStrings.storeMinAdvanceMin)) },
            modifier = Modifier.fillMaxWidth(),
        )

        Text(l10nString(AppStrings.storeDeliveryTypes))
        Row(verticalAlignment = Alignment.CenterVertically) {
            Checkbox(checked = s.pickupEnabled, onCheckedChange = { v -> patch { it.copy(pickupEnabled = v) } })
            Text(l10nString(AppStrings.storePickup))
        }
        Row(verticalAlignment = Alignment.CenterVertically) {
            Checkbox(checked = s.homeDeliveryEnabled, onCheckedChange = { v -> patch { it.copy(homeDeliveryEnabled = v) } })
            Text(l10nString(AppStrings.storeHomeDelivery))
        }
        Row(verticalAlignment = Alignment.CenterVertically) {
            Checkbox(checked = s.cemeteryDeliveryEnabled, onCheckedChange = { v -> patch { it.copy(cemeteryDeliveryEnabled = v) } })
            Text(l10nString(AppStrings.storeCemeteryDelivery))
        }

        Text(l10nString(AppStrings.storeHours))
        hours.sortedBy { it.dayOfWeek }.forEach { h ->
            Text(weekdayLabel(h.dayOfWeek))
            Row(verticalAlignment = Alignment.CenterVertically) {
                Checkbox(
                    checked = h.isClosed,
                    onCheckedChange = { closed ->
                        val updated = hours.map { x ->
                            if (x.dayOfWeek == h.dayOfWeek) x.copy(isClosed = closed) else x
                        }
                        patch { it.copy(openingHours = updated) }
                    },
                )
                Text(l10nString(AppStrings.storeClosed))
            }
            if (!h.isClosed) {
                OutlinedTextField(
                    value = h.openTime.orEmpty(),
                    onValueChange = { t ->
                        val updated = hours.map { x -> if (x.dayOfWeek == h.dayOfWeek) x.copy(openTime = t) else x }
                        patch { it.copy(openingHours = updated) }
                    },
                    label = { Text(l10nString(AppStrings.storeOpenTime)) },
                    modifier = Modifier.fillMaxWidth(),
                )
                OutlinedTextField(
                    value = h.closeTime.orEmpty(),
                    onValueChange = { t ->
                        val updated = hours.map { x -> if (x.dayOfWeek == h.dayOfWeek) x.copy(closeTime = t) else x }
                        patch { it.copy(openingHours = updated) }
                    },
                    label = { Text(l10nString(AppStrings.storeCloseTime)) },
                    modifier = Modifier.fillMaxWidth(),
                )
            }
        }

        Text(l10nString(AppStrings.storeDeliveryZones))
        zones.forEachIndexed { index, z ->
            OutlinedTextField(
                value = z.name,
                onValueChange = { name ->
                    zones = zones.toMutableList().also { it[index] = it[index].copy(name = name) }
                },
                label = { Text(l10nString(AppStrings.storeZoneName)) },
                modifier = Modifier.fillMaxWidth(),
            )
            OutlinedTextField(
                value = z.fee.toString(),
                onValueChange = { v ->
                    v.toDoubleOrNull()?.let { fee ->
                        zones = zones.toMutableList().also { it[index] = it[index].copy(fee = fee) }
                    }
                },
                label = { Text(l10nString(AppStrings.storeZoneFee)) },
                modifier = Modifier.fillMaxWidth(),
            )
            Row(verticalAlignment = Alignment.CenterVertically) {
                Checkbox(
                    checked = z.isActive,
                    onCheckedChange = { active ->
                        zones = zones.toMutableList().also { it[index] = it[index].copy(isActive = active) }
                    },
                )
                Text(l10nString(AppStrings.commonActive))
            }
            Button(
                onClick = { zones = zones.toMutableList().also { it.removeAt(index) } },
            ) { Text(l10nString(AppStrings.storeRemoveZone)) }
        }
        Button(
            onClick = {
                zones = zones + com.floristhub.network.dto.DeliveryZoneDto(
                    id = "",
                    name = newZoneDefault,
                    fee = 0.0,
                    isActive = true,
                    sortOrder = zones.size,
                )
            },
        ) { Text(l10nString(AppStrings.storeAddZone)) }

        Button(
            onClick = {
                val current = settings ?: return@Button
                if (current.name.isBlank()) {
                    message = nameRequired
                    return@Button
                }
                scope.launch {
                    message = null
                    runCatching {
                        val body = UpdateStoreSettingsDto(
                            name = current.name.trim(),
                            description = current.description?.trim(),
                            logoUrl = current.logoUrl,
                            contactEmail = current.contactEmail?.trim(),
                            contactPhone = current.contactPhone?.trim(),
                            addressLine = current.addressLine?.trim(),
                            city = current.city?.trim(),
                            postalCode = current.postalCode?.trim(),
                            ordersAcceptingEnabled = current.ordersAcceptingEnabled,
                            minimumOrderAdvanceMinutes = current.minimumOrderAdvanceMinutes,
                            pickupEnabled = current.pickupEnabled,
                            homeDeliveryEnabled = current.homeDeliveryEnabled,
                            cemeteryDeliveryEnabled = current.cemeteryDeliveryEnabled,
                            openingHours = if (current.openingHours.isNotEmpty()) current.openingHours else hours,
                            deliveryZones = zones.mapIndexed { i, z ->
                                DeliveryZoneInputDto(
                                    id = z.id.takeIf { it.length > 8 },
                                    name = z.name.trim(),
                                    fee = z.fee,
                                    isActive = z.isActive,
                                    sortOrder = i,
                                )
                            },
                        )
                        settings = api.updateStoreSettings(body)
                        zones = settings?.deliveryZones ?: emptyList()
                        message = saved
                    }.onFailure { message = it.message ?: saveError }
                }
            },
        ) { Text(l10nString(AppStrings.commonSave)) }
    }
}
